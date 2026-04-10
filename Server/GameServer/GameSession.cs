using System;
using System.Net;
using GameProto;
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

            var result = GameRoom.Instance.Remove(SessionId);
            if (result.Leaver == null) return;

            // 나머지 플레이어에게 퇴장 알림
            var leftBytes = GamePacketHandler.MakePacket(
                GamePacketId.SPlayerLeft,
                new S_PlayerLeft { PlayerId = result.Leaver.PlayerId });
            foreach (var s in result.Remaining)
                s.Send(leftBytes);

            // 호스트가 나갔으면 새 호스트 지정 알림
            if (result.NewHostPlayerId != -1)
            {
                Console.WriteLine($"[GameSession] 호스트 이탈 -> 새 호스트: playerId={result.NewHostPlayerId}");
                var hostBytes = GamePacketHandler.MakePacket(
                    GamePacketId.SHostChanged,
                    new S_HostChanged { NewHostId = result.NewHostPlayerId });
                foreach (var s in result.Remaining)
                    s.Send(hostBytes);
            }
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
