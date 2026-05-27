using System;
using System.Net;
using ServerCore;

namespace GameServer
{
    // LobbyServer 가 GameServer 내부 채널 포트(7772) 로 접속 시 생성되는 세션.
    // 클라용 GameSession 과 분리해 패킷 디스패치 테이블도 별도 사용한다.
    public class InternalSession : PacketSession
    {
        // GameServer 에서 LobbyServer 방향으로 송신할 때 사용.
        // 보통 LobbyServer 하나만 연결되므로 단일 참조로 충분.
        // 끊긴 후 재연결 시 갱신될 수 있도록 set 허용.
        public static InternalSession LobbyConnection { get; private set; }

        public override void OnConnected(EndPoint endPoint)
        {
            Console.WriteLine($"[InternalSession] 로비서버 연결: {endPoint} / SessionId: {SessionId}");
            LobbyConnection = this;
        }

        public override void OnDisconnected(EndPoint endPoint)
        {
            // 로비서버 연결이 끊겨도 진행 중인 게임은 영향 받지 않는다.
            // 재연결은 로비서버 측에서 책임진다.
            Console.WriteLine($"[InternalSession] 로비서버 연결 해제: {endPoint} / SessionId: {SessionId}");
            if (LobbyConnection == this) LobbyConnection = null;
        }

        public override void OnRecvPacket(ushort id, ArraySegment<byte> body)
        {
            if (id >= InternalPacketHandler.Handlers.Length)
            {
                Console.WriteLine($"[InternalSession] 알 수 없는 PacketId: {id}");
                return;
            }

            var handler = InternalPacketHandler.Handlers[id];
            if (handler == null)
            {
                Console.WriteLine($"[InternalSession] 핸들러 미등록 PacketId: {id}");
                return;
            }

            handler.Invoke(this, body);
        }

        public override void OnSend(int numOfBytes) { }
    }
}
