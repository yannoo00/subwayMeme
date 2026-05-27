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
    public record JoinRoomData(RoomInfo Room, PlayerInfo Joiner, List<PlayerInfo> AllPlayers, List<LobbySession> Others);
    // NewCreatorId: 방장이 나갔을 때 위임된 새 방장 ID (-1이면 위임 없음)
    public record LeaveRoomData(PlayerInfo Leaver, List<LobbySession> Remaining, int NewCreatorId = -1);
    public record StartGameData(int GamePort, int RoomId, List<LobbySession> AllSessions, int HostPlayerId);
    public record SelectCharacterData(int PlayerId, int CharacterId, List<LobbySession> AllSessions);




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

                var others = room.GetAllSessions(); // 입장 전에 캡처 (기존 멤버만)
                room.TryAdd(joiner, p.playerName);
                _players[joiner.SessionId] = (p.playerName, roomId);
                return RoomResult<JoinRoomData>.Success(new(
                    room.ToRoomInfo(),
                    room.GetPlayerInfo(joiner.SessionId),
                    room.GetAllPlayerInfos(),   // 참가 후 전체 멤버 (본인 포함)
                    others));
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
        // Phase 2: 프로세스 spawn 안 함. 클라가 접속할 포트(고정값) + 룸 ID + 세션 목록을 반환.
        // 실제 룸 생성 요청은 호출부가 L2G_CreateRoom 으로 GameServer 에 보낸다.
        public RoomResult<StartGameData> StartGame(LobbySession requester)
        {
            lock (_lock)
            {
                if (!_players.TryGetValue(requester.SessionId, out var p) || p.roomId == -1)
                    return RoomResult<StartGameData>.Fail("방에 있지 않습니다.");

                var room = _rooms[p.roomId];

                if (room.CreatorSessionId != requester.SessionId)
                    return RoomResult<StartGameData>.Fail("방장만 게임을 시작할 수 있습니다.");

                // 캐릭터 미선택 = 미준비. 전원 선택 완료여야 게임 시작 가능
                if (!room.IsAllReady())
                    return RoomResult<StartGameData>.Fail("아직 캐릭터를 선택하지 않은 플레이어가 있습니다.");

                // Phase 2: 프로세스 spawn 제거. GameServer 는 미리 떠 있고,
                // 룸 생성은 호출부(LobbyPacketHandler) 가 L2G_CreateRoom 패킷으로 요청한다.
                // 여기서는 클라에게 전달할 데이터만 구성. 포트는 고정값.
                var allSessions = room.GetAllSessions();
                return RoomResult<StartGameData>.Success(new(
                    LobbyConfig.Instance.GameServerClientPort,
                    room.RoomId,
                    allSessions,
                    room.CreatorSessionId));
            }
        }

        // 캐릭터 선택/변경 (대기방 안에서만 가능)
        // 동일한 characterId로 다시 선택해도 OK (재전송 idempotent)
        public RoomResult<SelectCharacterData> SelectCharacter(LobbySession requester, int characterId)
        {
            lock (_lock)
            {
                if (!_players.TryGetValue(requester.SessionId, out var p) || p.roomId == -1)
                    return RoomResult<SelectCharacterData>.Fail("방에 있지 않습니다.");

                var room = _rooms[p.roomId];
                if (!room.SetCharacter(requester.SessionId, characterId))
                    return RoomResult<SelectCharacterData>.Fail("플레이어가 방에 없습니다.");

                return RoomResult<SelectCharacterData>.Success(
                    new(requester.SessionId, characterId, room.GetAllSessions()));
            }
        }

        // G2L_RoomEnded 수신 시 방어적 룸 제거.
        // 정상 흐름에서는 게임 시작 시 모든 플레이어가 disconnect 하면서 LeaveRoom 누적으로
        // 이미 _rooms 에서 빠져 있을 가능성이 높지만, 혹시 남아있다면 여기서 강제 제거.
        public void RemoveRoom(int roomId)
        {
            lock (_lock)
            {
                if (_rooms.Remove(roomId))
                    Console.WriteLine($"[RoomManager] 룸 제거 (G2L): roomId={roomId}");
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
