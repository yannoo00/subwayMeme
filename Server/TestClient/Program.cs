using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

string host = "127.0.0.1";
int port = 7770;

TcpClient client = new TcpClient();
client.Connect(new IPEndPoint(IPAddress.Parse(host), port));
Console.WriteLine($"[TestClient] {host}:{port} 접속 완료");

NetworkStream stream = client.GetStream();

// 수신 스레드: 서버에서 오는 데이터를 계속 출력
Thread recvThread = new Thread(() =>
{
    byte[] buf = new byte[1024];
    while (true)
    {
        int read = stream.Read(buf, 0, buf.Length);
        if (read == 0) break; // 서버가 연결 끊음
        Console.WriteLine($"[TestClient] 수신: {Encoding.UTF8.GetString(buf, 0, read)}");
    }
});
recvThread.IsBackground = true;
recvThread.Start();

// 메인 스레드: 콘솔 입력을 서버로 전송
Console.WriteLine("[TestClient] 메시지 입력 후 Enter (종료: q)");
while (true)
{
    string input = Console.ReadLine();
    if (input == "q") break;

    byte[] data = Encoding.UTF8.GetBytes(input);
    stream.Write(data, 0, data.Length);
}

client.Close();
Console.WriteLine("[TestClient] 연결 종료");
