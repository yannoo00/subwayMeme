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

            Handlers[(int)GamePacketId.CEnterGame]    = Handle_C_EnterGame;
            Handlers[(int)GamePacketId.CReady]         = Handle_C_Ready;
            Handlers[(int)GamePacketId.CMove]          = Handle_C_Move;
            Handlers[(int)GamePacketId.CAttack]        = Handle_C_Attack;
            Handlers[(int)GamePacketId.CEnemySync]     = Handle_C_EnemySync;
            Handlers[(int)GamePacketId.CEnemyAttack]   = Handle_C_EnemyAttack;
            Handlers[(int)GamePacketId.CExitSubway]    = Handle_C_ExitSubway;
            Handlers[(int)GamePacketId.CBoardSubway]   = Handle_C_BoardSubway;
            Handlers[(int)GamePacketId.CSelectRoute]   = Handle_C_SelectRoute;
            Handlers[(int)GamePacketId.CInteract]      = Handle_C_Interact;
        }

        // === 접속/초기화 ===

        static void Handle_C_EnterGame(PacketSession session, ArraySegment<byte> body)
        {
            var pkt = C_EnterGame.Parser.ParseFrom(body.Array, body.Offset, body.Count);
            Console.WriteLine($"[Game] C_EnterGame: playerId={pkt.PlayerId}, name={pkt.PlayerName}");

            // TODO: GameRoom에 플레이어 등록, S_EnterGame 응답, S_PlayerEntered 브로드캐스트
        }

        static void Handle_C_Ready(PacketSession session, ArraySegment<byte> body)
        {
            Console.WriteLine($"[Game] C_Ready: sessionId={session.SessionId}");

            // TODO: 준비 카운트 증가, 전원 준비 시 S_GameStart (seed 포함) 브로드캐스트
        }

        // === 플레이어 이동 ===

        static void Handle_C_Move(PacketSession session, ArraySegment<byte> body)
        {
            var pkt = C_Move.Parser.ParseFrom(body.Array, body.Offset, body.Count);

            // TODO: S_Move로 다른 플레이어들에게 브로드캐스트 (송신자 제외)
        }

        // === 전투 ===

        static void Handle_C_Attack(PacketSession session, ArraySegment<byte> body)
        {
            var pkt = C_Attack.Parser.ParseFrom(body.Array, body.Offset, body.Count);
            Console.WriteLine($"[Game] C_Attack: sessionId={session.SessionId}, hits={pkt.HitEnemyIds.Count}");

            // TODO: 서버 검증 (쿨타임, 거리 등)
            // TODO: S_Attack 브로드캐스트 (공격 애니메이션)
            // TODO: 히트 대상별 S_EnemyDamaged / S_EnemyDied 전송
        }

        // === 적 동기화 (호스트만 전송) ===

        static void Handle_C_EnemySync(PacketSession session, ArraySegment<byte> body)
        {
            var pkt = C_EnemySync.Parser.ParseFrom(body.Array, body.Offset, body.Count);

            // TODO: 호스트 여부 확인
            // TODO: S_EnemySync로 비호스트에게 릴레이
        }

        static void Handle_C_EnemyAttack(PacketSession session, ArraySegment<byte> body)
        {
            var pkt = C_EnemyAttack.Parser.ParseFrom(body.Array, body.Offset, body.Count);
            Console.WriteLine($"[Game] C_EnemyAttack: enemyId={pkt.EnemyId}, target={pkt.TargetPlayerId}, dmg={pkt.Damage}");

            // TODO: 호스트 여부 확인
            // TODO: 서버가 데미지 적용 후 S_PlayerDamaged / S_PlayerDied 브로드캐스트
        }

        // === 스테이지 진행 ===

        static void Handle_C_ExitSubway(PacketSession session, ArraySegment<byte> body)
        {
            Console.WriteLine($"[Game] C_ExitSubway: sessionId={session.SessionId}");

            // TODO: 하차 요청 카운트 증가
            // TODO: S_PlayerExited 브로드캐스트
            // TODO: 전원 하차 시 S_AllExited 브로드캐스트
        }

        static void Handle_C_BoardSubway(PacketSession session, ArraySegment<byte> body)
        {
            Console.WriteLine($"[Game] C_BoardSubway: sessionId={session.SessionId}");

            // TODO: 탑승 요청 카운트 증가
            // TODO: S_PlayerBoarded 브로드캐스트
            // TODO: 전원 탑승 + 경로 선택 완료 시 S_AllBoarded 브로드캐스트
        }

        static void Handle_C_SelectRoute(PacketSession session, ArraySegment<byte> body)
        {
            var pkt = C_SelectRoute.Parser.ParseFrom(body.Array, body.Offset, body.Count);
            Console.WriteLine($"[Game] C_SelectRoute: nodeIndex={pkt.NodeIndex}");

            // TODO: 방장 여부 확인
            // TODO: 경로 저장, 전원 탑승 대기 중이면 S_AllBoarded 트리거
        }

        // === 상호작용 ===

        static void Handle_C_Interact(PacketSession session, ArraySegment<byte> body)
        {
            var pkt = C_Interact.Parser.ParseFrom(body.Array, body.Offset, body.Count);
            Console.WriteLine($"[Game] C_Interact: objectId={pkt.ObjectId}, type={pkt.Type}");

            // TODO: 상호작용 처리 후 S_InteractResult 전송
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
