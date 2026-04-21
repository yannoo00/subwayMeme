using System.Collections.Generic;
using LobbyProto;

namespace LobbyServer
{

    // 방 관리 관련 패킷 데이터
    public class RoomResult<T>
    {
        public bool Ok { get; init; }
        public string Error { get; init; }
        public T Data { get; init; }

        public static RoomResult<T> Success(T data) => new() { Ok = true, Data = data };
        public static RoomResult<T> Fail(string error) => new() { Ok = false, Error = error };
    }

    public record CreateRoomData(RoomInfo Room);
    public record JoinRoomData(RoomInfo Room, PlayerInfo Joiner, List<LobbySession> Others);
    // NewCreatorId: 방장이 나갔을 때 위임된 새 방장 ID (-1이면 위임 없음)
    public record LeaveRoomData(PlayerInfo Leaver, List<LobbySession> Remaining, int NewCreatorId = -1);
    public record StartGameData(int GamePort, int RoomId, List<LobbySession> AllSessions);




    public class RoomManager
    {
        public static readonly RoomManager Instance = new();

        readonly object _lock = new();
        readonly Dictionary<int, Room> _rooms = new();
        readonly Dictionary<int, (string playerName, int roomId)> _players = new();
        int _nextRoomId = 1;

        // C_Connected 수신 시 호출 - 플레이어 등록
        public void RegisterPlayer(int sessionId, string playerName)
        {
            lock (_lock)
                _players[sessionId] = (playerName, -1);
        }

        // 연결 해제 시 호출 - 플레이어 제거
        public void UnregisterPlayer(int sessionId)
        {
            lock (_lock)
                _players.Remove(sessionId);
        }

        // 방 생성
        public RoomResult<CreateRoomData> CreateRoom(LobbySession creator, string roomName, int maxPlayers)
        {
            lock (_lock)
            {
                if (!_players.TryGetValue(creator.SessionId, out var p) || p.roomId != -1)
                    return RoomResult<CreateRoomData>.Fail("이미 방에 있습니다.");

                var room = new Room(_nextRoomId++, roomName, maxPlayers, creator.SessionId);
                room.TryAdd(creator, p.playerName);
                _rooms[room.RoomId] = room;
                _players[creator.SessionId] = (p.playerName, room.RoomId);
                return RoomResult<CreateRoomData>.Success(new(room.ToRoomInfo()));
            }
        }

        // 방 참가
        public RoomResult<JoinRoomData> JoinRoom(LobbySession joiner, int roomId)
        {
            lock (_lock)
            {
                if (!_players.TryGetValue(joiner.SessionId, out var p) || p.roomId != -1)
                    return RoomResult<JoinRoomData>.Fail("이미 방에 있습니다.");
                if (!_rooms.TryGetValue(roomId, out var room) || room.IsFull)
                    return RoomResult<JoinRoomData>.Fail("방이 없거나 가득 찼습니다.");

                var others = room.GetAllSessions(); // 입장 전에 캡처
                room.TryAdd(joiner, p.playerName);
                _players[joiner.SessionId] = (p.playerName, roomId);
                return RoomResult<JoinRoomData>.Success(new(room.ToRoomInfo(), room.GetPlayerInfo(joiner.SessionId), others));
            }
        }

        // 방 퇴장
        public RoomResult<LeaveRoomData> LeaveRoom(LobbySession leaver)
        {
            lock (_lock)
            {
                if (!_players.TryGetValue(leaver.SessionId, out var p) || p.roomId == -1)
                    return RoomResult<LeaveRoomData>.Fail("방에 있지 않습니다.");

                var room = _rooms[p.roomId];
                var leaverInfo = room.GetPlayerInfo(leaver.SessionId);
                var remaining = room.GetOtherSessions(leaver.SessionId);
                bool wasCreator = (room.CreatorSessionId == leaver.SessionId);
                room.Remove(leaver.SessionId);
                _players[leaver.SessionId] = (p.playerName, -1);

                if (room.PlayerCount == 0)
                {
                    _rooms.Remove(room.RoomId);
                    return RoomResult<LeaveRoomData>.Success(new(leaverInfo, remaining));
                }

                // 방장이 나갔으면 새 방장 위임
                int newCreatorId = wasCreator ? room.MigrateCreator() : -1;
                return RoomResult<LeaveRoomData>.Success(new(leaverInfo, remaining, newCreatorId));
            }
        }

        // 게임 시작 (방장만 호출 가능)
        // ProcessManager로 GameServer 프로세스를 띄우고 포트와 전체 세션 목록을 반환
        public RoomResult<StartGameData> StartGame(LobbySession requester)
        {
            lock (_lock)
            {
                if (!_players.TryGetValue(requester.SessionId, out var p) || p.roomId == -1)
                    return RoomResult<StartGameData>.Fail("방에 있지 않습니다.");

                var room = _rooms[p.roomId];

                if (room.CreatorSessionId != requester.SessionId)
                    return RoomResult<StartGameData>.Fail("방장만 게임을 시작할 수 있습니다.");

                int? port = ProcessManager.Instance.Spawn(room.RoomId, room.PlayerCount);
                if (!port.HasValue)
                    return RoomResult<StartGameData>.Fail("게임 서버를 시작하지 못했습니다.");

                var allSessions = room.GetAllSessions();
                return RoomResult<StartGameData>.Success(new(port.Value, room.RoomId, allSessions));
            }
        }

        // 전체 방 목록 반환 (S_RoomList 용)
        public List<RoomInfo> GetAllRooms()
        {
            lock (_lock)
            {
                var list = new List<RoomInfo>();
                foreach (var room in _rooms.Values)
                    list.Add(room.ToRoomInfo());
                return list;
            }
        }
    }
}
