using System;
using Google.Protobuf;
using InternalProto;
using ServerCore;

namespace LobbyServer
{
    // LobbyServer 측 서버 간 내부 채널 패킷 디스패처.
    // GameServer 로부터 들어오는 G2L_ 패킷을 처리.
    public class InternalPacketHandler
    {
        public static Action<PacketSession, ArraySegment<byte>>[] Handlers { get; private set; }

        static InternalPacketHandler()
        {
            int maxId = (int)InternalPacketId.G2LRoomCreated + 1;
            Handlers = new Action<PacketSession, ArraySegment<byte>>[maxId];

            Handlers[(int)InternalPacketId.G2LRoomEnded]   = Handle_G2L_RoomEnded;
            Handlers[(int)InternalPacketId.G2LRoomCreated] = Handle_G2L_RoomCreated;
        }

        // 게임서버로부터 룸 종료 알림 수신.
        // 보통 게임 시작 시 모든 플레이어가 로비에서 disconnect 하므로 LeaveRoom 누적으로 이미 비어있을 가능성이 높음.
        // 방어적으로 한 번 더 정리.
        static void Handle_G2L_RoomEnded(PacketSession session, ArraySegment<byte> body)
        {
            var pkt = G2L_RoomEnded.Parser.ParseFrom(body.Array, body.Offset, body.Count);
            Console.WriteLine($"[Internal] G2L_RoomEnded: roomId={pkt.RoomId}");
            RoomManager.Instance.RemoveRoom(pkt.RoomId);
        }

        // 룸 생성 완료 ack 수신.
        // Handle_C_StartGame 에서 보관해둔 pending 정보를 꺼내 클라들에게 S_GameReady 송신.
        static void Handle_G2L_RoomCreated(PacketSession session, ArraySegment<byte> body)
        {
            var pkt = G2L_RoomCreated.Parser.ParseFrom(body.Array, body.Offset, body.Count);
            Console.WriteLine($"[Internal] G2L_RoomCreated: roomId={pkt.RoomId}");
            LobbyPacketHandler.FinalizeGameStart(pkt.RoomId);
        }

        public static ArraySegment<byte> MakePacket(InternalPacketId id, IMessage message)
        {
            byte[] body      = message.ToByteArray();
            ushort totalSize = (ushort)(PacketSession.HEADER_SIZE + body.Length);

            byte[] packet = new byte[totalSize];
            Array.Copy(BitConverter.GetBytes(totalSize), 0, packet, 0, 2);
            Array.Copy(BitConverter.GetBytes((ushort)id), 0, packet, 2, 2);
            Array.Copy(body, 0, packet, PacketSession.HEADER_SIZE, body.Length);

            return new ArraySegment<byte>(packet);
        }
    }
}
