using System;
using Google.Protobuf;
using GameProto;
using ServerCore;

namespace GameServer
{
    public class GamePacketHandler
    {
        public static Action<PacketSession, ArraySegment<byte>>[] Handlers { get; private set; }

        static GamePacketHandler()
        {
            // S_WEAPON_SWITCH(81) 가 최대 ID 라 배열 크기 기준이 됨
            int maxId = (int)GamePacketId.SWeaponSwitch + 1;
            Handlers = new Action<PacketSession, ArraySegment<byte>>[maxId];

            Handlers[(int)GamePacketId.CEnterGame]     = Handle_C_EnterGame;
            Handlers[(int)GamePacketId.CReady]         = Handle_C_Ready;
            Handlers[(int)GamePacketId.CMove]          = Handle_C_Move;
            Handlers[(int)GamePacketId.CAttack]        = Handle_C_Attack;
            Handlers[(int)GamePacketId.CAiming]        = Handle_C_Aiming;
            Handlers[(int)GamePacketId.CReload]        = Handle_C_Reload;
            Handlers[(int)GamePacketId.CWeaponSwitch]  = Handle_C_WeaponSwitch;
            Handlers[(int)GamePacketId.CEnemySpawned]  = Handle_C_EnemySpawned;
            Handlers[(int)GamePacketId.CEnemySync]     = Handle_C_EnemySync;
            Handlers[(int)GamePacketId.CEnemyAttack]   = Handle_C_EnemyAttack;
            Handlers[(int)GamePacketId.CExitSubway]    = Handle_C_ExitSubway;
            Handlers[(int)GamePacketId.CBoardSubway]   = Handle_C_BoardSubway;
            Handlers[(int)GamePacketId.CSelectRoute]   = Handle_C_SelectRoute;
            Handlers[(int)GamePacketId.CInteract]      = Handle_C_Interact;
            Handlers[(int)GamePacketId.CMutation]      = Handle_C_Mutation;
        }

        // === 접속/초기화 ===

        static void Handle_C_EnterGame(PacketSession session, ArraySegment<byte> body)
        {
            var pkt    = C_EnterGame.Parser.ParseFrom(body.Array, body.Offset, body.Count);
            var gs     = (GameSession)session;

            // Phase 2: roomId 로 GameRoomManager 에서 룸을 찾아 세션에 바인딩.
            // 이후 모든 핸들러는 gs.Room 으로 해당 룸에만 접근.
            var room = GameRoomManager.Instance.Get(pkt.RoomId);
            if (room == null)
            {
                Console.WriteLine($"[Game] C_EnterGame 거부: 알 수 없는 roomId={pkt.RoomId} (sessionId={session.SessionId})");
                return;
            }
            gs.BindRoom(room);

            var player = gs.Room.Add(gs, pkt.PlayerId, pkt.PlayerName, pkt.CharacterId);

            Console.WriteLine($"[Game] C_EnterGame: playerId={player.PlayerId}, name={player.PlayerName}, characterId={player.CharacterId}, isHost={player.IsHost}");

            // 입장한 본인에게: 역할 + 현재 방 플레이어 목록
            var enterRes = new S_EnterGame { IsHost = player.IsHost };
            foreach (var p in gs.Room.GetAllPlayers())
                enterRes.Players.Add(p.ToProto());
            session.Send(MakePacket(GamePacketId.SEnterGame, enterRes));

            // 기존 플레이어들에게 새 입장자 알림
            var enteredBytes = MakePacket(GamePacketId.SPlayerEntered,
                new S_PlayerEntered { Player = player.ToProto() });
            foreach (var s in gs.Room.GetOtherSessions(session.SessionId))
                s.Send(enteredBytes);
        }

        static void Handle_C_Ready(PacketSession session, ArraySegment<byte> body)
        {
            var gs     = (GameSession)session;
            Console.WriteLine($"[Game] C_Ready: sessionId={session.SessionId}");

            var result = gs.Room.MarkReady(session.SessionId);
            if (!result.AllReady) return;

            Console.WriteLine($"[Game] 전원 준비 완료 - S_GameStart (seed={result.Seed})");
            var startBytes = MakePacket(GamePacketId.SGameStart, new S_GameStart { MapSeed = result.Seed });
            foreach (var s in result.AllSessions)
                s.Send(startBytes);
        }

        // === 플레이어 이동 ===

        static void Handle_C_Move(PacketSession session, ArraySegment<byte> body)
        {
            var gs     = (GameSession)session;
            var pkt    = C_Move.Parser.ParseFrom(body.Array, body.Offset, body.Count);
            var player = gs.Room.Get(session.SessionId);
            if (player == null) return;

            var relay = new S_Move
            {
                PlayerId = player.PlayerId,
                PosX     = pkt.PosX, PosY = pkt.PosY, PosZ = pkt.PosZ,
                RotY     = pkt.RotY,
                Speed    = pkt.Speed,
                StrafeX  = pkt.StrafeX,
                StrafeY  = pkt.StrafeY,
            };
            var relayBytes = MakePacket(GamePacketId.SMove, relay);
            foreach (var s in gs.Room.GetOtherSessions(session.SessionId))
                s.Send(relayBytes);
        }

        // === 전투 ===

        static void Handle_C_Attack(PacketSession session, ArraySegment<byte> body)
        {
            var gs     = (GameSession)session;
            var pkt    = C_Attack.Parser.ParseFrom(body.Array, body.Offset, body.Count);
            var player = gs.Room.Get(session.SessionId);
            if (player == null) return;

            Console.WriteLine($"[Game] C_Attack: playerId={player.PlayerId}, hits={pkt.HitEnemyIds.Count}");

            // 공격 애니메이션 브로드캐스트
            var attackBytes = MakePacket(GamePacketId.SAttack,
                new S_Attack { PlayerId = player.PlayerId, WeaponId = pkt.WeaponId });
            foreach (var s in gs.Room.GetOtherSessions(session.SessionId))
                s.Send(attackBytes);

            // 피격 적마다 HP 계산 후 S_EnemyDamaged 또는 S_EnemyDied 브로드캐스트
            // TODO: 쿨타임/거리 검증 추가
            foreach (int enemyId in pkt.HitEnemyIds)
            {
                var (currentHp, isDead) = gs.Room.Enemies.ApplyDamage(enemyId, pkt.Damage);
                if (currentHp == -1) continue; // 서버에 미등록 적 (이미 사망 처리 중)

                if (isDead)
                {
                    Console.WriteLine($"[Game] S_EnemyDied: enemyId={enemyId}");
                    var diedBytes = MakePacket(GamePacketId.SEnemyDied, new S_EnemyDied { EnemyId = enemyId });
                    foreach (var s in gs.Room.GetAllSessions())
                        s.Send(diedBytes);
                }
                else
                {
                    var dmgBytes = MakePacket(GamePacketId.SEnemyDamaged,
                        new S_EnemyDamaged { EnemyId = enemyId, CurrentHp = currentHp });
                    foreach (var s in gs.Room.GetAllSessions())
                        s.Send(dmgBytes);
                }
            }
        }

        // === 무기 액션 (Aim / Reload / Switch) ===
        // 전부 단순 broadcast. 본인은 이미 로컬에서 처리했으므로 제외.

        static void Handle_C_Aiming(PacketSession session, ArraySegment<byte> body)
        {
            var gs     = (GameSession)session;
            var pkt    = C_Aiming.Parser.ParseFrom(body.Array, body.Offset, body.Count);
            var player = gs.Room.Get(session.SessionId);
            if (player == null) return;

            var relay = new S_Aiming
            {
                PlayerId     = player.PlayerId,
                IsAiming     = pkt.IsAiming,
                WeaponTypeId = pkt.WeaponTypeId,
            };
            var relayBytes = MakePacket(GamePacketId.SAiming, relay);
            foreach (var s in gs.Room.GetOtherSessions(session.SessionId))
                s.Send(relayBytes);
        }

        static void Handle_C_Reload(PacketSession session, ArraySegment<byte> body)
        {
            var gs     = (GameSession)session;
            var pkt    = C_Reload.Parser.ParseFrom(body.Array, body.Offset, body.Count);
            var player = gs.Room.Get(session.SessionId);
            if (player == null) return;

            var relay = new S_Reload
            {
                PlayerId = player.PlayerId,
                Duration = pkt.Duration,
            };
            var relayBytes = MakePacket(GamePacketId.SReload, relay);
            foreach (var s in gs.Room.GetOtherSessions(session.SessionId))
                s.Send(relayBytes);
        }

        static void Handle_C_WeaponSwitch(PacketSession session, ArraySegment<byte> body)
        {
            var gs     = (GameSession)session;
            var pkt    = C_WeaponSwitch.Parser.ParseFrom(body.Array, body.Offset, body.Count);
            var player = gs.Room.Get(session.SessionId);
            if (player == null) return;

            var relay = new S_WeaponSwitch
            {
                PlayerId        = player.PlayerId,
                Duration        = pkt.Duration,
                NewWeaponTypeId = pkt.NewWeaponTypeId,
            };
            var relayBytes = MakePacket(GamePacketId.SWeaponSwitch, relay);
            foreach (var s in gs.Room.GetOtherSessions(session.SessionId))
                s.Send(relayBytes);
        }

        // === 적 동기화 (호스트만 전송) ===

        static void Handle_C_EnemySpawned(PacketSession session, ArraySegment<byte> body)
        {
            var gs     = (GameSession)session;
            var pkt    = C_EnemySpawned.Parser.ParseFrom(body.Array, body.Offset, body.Count);
            var player = gs.Room.Get(session.SessionId);
            if (player == null || !player.IsHost) return;

            int enemyId = gs.Room.GenerateEnemyId();
            gs.Room.Enemies.RegisterEnemy(enemyId, pkt.MaxHp);

            Console.WriteLine($"[Game] C_EnemySpawned: type={pkt.EnemyType}, pos=({pkt.PosX:F1},{pkt.PosY:F1},{pkt.PosZ:F1}) -> id={enemyId}");

            var spawnBytes = MakePacket(GamePacketId.SEnemySpawn, new S_EnemySpawn
            {
                EnemyId   = enemyId,
                EnemyType = pkt.EnemyType,
                PosX      = pkt.PosX,
                PosY      = pkt.PosY,
                PosZ      = pkt.PosZ,
            });
            foreach (var s in gs.Room.GetAllSessions())
                s.Send(spawnBytes);
        }

        static void Handle_C_EnemySync(PacketSession session, ArraySegment<byte> body)
        {
            var gs     = (GameSession)session;
            var pkt    = C_EnemySync.Parser.ParseFrom(body.Array, body.Offset, body.Count);
            var player = gs.Room.Get(session.SessionId);
            if (player == null || !player.IsHost) return;

            var relay = new S_EnemySync();
            relay.Enemies.AddRange(pkt.Enemies);
            var relayBytes = MakePacket(GamePacketId.SEnemySync, relay);
            foreach (var s in gs.Room.GetOtherSessions(session.SessionId))
                s.Send(relayBytes);
        }

        static void Handle_C_EnemyAttack(PacketSession session, ArraySegment<byte> body)
        {
            var gs     = (GameSession)session;
            var pkt    = C_EnemyAttack.Parser.ParseFrom(body.Array, body.Offset, body.Count);
            var player = gs.Room.Get(session.SessionId);
            if (player == null || !player.IsHost) return;

            Console.WriteLine($"[Game] C_EnemyAttack: enemyId={pkt.EnemyId}, target={pkt.TargetPlayerId}, dmg={pkt.Damage}");

            var result = gs.Room.ApplyPlayerDamage(pkt.TargetPlayerId, pkt.Damage);
            if (result.Player == null) return;

            if (result.IsDead)
            {
                Console.WriteLine($"[Game] S_PlayerDied: playerId={result.Player.PlayerId}");
                var diedBytes = MakePacket(GamePacketId.SPlayerDied,
                    new S_PlayerDied { PlayerId = result.Player.PlayerId });
                foreach (var s in gs.Room.GetAllSessions())
                    s.Send(diedBytes);
            }
            else
            {
                var dmgBytes = MakePacket(GamePacketId.SPlayerDamaged,
                    new S_PlayerDamaged { PlayerId = result.Player.PlayerId, Damage = pkt.Damage, CurrentHp = result.CurrentHp });
                foreach (var s in gs.Room.GetAllSessions())
                    s.Send(dmgBytes);
            }
        }

        // === 스테이지 진행 ===

        static void Handle_C_ExitSubway(PacketSession session, ArraySegment<byte> body)
        {
            var gs     = (GameSession)session;
            var player = gs.Room.Get(session.SessionId);
            if (player == null) return;

            Console.WriteLine($"[Game] C_ExitSubway: playerId={player.PlayerId}");

            var result = gs.Room.MarkExited(session.SessionId);

            var exitedBytes = MakePacket(GamePacketId.SPlayerExited,
                new S_PlayerExited { PlayerId = player.PlayerId, ExitedCount = result.ExitedCount, TotalCount = result.Total });
            foreach (var s in result.Others)
                s.Send(exitedBytes);

            if (result.AllExited)
            {
                Console.WriteLine($"[Game] 전원 하차 완료 - S_AllExited");
                var allExitedBytes = MakePacket(GamePacketId.SAllExited, new S_AllExited());
                foreach (var s in gs.Room.GetAllSessions())
                    s.Send(allExitedBytes);
            }
        }

        static void Handle_C_BoardSubway(PacketSession session, ArraySegment<byte> body)
        {
            var gs     = (GameSession)session;
            var player = gs.Room.Get(session.SessionId);
            if (player == null) return;

            Console.WriteLine($"[Game] C_BoardSubway: playerId={player.PlayerId}");

            var result = gs.Room.MarkBoarded(session.SessionId);

            var boardedBytes = MakePacket(GamePacketId.SPlayerBoarded,
                new S_PlayerBoarded { PlayerId = player.PlayerId, BoardedCount = result.BoardedCount, TotalCount = result.Total });
            foreach (var s in result.Others)
                s.Send(boardedBytes);

            if (result.Trigger)
                BroadcastAllBoarded(gs.Room, result.NodeIndex);
        }

        static void Handle_C_SelectRoute(PacketSession session, ArraySegment<byte> body)
        {
            var gs     = (GameSession)session;
            var pkt    = C_SelectRoute.Parser.ParseFrom(body.Array, body.Offset, body.Count);
            var result = gs.Room.SelectRoute(session.SessionId, pkt.NodeIndex);

            if (!result.IsHost)
            {
                Console.WriteLine($"[Game] C_SelectRoute: 방장 아닌 플레이어 요청 무시 (sessionId={session.SessionId})");
                return;
            }

            Console.WriteLine($"[Game] C_SelectRoute: nodeIndex={result.NodeIndex}");

            if (result.Trigger)
                BroadcastAllBoarded(gs.Room, result.NodeIndex);
        }

        // 전원 탑승 + 경로 확정 -> S_AllBoarded 후 스테이지 상태 리셋
        // session 인자가 없으므로 호출부에서 GameRoom 을 직접 넘겨준다.
        static void BroadcastAllBoarded(GameRoom room, int nodeIndex)
        {
            Console.WriteLine($"[Game] 전원 탑승 + 경로 확정 - S_AllBoarded (node={nodeIndex})");
            var allBoardedBytes = MakePacket(GamePacketId.SAllBoarded,
                new S_AllBoarded { NodeIndex = nodeIndex });
            foreach (var s in room.GetAllSessions())
                s.Send(allBoardedBytes);

            room.ResetStageState();
        }

        // === 상호작용 ===

        static void Handle_C_Interact(PacketSession session, ArraySegment<byte> body)
        {
            var gs     = (GameSession)session;
            var pkt    = C_Interact.Parser.ParseFrom(body.Array, body.Offset, body.Count);
            var player = gs.Room.Get(session.SessionId);
            if (player == null) return;

            Console.WriteLine($"[Game] C_Interact: objectId={pkt.ObjectId}, type={pkt.Type}, playerId={player.PlayerId}");

            // TODO: 상호작용 타입별 처리 (아이템 획득, 상자 오픈 등)
            session.Send(MakePacket(GamePacketId.SInteractResult, new S_InteractResult
            {
                Success  = true,
                PlayerId = player.PlayerId,
                CurrentHp = player.Hp,
                Gold      = 0,
            }));
        }

        // === 변이 ===

        // 클라가 픽업 먹은 시점에 송신. 서버는 효과를 모르고 전원 브로드캐스트만 한다.
        // 송신자 본인에게도 보내서 "서버 응답 시점에 효과 적용" 흐름을 단일화
        static void Handle_C_Mutation(PacketSession session, ArraySegment<byte> body)
        {
            var gs     = (GameSession)session;
            var pkt    = C_Mutation.Parser.ParseFrom(body.Array, body.Offset, body.Count);
            var player = gs.Room.Get(session.SessionId);
            if (player == null) return;

            Console.WriteLine($"[Game] C_Mutation: playerId={player.PlayerId}, mutationId={pkt.MutationId}");

            var resultBytes = MakePacket(GamePacketId.SMutationResult, new S_MutationResult
            {
                PlayerId   = player.PlayerId,
                MutationId = pkt.MutationId,
            });
            foreach (var s in gs.Room.GetAllSessions())
                s.Send(resultBytes);
        }

        // === 공통 유틸 ===

        public static ArraySegment<byte> MakePacket(GamePacketId id, IMessage message)
        {
            byte[] body      = message.ToByteArray();
            ushort totalSize = (ushort)(PacketSession.HEADER_SIZE + body.Length);

            byte[] packet = new byte[totalSize];
            Array.Copy(BitConverter.GetBytes(totalSize), 0, packet, 0, 2);
            Array.Copy(BitConverter.GetBytes((ushort)id), 0, packet, 2, 2);
            Array.Copy(body, 0, packet, PacketSession.HEADER_SIZE, body.Length);

            return new ArraySegment<byte>(packet);
        }
    }
}
