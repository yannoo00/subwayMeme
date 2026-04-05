using System;
using System.Net;
using ServerCore;
using GameServer;

// LobbyServer가 Process.Start("GameServer.exe", "{port} {roomId}") 로 실행
if (args.Length < 2 || !int.TryParse(args[0], out int port) || !int.TryParse(args[1], out int roomId))
{
    Console.WriteLine("[GameServer] 인자 오류. 사용법: GameServer.exe <port> <roomId>");
    return;
}

Console.WriteLine($"[GameServer] 시작: port={port}, roomId={roomId}");

IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, port);

Listener listener = new Listener();
listener.Init(endPoint, () => new GameSession());

Console.WriteLine($"[GameServer] 포트 {port} 대기 중...");

while (true)
{
    Console.ReadLine();
}
