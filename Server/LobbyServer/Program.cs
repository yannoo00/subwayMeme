using System;
using System.Net;
using ServerCore;
using LobbyServer;

IPAddress ipAddr = IPAddress.Any;
int port = 7770;

IPEndPoint endPoint = new IPEndPoint(ipAddr, port);

Listener listener = new Listener();
listener.Init(endPoint, () => new LobbySession());

Console.WriteLine($"[LobbyServer] 포트 {port} 대기 중...");

// 메인 스레드가 종료되지 않도록 유지
// 실제 처리는 SAEA 콜백(I/O 스레드)이 담당
while (true)
{
    Console.ReadLine();
}
