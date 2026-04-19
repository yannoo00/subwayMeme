using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Microsoft.Extensions.Configuration;

namespace LobbyServer
{
    public class ProcessManager
    {
        public static readonly ProcessManager Instance = new();

        const int BASE_PORT = 7771;
        const int MAX_ROOMS = 50;

        public readonly string GameServerHost;
        readonly string _gameServerExePath;

        readonly object _lock = new();
        readonly HashSet<int> _usedPorts = new();

        ProcessManager()
        {
            var env = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Development";

            var config = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false)
                .Build();

            // DOTNET_ENVIRONMENT 값에 해당하는 섹션 읽기
            var section = config.GetSection(env);
            GameServerHost = section["GameServerHost"] ?? "127.0.0.1";
            string buildConfig = section["BuildConfig"] ?? "Debug";
            string exeName = OperatingSystem.IsWindows() ? "GameServer.exe" : "GameServer";

            _gameServerExePath = Path.GetFullPath(
                Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..",
                             "GameServer", "bin", buildConfig, "net10.0", exeName)
            );

            Console.WriteLine($"[ProcessManager] 환경: {env}");
            Console.WriteLine($"[ProcessManager] GameServer 경로: {_gameServerExePath}");
            Console.WriteLine($"[ProcessManager] GameServer 호스트: {GameServerHost}");
        }

        // GameServer 프로세스 spawn
        // 성공 시 할당된 포트 반환, 실패 시 null
        public int? Spawn(int roomId)
        {
            lock (_lock)
            {
                int port = AllocatePort();
                if (port == -1)
                {
                    Console.WriteLine("[ProcessManager] 사용 가능한 포트 없음"); 
                    return null;
                }

                try
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = _gameServerExePath,
                        Arguments = $"{port} {roomId}",
                        UseShellExecute = false,
                    };

                    Process.Start(psi);
                    Console.WriteLine($"[ProcessManager] GameServer 시작: port={port}, roomId={roomId}");
                    return port;
                }
                catch (Exception e)
                {
                    Console.WriteLine($"[ProcessManager] GameServer 시작 실패: {e.Message}");
                    _usedPorts.Remove(port);
                    return null;
                }
            }
        }

        // GameServer 종료 시 포트 반납
        public void Release(int port)
        {
            lock (_lock)
            {
                _usedPorts.Remove(port);
                Console.WriteLine($"[ProcessManager] 포트 반납: {port}");
            }
        }

        int AllocatePort()
        {
            for (int p = BASE_PORT; p < BASE_PORT + MAX_ROOMS; p++)
            {
                if (_usedPorts.Add(p))
                    return p;
            }
            return -1;
        }
    }
}
