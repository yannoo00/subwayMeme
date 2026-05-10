using System.Collections.Generic;
using LobbyProto;

namespace LobbyServer
{
    public class Room
    {
        public int RoomId { get; }
        public string RoomName { get; }
        public int MaxPlayers { get; }
        public int CreatorSessionId { get; private set; }

        // characterId == -1: 미선택(미준비), 0 이상: 선택 완료(준비)
        readonly Dictionary<int, (LobbySession session, string playerName, int characterId)> _players = new();

        public int PlayerCount => _players.Count;
        public bool IsFull => _players.Count >= MaxPlayers;

        public Room(int roomId, string roomName, int maxPlayers, int creatorSessionId)
        {
            RoomId = roomId;
            RoomName = roomName;
            MaxPlayers = maxPlayers;
            CreatorSessionId = creatorSessionId;
        }

        public bool TryAdd(LobbySession session, string playerName)
        {
            if (IsFull) return false;
            // 신규 입장은 항상 캐릭터 미선택 상태로 시작
            _players[session.SessionId] = (session, playerName, -1);
            return true;
        }

        public bool Remove(int sessionId) => _players.Remove(sessionId);

        // 캐릭터 선택/변경. 존재하지 않는 세션이면 false
        public bool SetCharacter(int sessionId, int characterId)
        {
            if (!_players.TryGetValue(sessionId, out var p)) return false;
            _players[sessionId] = (p.session, p.playerName, characterId);
            return true;
        }

        // 모든 플레이어가 캐릭터를 선택했는지 (게임 시작 가능 여부)
        public bool IsAllReady()
        {
            foreach (var (_, (_, _, cid)) in _players)
                if (cid < 0) return false;
            return _players.Count > 0;
        }

        // 방장이 나갔을 때 남은 플레이어 중 첫 번째에게 방장 위임
        // 위임할 대상이 없으면 -1 반환
        public int MigrateCreator()
        {
            foreach (var (sid, _) in _players)
            {
                CreatorSessionId = sid;
                return sid;
            }
            return -1;
        }

        public RoomInfo ToRoomInfo() => new RoomInfo
        {
            RoomId = RoomId,
            RoomName = RoomName,
            CurPlayers = PlayerCount,
            MaxPlayers = MaxPlayers,
        };

        public PlayerInfo GetPlayerInfo(int sessionId)
        {
            var (_, name, cid) = _players[sessionId];
            return new PlayerInfo { PlayerId = sessionId, PlayerName = name, CharacterId = cid };
        }

        // 방에 현재 있는 모든 플레이어의 PlayerInfo 목록
        public List<PlayerInfo> GetAllPlayerInfos()
        {
            var list = new List<PlayerInfo>();
            foreach (var (sid, (_, name, cid)) in _players)
                list.Add(new PlayerInfo { PlayerId = sid, PlayerName = name, CharacterId = cid });
            return list;
        }

        public List<LobbySession> GetOtherSessions(int excludeSessionId)
        {
            var list = new List<LobbySession>();
            foreach (var (sid, (s, _, _)) in _players)
                if (sid != excludeSessionId) list.Add(s);
            return list;
        }

        public List<LobbySession> GetAllSessions()
        {
            var list = new List<LobbySession>();
            foreach (var (_, (s, _, _)) in _players)
                list.Add(s);
            return list;
        }
    }
}
