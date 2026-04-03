using System;
using System.Net;
using ServerCore;

namespace GameServer
{
    public class GameSession : PacketSession
    {
        public override void OnConnected(EndPoint endPoint)
        {
            Console.WriteLine($"[GameSession] 접속: {endPoint} / SessionId: {SessionId}");
        }

        public override void OnDisconnected(EndPoint endPoint)
        {
            Console.WriteLine($"[GameSession] 해제: {endPoint} / SessionId: {SessionId}");
        }

        public override void OnRecvPacket(ushort id, ArraySegment<byte> body)
        {
            // 4단계에서 GamePacketHandler 연결 예정
            Console.WriteLine($"[GameSession] 패킷 수신: id={id}, size={body.Count}");
        }

        public override void OnSend(int numOfBytes) { }
    }
}
