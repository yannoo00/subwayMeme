using System;
using Microsoft.Extensions.Configuration;

namespace LobbyServer
{
    // 옛 ProcessManager 가 갖고 있던 설정 역할 분리.
    // appsettings.json 의 환경별 섹션에서 GameServer 호스트를 읽어온다.
    public class LobbyConfig
    {
        public static readonly LobbyConfig Instance = new();

        // 클라이언트에게 알려줄 GameServer 호스트 (S_GameReady.Host 에 채워짐).
        // 클라가 LAN/WAN 어디서 접속하느냐에 따라 다른 값을 써야 하므로 설정 분리.
        public string GameServerHost { get; }

        // 클라이언트가 게임서버에 접속할 때 사용할 포트 (S_GameReady 에 실려 클라에 전달).
        // WebGL/WebSocket 전환 후엔 websockify 프록시 포트(8771) 를 알려준다.
        // 실제 GameServer 의 TCP 바인드 포트는 7771 (GameServer/Program.cs 의 CLIENT_PORT) — websockify 가 변환.
        public int GameServerClientPort   { get; } = 8771;

        // LobbyServer 가 GameServer 내부 채널에 접속할 때 쓰는 포트. 서버 간 직접 TCP 라 프록시 불필요.
        // GameServer/Program.cs 의 INTERNAL_PORT 와 반드시 일치.
        public int GameServerInternalPort { get; } = 7772;

        LobbyConfig()
        {
            var env = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Development";

            var config = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false)
                .Build();

            var section = config.GetSection(env);
            GameServerHost = section["GameServerHost"] ?? "127.0.0.1";

            Console.WriteLine($"[LobbyConfig] 환경: {env}, GameServer: {GameServerHost}:{GameServerClientPort} (클라용) / :{GameServerInternalPort} (내부)");
        }
    }
}
