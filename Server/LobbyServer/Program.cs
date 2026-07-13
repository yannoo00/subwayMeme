using System;
using System.Net;
using YannooNet;
using LobbyServer;

// === 로비 YListener (포트 7770) === 외부 클라이언트 접속용
const int LOBBY_PORT = 7770;
IPEndPoint lobbyEndPoint = new IPEndPoint(IPAddress.Any, LOBBY_PORT);

YListener lobbyListener = new YListener();
lobbyListener.Init(lobbyEndPoint, () => new LobbySession());
Console.WriteLine($"[LobbyServer] 포트 {LOBBY_PORT} 대기 중");

// === GameServer 내부 채널 접속 (loopback 7772) ===
// Phase 2: GameServer 는 별도 프로세스로 미리 떠 있어야 함.
// 연결 실패 시 에러만 출력하고 진행 (재연결 로직 미구현 - 학습 프로젝트 범위 외).
// 게임 시작 요청 시 GameServerSession.Instance 가 null 이면 클라에게 에러 응답.
var gameServerEndPoint = new IPEndPoint(
    IPAddress.Loopback,
    LobbyConfig.Instance.GameServerInternalPort);

YConnector gameServerConnector = new YConnector();
gameServerConnector.Connect(gameServerEndPoint, () => new GameServerSession());
Console.WriteLine($"[LobbyServer] GameServer 내부 채널 접속 시도: {gameServerEndPoint}");

// 메인 스레드 유지. 실제 처리는 SAEA 콜백이 담당.
while (true)
{
    Console.ReadLine();
}
