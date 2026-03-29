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
        // PacketId → 핸들러 함수 테이블
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

            // TODO: 플레이어 등록, ID 발급
            var res = new S_Connected { PlayerId = session.SessionId };
            session.Send(MakePacket(PacketId.SConnected, res));
        }

        static void Handle_C_CreateRoom(PacketSession session, ArraySegment<byte> body)
        {
            var pkt = C_CreateRoom.Parser.ParseFrom(body.Array, body.Offset, body.Count);
            Console.WriteLine($"[Lobby] C_CreateRoom: name={pkt.RoomName}, max={pkt.MaxPlayers}");

            // TODO: RoomManager에 방 생성 요청
        }

        static void Handle_C_JoinRoom(PacketSession session, ArraySegment<byte> body)
        {
            var pkt = C_JoinRoom.Parser.ParseFrom(body.Array, body.Offset, body.Count);
            Console.WriteLine($"[Lobby] C_JoinRoom: roomId={pkt.RoomId}");

            // TODO: RoomManager에 방 참가 요청
        }

        static void Handle_C_LeaveRoom(PacketSession session, ArraySegment<byte> body)
        {
            Console.WriteLine($"[Lobby] C_LeaveRoom");

            // TODO: 현재 방에서 퇴장
        }

        static void Handle_C_GetRooms(PacketSession session, ArraySegment<byte> body)
        {
            Console.WriteLine($"[Lobby] C_GetRooms");

            // TODO: RoomManager에서 목록 가져와 S_RoomList 전송
            var res = new S_RoomList();
            session.Send(MakePacket(PacketId.SRoomList, res));
        }

        // 공통 유틸 =====================================================================

        // protobuf 메시지 → [size(2)][packetId(2)][body] 바이트 배열
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
