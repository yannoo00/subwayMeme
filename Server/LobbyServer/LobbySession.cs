using System;
using System.Net;
using LobbyProto;
using YannooNet;

namespace LobbyServer
{
    public class LobbySession : YPacketSession
    {
        public override void OnConnected(EndPoint endPoint)
        {
            Console.WriteLine($"[LobbySession] 접속: {endPoint} / SessionId: {SessionId}");
        }

        public override void OnDisconnected(EndPoint endPoint)
        {
            Console.WriteLine($"[LobbySession] 해제: {endPoint} / SessionId: {SessionId}");

            // 방에 있었다면 자동 퇴장 처리
            var result = RoomManager.Instance.LeaveRoom(this);
            if (result.Ok)
            {
                var notifyBytes = LobbyPacketHandler.MakePacket(PacketId.SPlayerLeft, new S_PlayerLeft { PlayerId = result.Data.Leaver.PlayerId });
                foreach (var s in result.Data.Remaining)
                    s.Send(notifyBytes);
            }

            RoomManager.Instance.UnregisterPlayer(SessionId);
        }

        // YPacketSession이 헤더를 파싱하고, 완성된 패킷 1개씩 여기로 올려줌
        public override void OnRecvPacket(ushort id, ArraySegment<byte> body)
        {
            // id 범위 체크
            if (id >= LobbyPacketHandler.Handlers.Length)
            {
                Console.WriteLine($"[LobbySession] 알 수 없는 PacketId: {id}");
                return;
            }

            var handler = LobbyPacketHandler.Handlers[id];
            if (handler == null)
            {
                Console.WriteLine($"[LobbySession] 핸들러 미등록 PacketId: {id}");
                return;
            }

            handler.Invoke(this, body);
        }

        public override void OnSend(int numOfBytes)
        {
            // 필요 시 로그 추가
        }
    }
}
