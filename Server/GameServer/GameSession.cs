using System;
using System.Net;
using GameProto;
using InternalProto;
using YannooNet;

namespace GameServer
{
    public class GameSession : YPacketSession
    {
        // 이 세션이 속한 게임 룸. 처음에는 null.
        // C_EnterGame 수신 시 GamePacketHandler 가 roomId 로 GameRoomManager 에서 찾아 BindRoom 으로 주입.
        public GameRoom Room { get; private set; }

        public void BindRoom(GameRoom room)
        {
            Room = room;
        }

        public override void OnConnected(EndPoint endPoint)
        {
            Console.WriteLine($"[GameSession] 접속: {endPoint} / SessionId: {SessionId}");
        }

        public override void OnDisconnected(EndPoint endPoint)
        {
            Console.WriteLine($"[GameSession] 해제: {endPoint} / SessionId: {SessionId}");

            // C_EnterGame 이전에 끊긴 세션은 Room 이 null 이라 정리할 게 없다.
            if (Room == null) return;

            var room = Room;

            // 상태 변경(Remove) + 관련 브로드캐스트/룸 정리를 하나의 Job으로 묶어
            // 다른 플레이어의 패킷 처리와 절대 끼어들지 않게 함
            room.Push(() =>
            {
                var result = room.Remove(SessionId);
                if (result.Leaver == null) return;

                // 나머지 플레이어에게 퇴장 알림
                var leftBytes = GamePacketHandler.MakePacket(
                    GamePacketId.SPlayerLeft,
                    new S_PlayerLeft { PlayerId = result.Leaver.PlayerId });
                foreach (var s in result.Remaining)
                    s.Send(leftBytes);

                // 호스트가 나갔으면 새 호스트 지정 알림
                if (result.NewHostPlayerId != -1)
                {
                    Console.WriteLine($"[GameSession] 호스트 이탈 -> 새 호스트: playerId={result.NewHostPlayerId}");
                    var hostBytes = GamePacketHandler.MakePacket(
                        GamePacketId.SHostChanged,
                        new S_HostChanged { NewHostId = result.NewHostPlayerId });
                    foreach (var s in result.Remaining)
                        s.Send(hostBytes);
                }

                // 룸이 비었으면 GameRoomManager 에서 제거 + 로비에 종료 알림 (best-effort)
                if (result.Remaining.Count == 0)
                {
                    Console.WriteLine($"[GameSession] 룸 비어있음 - 정리: roomId={room.RoomId}");
                    GameRoomManager.Instance.RemoveRoom(room.RoomId);

                    var lobby = InternalSession.LobbyConnection;
                    if (lobby != null)
                    {
                        var endedBytes = InternalPacketHandler.MakePacket(
                            InternalPacketId.G2LRoomEnded,
                            new G2L_RoomEnded { RoomId = room.RoomId });
                        lobby.Send(endedBytes);
                    }
                    else
                    {
                        Console.WriteLine($"[GameSession] G2L_RoomEnded 송신 스킵: 로비 연결 없음");
                    }
                }
            });
        }

        public override void OnRecvPacket(ushort id, ArraySegment<byte> body)
        {
            if (id >= GamePacketHandler.Handlers.Length)
            {
                Console.WriteLine($"[GameSession] 알 수 없는 PacketId: {id}");
                return;
            }

            var handler = GamePacketHandler.Handlers[id];
            if (handler == null)
            {
                Console.WriteLine($"[GameSession] 핸들러 미등록 PacketId: {id}");
                return;
            }

            handler.Invoke(this, body);
        }

        public override void OnSend(int numOfBytes) { }
    }
}
