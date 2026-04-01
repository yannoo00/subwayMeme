using System.Collections.Generic;
using LobbyProto;

namespace LobbyServer
{
    public class Room
    {
        public int RoomId { get; }
        public string RoomName { get; }
        public int MaxPlayers { get; }

        readonly Dictionary<int, (LobbySession session, string playerName)> _players = new();

        public int PlayerCount => _players.Count;
        public bool IsFull => _players.Count >= MaxPlayers;

        public Room(int roomId, string roomName, int maxPlayers)
        {
            RoomId = roomId;
            RoomName = roomName;
            MaxPlayers = maxPlayers;
        }

        public bool TryAdd(LobbySession session, string playerName)
        {
            if (IsFull) return false;
            _players[session.SessionId] = (session, playerName);
            return true;
        }

        public bool Remove(int sessionId) => _players.Remove(sessionId);

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

        // 특정 세션을 제외한 나머지 세션 목록
        public List<LobbySession> GetOtherSessions(int excludeSessionId)
        {
            var list = new List<LobbySession>();
            foreach (var (sid, (s, _)) in _players)
                if (sid != excludeSessionId) list.Add(s);
            return list;
        }

        // 모든 세션 목록 (입장 알림을 기존 멤버에게 보낼 때 입장 전에 캡처)
        public List<LobbySession> GetAllSessions()
        {
            var list = new List<LobbySession>();
            foreach (var (_, (s, _)) in _players)
                list.Add(s);
            return list;
        }
    }
}
