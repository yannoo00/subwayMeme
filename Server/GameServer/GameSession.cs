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

            // TODO: GameRoom에서 플레이어 제거, S_PlayerLeft 브로드캐스트
            // TODO: 호스트였으면 S_HostChanged 브로드캐��트
        }

        public override void OnRecvPacket(ushort id, ArraySegment<byte> body)
        {
            if (id >= GamePacketHandler.Handlers.Length)
            {
                Console.WriteLine($"[GameSession] 알 수 없는 PacketId: {id}");
                return;
            }

            var handler = GamePacketHandler.Handlers[id];
            if (handler == null)
            {
                Console.WriteLine($"[GameSession] 핸들러 미등록 PacketId: {id}");
                return;
            }

            handler.Invoke(this, body);
        }

        public override void OnSend(int numOfBytes) { }
    }
}
