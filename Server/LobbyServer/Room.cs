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

        readonly Dictionary<int, (LobbySession session, string playerName)> _players = new();

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
            _players[session.SessionId] = (session, playerName);
            return true;
        }

        public bool Remove(int sessionId) => _players.Remove(sessionId);

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
            var (_, name) = _players[sessionId];
            return new PlayerInfo { PlayerId = sessionId, PlayerName = name };
        }

        // 방에 현재 있는 모든 플레이어의 PlayerInfo 목록
        public List<PlayerInfo> GetAllPlayerInfos()
        {
            var list = new List<PlayerInfo>();
            foreach (var (sid, (_, name)) in _players)
                list.Add(new PlayerInfo { PlayerId = sid, PlayerName = name });
            return list;
        }

        public List<LobbySession> GetOtherSessions(int excludeSessionId)
        {
            var list = new List<LobbySession>();
            foreach (var (sid, (s, _)) in _players)
                if (sid != excludeSessionId) list.Add(s);
            return list;
        }

        public List<LobbySession> GetAllSessions()
        {
            var list = new List<LobbySession>();
            foreach (var (_, (s, _)) in _players)
                list.Add(s);
            return list;
        }
    }
}
