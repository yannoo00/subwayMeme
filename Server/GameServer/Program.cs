using System;
using System.Net;
using ServerCore;
using GameServer;

// LobbyServer가 Process.Start("GameServer.exe", "{port} {roomId} {playerCount} {hostPlayerId}") 로 실행
if (args.Length < 4
    || !int.TryParse(args[0], out int port)
    || !int.TryParse(args[1], out int roomId)
    || !int.TryParse(args[2], out int playerCount)
    || !int.TryParse(args[3], out int hostPlayerId))
{
    Console.WriteLine("[GameServer] 인자 오류. 사용법: GameServer.exe <port> <roomId> <playerCount> <hostPlayerId>");
    return;
}

Console.WriteLine($"[GameServer] 시작: port={port}, roomId={roomId}, playerCount={playerCount}, hostPlayerId={hostPlayerId}");
GameRoom.Instance.Init(playerCount, hostPlayerId);

IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, port);

Listener listener = new Listener();
listener.Init(endPoint, () => new GameSession());

Console.WriteLine($"[GameServer] 포트 {port} 대기 중...");

while (true)
{
    Console.ReadLine();
}
