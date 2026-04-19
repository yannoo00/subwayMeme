using System;
using Google.Protobuf;
using LobbyProto;
using ServerCore;

namespace LobbyServer
{
    public class LobbyPacketHandler
    {
        // PacketId -> 핸들러 함수 테이블
        // LobbySession.OnRecvPacket()에서 이 테이블을 조회해 호출
        public static Action<PacketSession, ArraySegment<byte>>[] Handlers { get; private set; }

        static LobbyPacketHandler()
        {
            int maxId = (int)PacketId.SError + 1;
            Handlers = new Action<PacketSession, ArraySegment<byte>>[maxId];

            Handlers[(int)PacketId.CConnected]  = Handle_C_Connected;
            Handlers[(int)PacketId.CCreateRoom] = Handle_C_CreateRoom;
            Handlers[(int)PacketId.CJoinRoom]   = Handle_C_JoinRoom;
            Handlers[(int)PacketId.CLeaveRoom]  = Handle_C_LeaveRoom;
            Handlers[(int)PacketId.CGetRooms]   = Handle_C_GetRooms;
            Handlers[(int)PacketId.CStartGame]  = Handle_C_StartGame;
        }

        // 핸들러 구현 ============================================================

        static void Handle_C_Connected(PacketSession session, ArraySegment<byte> body)
        {
            var pkt = C_Connected.Parser.ParseFrom(body.Array, body.Offset, body.Count);
            Console.WriteLine($"[Lobby] C_Connected: name={pkt.PlayerName}");

            RoomManager.Instance.RegisterPlayer(session.SessionId, pkt.PlayerName);
            var res = new S_Connected { PlayerId = session.SessionId };
            session.Send(MakePacket(PacketId.SConnected, res));
        }

        static void Handle_C_CreateRoom(PacketSession session, ArraySegment<byte> body)
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

        static void Handle_C_JoinRoom(PacketSession session, ArraySegment<byte> body)
        {
            var pkt = C_JoinRoom.Parser.ParseFrom(body.Array, body.Offset, body.Count);
            Console.WriteLine($"[Lobby] C_JoinRoom: roomId={pkt.RoomId}");

            var result = RoomManager.Instance.JoinRoom((LobbySession)session, pkt.RoomId);
            if (!result.Ok)
            {
                session.Send(MakePacket(PacketId.SError, new S_Error { Code = 2, Message = result.Error }));
                return;
            }

            // 입장한 본인에게 방 정보 전송
            session.Send(MakePacket(PacketId.SRoomCreated, new S_RoomCreated { Room = result.Data.Room }));

            // 기존 멤버들에게 새 플레이어 입장 알림
            var notifyBytes = MakePacket(PacketId.SPlayerJoined, new S_PlayerJoined { Player = result.Data.Joiner });
            foreach (var s in result.Data.Others)
                s.Send(notifyBytes);
        }

        static void Handle_C_LeaveRoom(PacketSession session, ArraySegment<byte> body)
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

        static void Handle_C_GetRooms(PacketSession session, ArraySegment<byte> body)
        {
            Console.WriteLine($"[Lobby] C_GetRooms");

            var res = new S_RoomList();
            res.Rooms.AddRange(RoomManager.Instance.GetAllRooms());
            session.Send(MakePacket(PacketId.SRoomList, res));
        }

        static void Handle_C_StartGame(PacketSession session, ArraySegment<byte> body)
        {
            Console.WriteLine($"[Lobby] C_StartGame: sessionId={session.SessionId}");

            var result = RoomManager.Instance.StartGame((LobbySession)session);
            if (!result.Ok)
            {
                session.Send(MakePacket(PacketId.SError, new S_Error { Code = 3, Message = result.Error }));
                return;
            }

            // 방의 모든 플레이어에게 게임 서버 포트 전달
            var readyBytes = MakePacket(PacketId.SGameReady, new S_GameReady
            {
                Port   = result.Data.GamePort,
                Host   = ProcessManager.Instance.GameServerHost,
                RoomId = result.Data.RoomId,
            });

            foreach (var s in result.Data.AllSessions)
                s.Send(readyBytes);
        }

        // 공통 유틸 =====================================================================

        // protobuf 메시지 -> [size(2)][packetId(2)][body] 바이트 배열
        public static ArraySegment<byte> MakePacket(PacketId id, IMessage message)
        {
            byte[] body       = message.ToByteArray();
            ushort totalSize  = (ushort)(PacketSession.HEADER_SIZE + body.Length);

            byte[] packet = new byte[totalSize];
            Array.Copy(BitConverter.GetBytes(totalSize), 0, packet, 0, 2);
            Array.Copy(BitConverter.GetBytes((ushort)id), 0, packet, 2, 2);
            Array.Copy(body, 0, packet, PacketSession.HEADER_SIZE, body.Length);

            return new ArraySegment<byte>(packet);
        }
    }
}
