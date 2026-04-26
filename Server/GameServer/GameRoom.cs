using System;
using System.Collections.Generic;
using System.Linq;

namespace GameServer
{
    // ==== 결과 레코드 ============================================================

    public record RemoveResult(GamePlayer Leaver, List<GameSession> Remaining, int NewHostPlayerId);
    public record ReadyResult(bool AllReady, int Seed, List<GameSession> AllSessions);
    public record ExitResult(int ExitedCount, int Total, List<GameSession> Others, bool AllExited);
    public record BoardResult(int BoardedCount, int Total, List<GameSession> Others, bool Trigger, int NodeIndex);
    public record RouteResult(bool IsHost, bool Trigger, int NodeIndex, List<GameSession> AllSessions);
    public record DamageResult(GamePlayer Player, int CurrentHp, bool IsDead);

    // ============================================================================

    public class GameRoom
    {
        public static readonly GameRoom Instance = new();

        readonly object _lock = new();
        readonly Dictionary<int, GamePlayer> _players = new(); // key: SessionId
        int _expectedCount = 0; // 로비에서 확정된 참가 인원 (Program.cs에서 Init으로 설정)
        int _hostPlayerId  = -1; // 로비 방장의 PlayerId (접속 순서와 무관하게 호스트 결정)

        // 스테이지 진행 상태 (하차/탑승 카운터는 역마다 리셋)
        readonly HashSet<int> _exitedIds  = new();
        readonly HashSet<int> _boardedIds = new();
        bool _routeSelected = false;
        int  _selectedNode  = -1;


        // ==== 초기화 =============================================================

        // Program.cs에서 GameServer 시작 직후 호출
        public void Init(int expectedCount, int hostPlayerId)
        {
            lock (_lock)
            {
                _expectedCount = expectedCount;
                _hostPlayerId  = hostPlayerId;
            }
        }


        // ==== 플레이어 관리 ======================================================

        // 입장: 로비 방장(hostPlayerId)이 호스트
        public GamePlayer Add(GameSession session, int playerId, string playerName)
        {
            lock (_lock)
            {
                bool isHost = (playerId == _hostPlayerId);
                var player  = new GamePlayer(session, playerId, playerName, isHost);
                _players[session.SessionId] = player;
                return player;
            }
        }

        // 퇴장: 호스트 이탈 시 남은 플레이어 중 첫 번째로 migration
        public RemoveResult Remove(int sessionId)
        {
            lock (_lock)
            {
                if (!_players.TryGetValue(sessionId, out var leaver))
                    return new RemoveResult(null, new List<GameSession>(), -1);

                bool wasHost = leaver.IsHost;
                _players.Remove(sessionId);

                var remaining   = GetAllSessionsUnsafe();
                int newHostId   = -1;

                if (wasHost && _players.Count > 0)
                {
                    var newHost = _players.Values.First();
                    newHost.IsHost = true;
                    newHostId      = newHost.PlayerId;
                }

                return new RemoveResult(leaver, remaining, newHostId);
            }
        }

        public GamePlayer Get(int sessionId)
        {
            lock (_lock)
            {
                _players.TryGetValue(sessionId, out var p);
                return p;
            }
        }

        public List<GamePlayer> GetAllPlayers()
        {
            lock (_lock) return _players.Values.ToList();
        }

        public List<GameSession> GetAllSessions()
        {
            lock (_lock) return GetAllSessionsUnsafe();
        }

        public List<GameSession> GetOtherSessions(int excludeSessionId)
        {
            lock (_lock) return GetOtherSessionsUnsafe(excludeSessionId);
        }


        // ==== 게임 시작 준비 =====================================================

        // 준비: 전원 입장 + 전원 ready 시에만 seed 생성
        public ReadyResult MarkReady(int sessionId)
        {
            lock (_lock)
            {
                if (!_players.TryGetValue(sessionId, out var player) || player.IsReady)
                    return new ReadyResult(false, 0, null);

                player.IsReady = true;

                // 아직 입장하지 않은 플레이어가 있으면 대기
                bool allEntered = _players.Count == _expectedCount;
                bool allReady   = _players.Values.All(p => p.IsReady);
                if (!allEntered || !allReady)
                    return new ReadyResult(false, 0, null);

                int seed = new Random().Next(1, int.MaxValue);
                return new ReadyResult(true, seed, GetAllSessionsUnsafe());
            }
        }


        // ==== 스테이지 진행 ======================================================

        // 하차 요청
        public ExitResult MarkExited(int sessionId)
        {
            lock (_lock)
            {
                if (!_players.ContainsKey(sessionId))
                    return new ExitResult(0, 0, null, false);

                _exitedIds.Add(sessionId);
                int  total     = _players.Count;
                bool allExited = _exitedIds.Count >= total;
                return new ExitResult(_exitedIds.Count, total, GetOtherSessionsUnsafe(sessionId), allExited);
            }
        }

        // 탑승 요청
        public BoardResult MarkBoarded(int sessionId)
        {
            lock (_lock)
            {
                if (!_players.ContainsKey(sessionId))
                    return new BoardResult(0, 0, null, false, -1);

                _boardedIds.Add(sessionId);
                int  total   = _players.Count;
                bool trigger = _boardedIds.Count >= total && _routeSelected;
                return new BoardResult(_boardedIds.Count, total, GetOtherSessionsUnsafe(sessionId), trigger, _selectedNode);
            }
        }

        // 경로 선택 (방장만)
        public RouteResult SelectRoute(int sessionId, int nodeIndex)
        {
            lock (_lock)
            {
                if (!_players.TryGetValue(sessionId, out var player) || !player.IsHost)
                    return new RouteResult(false, false, -1, null);

                _routeSelected = true;
                _selectedNode  = nodeIndex;
                bool trigger   = _boardedIds.Count >= _players.Count;
                return new RouteResult(true, trigger, nodeIndex, GetAllSessionsUnsafe());
            }
        }

        // 다음 역으로 이동 시 하차/탑승 카운터 리셋
        public void ResetStageState()
        {
            lock (_lock)
            {
                _exitedIds.Clear();
                _boardedIds.Clear();
                _routeSelected = false;
                _selectedNode  = -1;
            }
        }


        // ==== 전투 ===============================================================

        // 플레이어 피해 적용 (서버 권위)
        public DamageResult ApplyPlayerDamage(int targetPlayerId, int damage)
        {
            lock (_lock)
            {
                foreach (var p in _players.Values)
                {
                    if (p.PlayerId != targetPlayerId) continue;
                    p.Hp = Math.Max(0, p.Hp - damage);
                    return new DamageResult(p, p.Hp, p.Hp <= 0);
                }
                return new DamageResult(null, 0, false);
            }
        }


        // ==== 내부 헬퍼 (반드시 lock 안에서만 호출) ==============================

        List<GameSession> GetAllSessionsUnsafe()
            => _players.Values.Select(p => p.Session).ToList();

        List<GameSession> GetOtherSessionsUnsafe(int excludeSessionId)
            => _players.Values.Where(p => p.SessionId != excludeSessionId).Select(p => p.Session).ToList();
    }
}
