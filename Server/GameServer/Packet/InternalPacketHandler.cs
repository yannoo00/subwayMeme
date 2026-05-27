using System;
using Google.Protobuf;
using InternalProto;
using ServerCore;

namespace GameServer
{
    // 서버 간 내부 채널 패킷 디스패처.
    // 외부 클라이언트와 격리되어 있고 (loopback 바인딩),
    // 패킷 ID 공간도 GameProto 와 별도이므로 충돌하지 않는다.
    public class InternalPacketHandler
    {
        public static Action<PacketSession, ArraySegment<byte>>[] Handlers { get; private set; }

        static InternalPacketHandler()
        {
            int maxId = (int)InternalPacketId.G2LRoomEnded + 1;
            Handlers = new Action<PacketSession, ArraySegment<byte>>[maxId];

            Handlers[(int)InternalPacketId.L2GCreateRoom] = Handle_L2G_CreateRoom;
        }

        // 로비서버로부터 룸 생성 요청 수신.
        // 기존 ProcessManager.Spawn 의 자리를 대체한다.
        static void Handle_L2G_CreateRoom(PacketSession session, ArraySegment<byte> body)
        {
            var pkt = L2G_CreateRoom.Parser.ParseFrom(body.Array, body.Offset, body.Count);
            Console.WriteLine($"[Internal] L2G_CreateRoom: roomId={pkt.RoomId}, playerCount={pkt.PlayerCount}, hostPlayerId={pkt.HostPlayerId}");

            GameRoomManager.Instance.CreateRoom(pkt.RoomId, pkt.PlayerCount, pkt.HostPlayerId);
        }

        // protobuf 메시지 -> [size(2)][packetId(2)][body] 바이트 배열
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
