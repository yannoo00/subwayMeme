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
            int maxId = (int)GamePacketId.SInteractResult + 1;
            Handlers = new Action<PacketSession, ArraySegment<byte>>[maxId];

            Handlers[(int)GamePacketId.CEnterGame]  = Handle_C_EnterGame;
            Handlers[(int)GamePacketId.CReady]       = Handle_C_Ready;
            Handlers[(int)GamePacketId.CMove]        = Handle_C_Move;
            Handlers[(int)GamePacketId.CAttack]      = Handle_C_Attack;
            Handlers[(int)GamePacketId.CEnemySpawned] = Handle_C_EnemySpawned;
            Handlers[(int)GamePacketId.CEnemySync]    = Handle_C_EnemySync;
            Handlers[(int)GamePacketId.CEnemyAttack]  = Handle_C_EnemyAttack;
            Handlers[(int)GamePacketId.CExitSubway]  = Handle_C_ExitSubway;
            Handlers[(int)GamePacketId.CBoardSubway] = Handle_C_BoardSubway;
            Handlers[(int)GamePacketId.CSelectRoute] = Handle_C_SelectRoute;
            Handlers[(int)GamePacketId.CInteract]    = Handle_C_Interact;
        }

        // === 접속/초기화 ===

        static void Handle_C_EnterGame(PacketSession session, ArraySegment<byte> body)
        {
            var pkt    = C_EnterGame.Parser.ParseFrom(body.Array, body.Offset, body.Count);
            var gs     = (GameSession)session;
            var player = GameRoom.Instance.Add(gs, pkt.PlayerId, pkt.PlayerName);

            Console.WriteLine($"[Game] C_EnterGame: playerId={player.PlayerId}, name={player.PlayerName}, isHost={player.IsHost}");

            // 입장한 본인에게: 역할 + 현재 방 플레이어 목록
            var enterRes = new S_EnterGame { IsHost = player.IsHost };
            foreach (var p in GameRoom.Instance.GetAllPlayers())
                enterRes.Players.Add(p.ToProto());
            session.Send(MakePacket(GamePacketId.SEnterGame, enterRes));

            // 기존 플레이어들에게 새 입장자 알림
            var enteredBytes = MakePacket(GamePacketId.SPlayerEntered,
                new S_PlayerEntered { Player = player.ToProto() });
            foreach (var s in GameRoom.Instance.GetOtherSessions(session.SessionId))
                s.Send(enteredBytes);
        }

        static void Handle_C_Ready(PacketSession session, ArraySegment<byte> body)
        {
            Console.WriteLine($"[Game] C_Ready: sessionId={session.SessionId}");

            var result = GameRoom.Instance.MarkReady(session.SessionId);
            if (!result.AllReady) return;

            Console.WriteLine($"[Game] 전원 준비 완료 - S_GameStart (seed={result.Seed})");
            var startBytes = MakePacket(GamePacketId.SGameStart, new S_GameStart { MapSeed = result.Seed });
            foreach (var s in result.AllSessions)
                s.Send(startBytes);
        }

        // === 플레이어 이동 ===

        static void Handle_C_Move(PacketSession session, ArraySegment<byte> body)
        {
            var pkt    = C_Move.Parser.ParseFrom(body.Array, body.Offset, body.Count);
            var player = GameRoom.Instance.Get(session.SessionId);
            if (player == null) return;

            var relay = new S_Move
            {
                PlayerId  = player.PlayerId,
                PosX      = pkt.PosX, PosY = pkt.PosY, PosZ = pkt.PosZ,
                RotY      = pkt.RotY,
                IsMoving  = pkt.IsMoving,
                IsDodging = pkt.IsDodging,
            };
            var relayBytes = MakePacket(GamePacketId.SMove, relay);
            foreach (var s in GameRoom.Instance.GetOtherSessions(session.SessionId))
                s.Send(relayBytes);
        }

        // === 전투 ===

        static void Handle_C_Attack(PacketSession session, ArraySegment<byte> body)
        {
            var pkt    = C_Attack.Parser.ParseFrom(body.Array, body.Offset, body.Count);
            var player = GameRoom.Instance.Get(session.SessionId);
            if (player == null) return;

            Console.WriteLine($"[Game] C_Attack: playerId={player.PlayerId}, hits={pkt.HitEnemyIds.Count}");

            // 공격 애니메이션 브로드캐스트
            var attackBytes = MakePacket(GamePacketId.SAttack,
                new S_Attack { PlayerId = player.PlayerId, WeaponId = pkt.WeaponId });
            foreach (var s in GameRoom.Instance.GetOtherSessions(session.SessionId))
                s.Send(attackBytes);

            // 피격 적마다 HP 계산 후 S_EnemyDamaged 또는 S_EnemyDied 브로드캐스트
            // TODO: 쿨타임/거리 검증 추가
            foreach (int enemyId in pkt.HitEnemyIds)
            {
                var (currentHp, isDead) = EnemyManager.Instance.ApplyDamage(enemyId, pkt.Damage);
                if (currentHp == -1) continue; // 서버에 미등록 적 (이미 사망 처리 중)

                if (isDead)
                {
                    Console.WriteLine($"[Game] S_EnemyDied: enemyId={enemyId}");
                    var diedBytes = MakePacket(GamePacketId.SEnemyDied, new S_EnemyDied { EnemyId = enemyId });
                    foreach (var s in GameRoom.Instance.GetAllSessions())
                        s.Send(diedBytes);
                }
                else
                {
                    var dmgBytes = MakePacket(GamePacketId.SEnemyDamaged,
                        new S_EnemyDamaged { EnemyId = enemyId, CurrentHp = currentHp });
                    foreach (var s in GameRoom.Instance.GetAllSessions())
                        s.Send(dmgBytes);
                }
            }
        }

        // === 적 동기화 (호스트만 전송) ===

        static void Handle_C_EnemySpawned(PacketSession session, ArraySegment<byte> body)
        {
            var pkt    = C_EnemySpawned.Parser.ParseFrom(body.Array, body.Offset, body.Count);
            var player = GameRoom.Instance.Get(session.SessionId);
            if (player == null || !player.IsHost) return;

            int enemyId = GameRoom.Instance.GenerateEnemyId();
            EnemyManager.Instance.RegisterEnemy(enemyId, pkt.MaxHp);

            Console.WriteLine($"[Game] C_EnemySpawned: type={pkt.EnemyType}, pos=({pkt.PosX:F1},{pkt.PosY:F1},{pkt.PosZ:F1}) -> id={enemyId}");

            var spawnBytes = MakePacket(GamePacketId.SEnemySpawn, new S_EnemySpawn
            {
                EnemyId   = enemyId,
                EnemyType = pkt.EnemyType,
                PosX      = pkt.PosX,
                PosY      = pkt.PosY,
                PosZ      = pkt.PosZ,
            });
            foreach (var s in GameRoom.Instance.GetAllSessions())
                s.Send(spawnBytes);
        }

        static void Handle_C_EnemySync(PacketSession session, ArraySegment<byte> body)
        {
            var pkt    = C_EnemySync.Parser.ParseFrom(body.Array, body.Offset, body.Count);
            var player = GameRoom.Instance.Get(session.SessionId);
            if (player == null || !player.IsHost) return;

            var relay = new S_EnemySync();
            relay.Enemies.AddRange(pkt.Enemies);
            var relayBytes = MakePacket(GamePacketId.SEnemySync, relay);
            foreach (var s in GameRoom.Instance.GetOtherSessions(session.SessionId))
                s.Send(relayBytes);
        }

        static void Handle_C_EnemyAttack(PacketSession session, ArraySegment<byte> body)
        {
            var pkt    = C_EnemyAttack.Parser.ParseFrom(body.Array, body.Offset, body.Count);
            var player = GameRoom.Instance.Get(session.SessionId);
            if (player == null || !player.IsHost) return;

            Console.WriteLine($"[Game] C_EnemyAttack: enemyId={pkt.EnemyId}, target={pkt.TargetPlayerId}, dmg={pkt.Damage}");

            var result = GameRoom.Instance.ApplyPlayerDamage(pkt.TargetPlayerId, pkt.Damage);
            if (result.Player == null) return;

            if (result.IsDead)
            {
                Console.WriteLine($"[Game] S_PlayerDied: playerId={result.Player.PlayerId}");
                var diedBytes = MakePacket(GamePacketId.SPlayerDied,
                    new S_PlayerDied { PlayerId = result.Player.PlayerId });
                foreach (var s in GameRoom.Instance.GetAllSessions())
                    s.Send(diedBytes);
            }
            else
            {
                var dmgBytes = MakePacket(GamePacketId.SPlayerDamaged,
                    new S_PlayerDamaged { PlayerId = result.Player.PlayerId, Damage = pkt.Damage, CurrentHp = result.CurrentHp });
                foreach (var s in GameRoom.Instance.GetAllSessions())
                    s.Send(dmgBytes);
            }
        }

        // === 스테이지 진행 ===

        static void Handle_C_ExitSubway(PacketSession session, ArraySegment<byte> body)
        {
            var player = GameRoom.Instance.Get(session.SessionId);
            if (player == null) return;

            Console.WriteLine($"[Game] C_ExitSubway: playerId={player.PlayerId}");

            var result = GameRoom.Instance.MarkExited(session.SessionId);

            var exitedBytes = MakePacket(GamePacketId.SPlayerExited,
                new S_PlayerExited { PlayerId = player.PlayerId, ExitedCount = result.ExitedCount, TotalCount = result.Total });
            foreach (var s in result.Others)
                s.Send(exitedBytes);

            if (result.AllExited)
            {
                Console.WriteLine($"[Game] 전원 하차 완료 - S_AllExited");
                var allExitedBytes = MakePacket(GamePacketId.SAllExited, new S_AllExited());
                foreach (var s in GameRoom.Instance.GetAllSessions())
                    s.Send(allExitedBytes);
            }
        }

        static void Handle_C_BoardSubway(PacketSession session, ArraySegment<byte> body)
        {
            var player = GameRoom.Instance.Get(session.SessionId);
            if (player == null) return;

            Console.WriteLine($"[Game] C_BoardSubway: playerId={player.PlayerId}");

            var result = GameRoom.Instance.MarkBoarded(session.SessionId);

            var boardedBytes = MakePacket(GamePacketId.SPlayerBoarded,
                new S_PlayerBoarded { PlayerId = player.PlayerId, BoardedCount = result.BoardedCount, TotalCount = result.Total });
            foreach (var s in result.Others)
                s.Send(boardedBytes);

            if (result.Trigger)
                BroadcastAllBoarded(result.NodeIndex);
        }

        static void Handle_C_SelectRoute(PacketSession session, ArraySegment<byte> body)
        {
            var pkt    = C_SelectRoute.Parser.ParseFrom(body.Array, body.Offset, body.Count);
            var result = GameRoom.Instance.SelectRoute(session.SessionId, pkt.NodeIndex);

            if (!result.IsHost)
            {
                Console.WriteLine($"[Game] C_SelectRoute: 방장 아닌 플레이어 요청 무시 (sessionId={session.SessionId})");
                return;
            }

            Console.WriteLine($"[Game] C_SelectRoute: nodeIndex={result.NodeIndex}");

            if (result.Trigger)
                BroadcastAllBoarded(result.NodeIndex);
        }

        // 전원 탑승 + 경로 확정 -> S_AllBoarded 후 스테이지 상태 리셋
        static void BroadcastAllBoarded(int nodeIndex)
        {
            Console.WriteLine($"[Game] 전원 탑승 + 경로 확정 - S_AllBoarded (node={nodeIndex})");
            var allBoardedBytes = MakePacket(GamePacketId.SAllBoarded,
                new S_AllBoarded { NodeIndex = nodeIndex });
            foreach (var s in GameRoom.Instance.GetAllSessions())
                s.Send(allBoardedBytes);

            GameRoom.Instance.ResetStageState();
        }

        // === 상호작용 ===

        static void Handle_C_Interact(PacketSession session, ArraySegment<byte> body)
        {
            var pkt    = C_Interact.Parser.ParseFrom(body.Array, body.Offset, body.Count);
            var player = GameRoom.Instance.Get(session.SessionId);
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
