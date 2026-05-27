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

        // GameServer 의 두 포트.
        // GameServer/Program.cs 의 CLIENT_PORT, INTERNAL_PORT 와 반드시 일치해야 한다.
        public int GameServerClientPort   { get; } = 7771;
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
