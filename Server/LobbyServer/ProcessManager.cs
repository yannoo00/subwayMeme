using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace LobbyServer
{
    public class ProcessManager
    {
        public static readonly ProcessManager Instance = new();

        // 개발 환경 기준 경로: LobbyServer/bin/Debug/net9.0/ 에서 4단계 상위 -> Server/ -> GameServer/bin/Debug/net9.0/
        // 배포 시 이 경로를 환경에 맞게 수정
        static readonly string GameServerExePath = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "GameServer", "bin", "Debug", "net9.0", "GameServer.exe")
        );

        const int BASE_PORT = 7771;
        const int MAX_ROOMS = 50;

        // GameServer가 접속하는 호스트 (클라이언트에게 전달할 주소)
        // 로컬 테스트: "127.0.0.1", 실제 서버: 서버 공인 IP로 변경
        public const string GameServerHost = "127.0.0.1";

        readonly object _lock = new();
        readonly HashSet<int> _usedPorts = new();

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
                        FileName = GameServerExePath,
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

        // GameServer 종료 시 포트 반납 (GameServer가 Lobby HTTP로 종료 보고 시 호출 예정)
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
