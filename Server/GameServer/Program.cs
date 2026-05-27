using System;
using System.Net;
using ServerCore;
using GameServer;

// Phase 2: GameServer 는 더 이상 LobbyServer 가 spawn 하지 않는다.
// 별도 프로세스로 미리 떠 있으며, 두 개의 Listener 를 운영한다.
//   1) CLIENT_PORT  (7771)  - 클라이언트 접속용 (모든 인터페이스)
//   2) INTERNAL_PORT (7772) - LobbyServer 와의 서버 간 채널 (loopback 전용, 보안)
//
// 룸은 로비서버가 L2G_CreateRoom 패킷으로 생성 요청을 보낼 때마다 GameRoomManager 에 등록된다.
const int CLIENT_PORT   = 7771;
const int INTERNAL_PORT = 7772;

Console.WriteLine($"[GameServer] 시작");

// 클라이언트용 Listener (외부 노출)
IPEndPoint clientEndPoint = new IPEndPoint(IPAddress.Any, CLIENT_PORT);
Listener clientListener = new Listener();
clientListener.Init(clientEndPoint, () => new GameSession());
Console.WriteLine($"[GameServer] 클라이언트 포트 {CLIENT_PORT} 대기 중");

// 내부 채널용 Listener (loopback 만 - 외부에서 접근 불가)
IPEndPoint internalEndPoint = new IPEndPoint(IPAddress.Loopback, INTERNAL_PORT);
Listener internalListener = new Listener();
internalListener.Init(internalEndPoint, () => new InternalSession());
Console.WriteLine($"[GameServer] 내부 채널 포트 {INTERNAL_PORT} 대기 중 (loopback 전용)");

// 메인 스레드 유지. 실제 처리는 SAEA 콜백이 담당.
while (true)
{
    Console.ReadLine();
}
