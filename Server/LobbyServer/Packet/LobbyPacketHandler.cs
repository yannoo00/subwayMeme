using System;
using Google.Protobuf;
using LobbyProto;
using ServerCore;

namespace LobbyServer
{
    //  패킷 핸들러 모음
    //  각 메서드는 PacketId 하나에 대응.
    //  실제 게임 로직은 여기서 호출한다.
    
    public class LobbyPacketHandler
    {
        // PacketId -> 핸들러 함수 테이블
        // LobbySession.OnRecvPacket()에서 이 테이블을 조회해 호출
        public static Action<PacketSession, ArraySegment<byte>>[] Handlers { get; private set; }

        static LobbyPacketHandler()
        {
            // PacketId 최대값 + 1 크기로 배열 초기화 (인덱스 = PacketId 값)
            int maxId = (int)PacketId.SError + 1;
            Handlers = new Action<PacketSession, ArraySegment<byte>>[maxId];

            Handlers[(int)PacketId.CConnected]  = Handle_C_Connected;
            Handlers[(int)PacketId.CCreateRoom] = Handle_C_CreateRoom;
            Handlers[(int)PacketId.CJoinRoom]   = Handle_C_JoinRoom;
            Handlers[(int)PacketId.CLeaveRoom]  = Handle_C_LeaveRoom;
            Handlers[(int)PacketId.CGetRooms]   = Handle_C_GetRooms;
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

            var res = new S_RoomCreated { Room = result.Data.Room };
            session.Send(MakePacket(PacketId.SRoomCreated, res));
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

            // 남은 멤버들에게 퇴장 알림
            var notifyBytes = MakePacket(PacketId.SPlayerLeft, new S_PlayerLeft { PlayerId = result.Data.Leaver.PlayerId });
            foreach (var s in result.Data.Remaining)
                s.Send(notifyBytes);
        }

        static void Handle_C_GetRooms(PacketSession session, ArraySegment<byte> body)
        {
            Console.WriteLine($"[Lobby] C_GetRooms");

            var res = new S_RoomList();
            res.Rooms.AddRange(RoomManager.Instance.GetAllRooms());
            session.Send(MakePacket(PacketId.SRoomList, res));
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
