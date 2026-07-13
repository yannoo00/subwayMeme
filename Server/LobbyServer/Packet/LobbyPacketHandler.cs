using System;
using System.Collections.Generic;
using Google.Protobuf;
using LobbyProto;
using InternalProto;
using YannooNet;

namespace LobbyServer
{
    public class LobbyPacketHandler
    {
        // === pending game start (G2L_RoomCreated ack 대기 중) ===
        // C_StartGame 처리 시 S_GameReady 를 즉시 보내지 않고 여기 보관.
        // GameServer 가 룸 생성 완료를 ack 로 알려오면 FinalizeGameStart 에서 꺼내 송신.
        // race 방어: 클라가 C_EnterGame 보낼 때 GameRoom 이 보장됨.
        record PendingGameStart(int RoomId, int Port, string Host, List<LobbySession> AllSessions);

        static readonly object _pendingLock = new();
        static readonly Dictionary<int, PendingGameStart> _pendingStarts = new();

        // PacketId -> 핸들러 함수 테이블
        // LobbySession.OnRecvPacket()에서 이 테이블을 조회해 호출
        public static Action<YPacketSession, ArraySegment<byte>>[] Handlers { get; private set; }

        static LobbyPacketHandler()
        {
            // 새 패킷 ID 추가 시 maxId 기준값도 갱신해야 함
            int maxId = (int)PacketId.SPlayerCharacterSelected + 1;
            Handlers = new Action<YPacketSession, ArraySegment<byte>>[maxId];

            Handlers[(int)PacketId.CConnected]       = Handle_C_Connected;
            Handlers[(int)PacketId.CCreateRoom]      = Handle_C_CreateRoom;
            Handlers[(int)PacketId.CJoinRoom]        = Handle_C_JoinRoom;
            Handlers[(int)PacketId.CLeaveRoom]       = Handle_C_LeaveRoom;
            Handlers[(int)PacketId.CGetRooms]        = Handle_C_GetRooms;
            Handlers[(int)PacketId.CStartGame]       = Handle_C_StartGame;
            Handlers[(int)PacketId.CSelectCharacter] = Handle_C_SelectCharacter;
        }

        // 핸들러 구현 ============================================================

        static void Handle_C_Connected(YPacketSession session, ArraySegment<byte> body)
        {
            var pkt = C_Connected.Parser.ParseFrom(body.Array, body.Offset, body.Count);
            Console.WriteLine($"[Lobby] C_Connected: name={pkt.PlayerName}");

            RoomManager.Instance.RegisterPlayer(session.SessionId, pkt.PlayerName);
            var res = new S_Connected { PlayerId = session.SessionId };
            session.Send(MakePacket(PacketId.SConnected, res));
        }

        static void Handle_C_CreateRoom(YPacketSession session, ArraySegment<byte> body)
        {
            var pkt = C_CreateRoom.Parser.ParseFrom(body.Array, body.Offset, body.Count);
            Console.WriteLine($"[Lobby] C_CreateRoom: name={pkt.RoomName}, max={pkt.MaxPlayers}");

            var result = RoomManager.Instance.CreateRoom((LobbySession)session, pkt.RoomName, pkt.MaxPlayers);
            if (!result.Ok)
            {
                session.Send(MakePacket(PacketId.SError, new S_Error { Code = 1, Message = result.Error }));
                return;
            }

            session.Send(MakePacket(PacketId.SRoomCreated, new S_RoomCreated { Room = result.Data.Room }));
        }

        static void Handle_C_JoinRoom(YPacketSession session, ArraySegment<byte> body)
        {
            var pkt = C_JoinRoom.Parser.ParseFrom(body.Array, body.Offset, body.Count);
            Console.WriteLine($"[Lobby] C_JoinRoom: roomId={pkt.RoomId}");

            var result = RoomManager.Instance.JoinRoom((LobbySession)session, pkt.RoomId);
            if (!result.Ok)
            {
                session.Send(MakePacket(PacketId.SError, new S_Error { Code = 2, Message = result.Error }));
                return;
            }

            // 입장한 본인에게 방 정보 + 현재 방 전체 멤버 전송
            var joinedRes = new S_RoomJoined { Room = result.Data.Room };
            joinedRes.Players.AddRange(result.Data.AllPlayers);
            session.Send(MakePacket(PacketId.SRoomJoined, joinedRes));

            // 기존 멤버들에게 새 플레이어 입장 알림
            var notifyBytes = MakePacket(PacketId.SPlayerJoined, new S_PlayerJoined { Player = result.Data.Joiner });
            foreach (var s in result.Data.Others)
                s.Send(notifyBytes);
        }

        static void Handle_C_LeaveRoom(YPacketSession session, ArraySegment<byte> body)
        {
            Console.WriteLine($"[Lobby] C_LeaveRoom");

            var result = RoomManager.Instance.LeaveRoom((LobbySession)session);
            if (!result.Ok) return;

            var notifyBytes = MakePacket(PacketId.SPlayerLeft, new S_PlayerLeft { PlayerId = result.Data.Leaver.PlayerId });
            foreach (var s in result.Data.Remaining)
                s.Send(notifyBytes);

            // 방장이 나갔으면 남은 플레이어들에게 새 방장 알림
            if (result.Data.NewCreatorId != -1)
            {
                var creatorBytes = MakePacket(PacketId.SCreatorChanged, new S_CreatorChanged { NewCreatorId = result.Data.NewCreatorId });
                foreach (var s in result.Data.Remaining)
                    s.Send(creatorBytes);
            }
        }

        static void Handle_C_GetRooms(YPacketSession session, ArraySegment<byte> body)
        {
            Console.WriteLine($"[Lobby] C_GetRooms");

            var res = new S_RoomList();
            res.Rooms.AddRange(RoomManager.Instance.GetAllRooms());
            session.Send(MakePacket(PacketId.SRoomList, res));
        }

        static void Handle_C_StartGame(YPacketSession session, ArraySegment<byte> body)
        {
            Console.WriteLine($"[Lobby] C_StartGame: sessionId={session.SessionId}");

            var result = RoomManager.Instance.StartGame((LobbySession)session);
            if (!result.Ok)
            {
                session.Send(MakePacket(PacketId.SError, new S_Error { Code = 3, Message = result.Error }));
                return;
            }

            if (GameServerSession.Instance == null)
            {
                Console.WriteLine($"[Lobby] C_StartGame 실패: GameServer 내부 채널 연결 없음");
                session.Send(MakePacket(PacketId.SError, new S_Error { Code = 3, Message = "게임서버 연결 없음" }));
                return;
            }

            // ack 기반 흐름: pending 보관 → L2G 전송 → G2L_RoomCreated 수신 시 FinalizeGameStart 에서 S_GameReady 송신.
            // 이렇게 해야 클라의 C_EnterGame 이 GameServer 에 도착할 때 룸 생성이 완료되어 있다 (race 방어).
            var pending = new PendingGameStart(
                result.Data.RoomId,
                result.Data.GamePort,
                LobbyConfig.Instance.GameServerHost,
                result.Data.AllSessions);
            lock (_pendingLock)
                _pendingStarts[result.Data.RoomId] = pending;

            var createPkt = InternalPacketHandler.MakePacket(
                InternalPacketId.L2GCreateRoom,
                new L2G_CreateRoom
                {
                    RoomId       = result.Data.RoomId,
                    PlayerCount  = result.Data.AllSessions.Count,
                    HostPlayerId = result.Data.HostPlayerId,
                });
            GameServerSession.Instance.Send(createPkt);

            // S_GameReady 송신은 G2L_RoomCreated 수신 후 FinalizeGameStart 에서.
        }

        // InternalPacketHandler.Handle_G2L_RoomCreated 에서 호출.
        // 보관해둔 pending 을 꺼내 클라들에게 S_GameReady 송신.
        public static void FinalizeGameStart(int roomId)
        {
            PendingGameStart pending;
            lock (_pendingLock)
            {
                if (!_pendingStarts.Remove(roomId, out pending))
                {
                    Console.WriteLine($"[Lobby] FinalizeGameStart 무시: pending 없음 roomId={roomId}");
                    return;
                }
            }

            Console.WriteLine($"[Lobby] S_GameReady 송신: roomId={roomId}, 수신자={pending.AllSessions.Count}");

            var readyBytes = MakePacket(PacketId.SGameReady, new S_GameReady
            {
                Port   = pending.Port,
                Host   = pending.Host,
                RoomId = pending.RoomId,
            });

            foreach (var s in pending.AllSessions)
                s.Send(readyBytes);
        }

        static void Handle_C_SelectCharacter(YPacketSession session, ArraySegment<byte> body)
        {
            var pkt = C_SelectCharacter.Parser.ParseFrom(body.Array, body.Offset, body.Count);
            Console.WriteLine($"[Lobby] C_SelectCharacter: sessionId={session.SessionId}, characterId={pkt.CharacterId}");

            var result = RoomManager.Instance.SelectCharacter((LobbySession)session, pkt.CharacterId);
            if (!result.Ok)
            {
                session.Send(MakePacket(PacketId.SError, new S_Error { Code = 4, Message = result.Error }));
                return;
            }

            // 방 전체에 변경 알림 (본인 포함 - 클라가 서버 확정값 받고 UI 갱신)
            var notifyBytes = MakePacket(PacketId.SPlayerCharacterSelected, new S_PlayerCharacterSelected
            {
                PlayerId    = result.Data.PlayerId,
                CharacterId = result.Data.CharacterId,
            });
            foreach (var s in result.Data.AllSessions)
                s.Send(notifyBytes);
        }

        // 공통 유틸 =====================================================================

        // protobuf 메시지 -> [size(2)][packetId(2)][body] 바이트 배열
        public static ArraySegment<byte> MakePacket(PacketId id, IMessage message)
        {
            byte[] body       = message.ToByteArray();
            ushort totalSize  = (ushort)(YPacketSession.HEADER_SIZE + body.Length);

            byte[] packet = new byte[totalSize];
            Array.Copy(BitConverter.GetBytes(totalSize), 0, packet, 0, 2);
            Array.Copy(BitConverter.GetBytes((ushort)id), 0, packet, 2, 2);
            Array.Copy(body, 0, packet, YPacketSession.HEADER_SIZE, body.Length);

            return new ArraySegment<byte>(packet);
        }
    }
}
