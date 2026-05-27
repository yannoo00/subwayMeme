using System.Collections.Generic;

namespace GameServer
{
    // 단일 GameServer 프로세스 안에서 여러 GameRoom 을 룸 ID 키로 관리.
    // 룸은 LobbyServer 가 보낸 L2G_CreateRoom 수신 시 생성.
    // 게임 종료 또는 전원 이탈 시 RemoveRoom 으로 정리.
    public class GameRoomManager
    {
        public static readonly GameRoomManager Instance = new();

        readonly object _lock = new();
        readonly Dictionary<int, GameRoom> _rooms = new();

        // 같은 roomId 재요청 시에는 기존 인스턴스를 반환 (덮어쓰지 않음).
        // 로비서버 재전송 등으로 인한 중복 생성 방지.
        public GameRoom CreateRoom(int roomId, int playerCount, int hostPlayerId)
        {
            lock (_lock)
            {
                if (_rooms.TryGetValue(roomId, out var existing))
                    return existing;

                var room = new GameRoom();
                room.Init(roomId, playerCount, hostPlayerId);
                _rooms[roomId] = room;
                return room;
            }
        }

        // C_EnterGame 에서 클라가 보낸 roomId 로 룸 조회. 없으면 null.
        public GameRoom Get(int roomId)
        {
            lock (_lock)
            {
                _rooms.TryGetValue(roomId, out var room);
                return room;
            }
        }

        public void RemoveRoom(int roomId)
        {
            lock (_lock)
                _rooms.Remove(roomId);
        }
    }
}
