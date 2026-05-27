using System;
using System.Net;
using ServerCore;

namespace LobbyServer
{
    // LobbyServer 가 GameServer 내부 채널(7772) 에 클라이언트로 접속할 때 사용하는 세션.
    // GameServer 쪽 수신 세션은 InternalSession 으로 한 쌍을 이룬다.
    // Lobby 프로세스 안에 한 번만 연결하므로 Send 호출용 static 참조를 보관.
    public class GameServerSession : PacketSession
    {
        public static GameServerSession Instance { get; private set; }

        public override void OnConnected(EndPoint endPoint)
        {
            Console.WriteLine($"[GameServerSession] 게임서버 연결 성공: {endPoint}");
            Instance = this;
        }

        public override void OnDisconnected(EndPoint endPoint)
        {
            Console.WriteLine($"[GameServerSession] 게임서버 연결 해제: {endPoint}");
            if (Instance == this) Instance = null;
            // TODO: 재연결 로직 (학습 프로젝트 범위 외)
        }

        public override void OnRecvPacket(ushort id, ArraySegment<byte> body)
        {
            if (id >= InternalPacketHandler.Handlers.Length)
            {
                Console.WriteLine($"[GameServerSession] 알 수 없는 PacketId: {id}");
                return;
            }

            var handler = InternalPacketHandler.Handlers[id];
            if (handler == null)
            {
                Console.WriteLine($"[GameServerSession] 핸들러 미등록 PacketId: {id}");
                return;
            }

            handler.Invoke(this, body);
        }

        public override void OnSend(int numOfBytes) { }
    }
}
