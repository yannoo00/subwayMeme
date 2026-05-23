using GameProto;
using Unity.Services.Lobbies.Models;
using UnityEngine;

// 게임 서버에서 오는 S_ 패킷 처리
// 항상 메인 스레드에서 호출됨
public static class ClientGamePacketHandler
{
    // =============================================
    // EnteringGame 구간 패킷
    // =============================================

    // 게임 서버 입장 완료: 내 역할(호스트 여부)과 현재 플레이어 목록 수신
    public static void Handle_S_EnterGame(byte[] body)
    {
        if (GameManager.Instance.CurrentState != GameState.EnteringGame) return;

        var pkt = S_EnterGame.Parser.ParseFrom(body);
        NetworkManager.Instance.IsHost = pkt.IsHost;

        Debug.Log($"[Game] S_EnterGame: isHost={pkt.IsHost}, players={pkt.Players.Count}");

        // pkt.Players에는 나 자신도 포함되어 있어서 내 PlayerId는 PlayerRegistry 안에서 걸러냄
        PlayerRegistry.Instance.SpawnPlayers(pkt.Players);

        // TODO: 호스트라면 NavMesh / 적 AI 권한 활성화

        // 씬 로드 + 서버 접속 완료 신호 - 서버가 전원 준비 확인 후 S_GameStart 발행
        NetworkManager.Instance.SendGame(GamePacketId.CReady, new C_Ready());
    }

    // 게임 시작: 서버가 생성한 seed로 맵 생성 
    public static void Handle_S_GameStart(byte[] body)
    {
        if (GameManager.Instance.CurrentState != GameState.EnteringGame) return;

        var pkt = S_GameStart.Parser.ParseFrom(body);

        Debug.Log($"[Game] S_GameStart: seed={pkt.MapSeed}");

        GameManager.Instance.StartGame((int)pkt.MapSeed);
    }




    // =============================================
    // Playing 구간 패킷 - SceneLoading 중 무시
    // =============================================

    // 다른 플레이어 추가 입장
    public static void Handle_S_PlayerEntered(byte[] body)
    {
        var pkt = S_PlayerEntered.Parser.ParseFrom(body);

        Debug.Log($"[Game] S_PlayerEntered: playerId={pkt.Player.PlayerId}, name={pkt.Player.PlayerName}");

        PlayerRegistry.Instance.SpawnRemotePlayer(pkt.Player);
    }

    // 플레이어 퇴장
    public static void Handle_S_PlayerLeft(byte[] body)
    {
        var pkt = S_PlayerLeft.Parser.ParseFrom(body);

        Debug.Log($"[Game] S_PlayerLeft: playerId={pkt.PlayerId}");

        PlayerRegistry.Instance.RemovePlayer(pkt.PlayerId);
    }

    // 호스트 변경: 내가 새 호스트인지 확인 후 권한 전환
    public static void Handle_S_HostChanged(byte[] body)
    {
        var pkt = S_HostChanged.Parser.ParseFrom(body);
        NetworkManager.Instance.IsHost = pkt.NewHostId == NetworkManager.Instance.MyPlayerId;

        Debug.Log($"[Game] S_HostChanged: newHostId={pkt.NewHostId}, iAmHost={NetworkManager.Instance.IsHost}");

        // TODO: IsHost true 시 NavMesh 실행 권한 활성화, 적 AI 컴포넌트 활성화
    }




    // =============================================
    // Playing 구간 패킷 - SceneLoading 중 무시
    // =============================================

    // 다른 플레이어 이동: 해당 오브젝트 위치 보간 적용
    public static void Handle_S_Move(byte[] body)
    {
        if (GameManager.Instance.CurrentState != GameState.Playing) return;

        var pkt = S_Move.Parser.ParseFrom(body);

        var np = PlayerRegistry.Instance.Get(pkt.PlayerId);
        if (np == null) return;

        np.SetTargetState(
            new UnityEngine.Vector3(pkt.PosX, pkt.PosY, pkt.PosZ),
            pkt.RotY,
            pkt.IsMoving
        );
    }

    // 다른 플레이어 공격 애니메이션 재생
    public static void Handle_S_Attack(byte[] body)
    {
        if (GameManager.Instance.CurrentState != GameState.Playing) return;

        var pkt = S_Attack.Parser.ParseFrom(body);

        var np = PlayerRegistry.Instance.Get(pkt.PlayerId);
        if (np == null) return;

        np.TriggerAttackAnim();
    }

    // 적 스폰 (서버 권위)
    public static void Handle_S_EnemySpawn(byte[] body)
    {
        if (GameManager.Instance.CurrentState != GameState.Playing) return;

        var pkt = S_EnemySpawn.Parser.ParseFrom(body);

        Debug.Log($"[Game] S_EnemySpawn: enemyId={pkt.EnemyId}, type={pkt.EnemyType}");

        var pos = new Vector3(pkt.PosX, pkt.PosY, pkt.PosZ);
        SpawnManager.Instance.SpawnNetworkEnemy(pkt.EnemyId, pkt.EnemyType, pos);
    }

    // 적 위치/상태 동기화 (호스트 → 서버 → 비호스트)
    public static void Handle_S_EnemySync(byte[] body)
    {
        if (GameManager.Instance.CurrentState != GameState.Playing) return;
        if (NetworkManager.Instance.IsHost) return; // 방장은 자신이 AI를 직접 실행

        var pkt = S_EnemySync.Parser.ParseFrom(body);

        foreach (var data in pkt.Enemies)
        {
            var ne = SpawnManager.Instance.GetNetworkEnemy(data.EnemyId);
            if (ne == null) continue;

            ne.SetTargetState(
                new Vector3(data.PosX, data.PosY, data.PosZ),
                data.RotY,
                data.State
            );
        }
    }

    // 적 피격 (서버 권위 HP)
    public static void Handle_S_EnemyDamaged(byte[] body)
    {
        if (GameManager.Instance.CurrentState != GameState.Playing) return;

        var pkt = S_EnemyDamaged.Parser.ParseFrom(body);

        Debug.Log($"[Game] S_EnemyDamaged: enemyId={pkt.EnemyId}, hp={pkt.CurrentHp}");

        var enemy = SpawnManager.Instance.GetEnemyById(pkt.EnemyId);
        if (enemy == null) return;

        enemy.ApplyNetworkDamage(pkt.CurrentHp);
    }

    // 적 사망
    public static void Handle_S_EnemyDied(byte[] body)
    {
        if (GameManager.Instance.CurrentState != GameState.Playing) return;

        var pkt = S_EnemyDied.Parser.ParseFrom(body);

        Debug.Log($"[Game] S_EnemyDied: enemyId={pkt.EnemyId}");

        SpawnManager.Instance.DespawnNetworkEnemy(pkt.EnemyId);
    }

    // 내 캐릭터 또는 다른 플레이어 피격
    public static void Handle_S_PlayerDamaged(byte[] body)
    {
        if (GameManager.Instance.CurrentState != GameState.Playing) return;

        var pkt = S_PlayerDamaged.Parser.ParseFrom(body);

        bool isMe = pkt.PlayerId == NetworkManager.Instance.MyPlayerId;
        Debug.Log($"[Game] S_PlayerDamaged: playerId={pkt.PlayerId}, dmg={pkt.Damage}, hp={pkt.CurrentHp}");

        if (isMe)
        {
            var playerObj = GameObject.FindWithTag("Player");
            playerObj?.GetComponent<PlayerStats>()?.ApplyServerDamage(pkt.Damage, pkt.CurrentHp);
        }
        // else: 다른 플레이어 피격 이펙트는 추후 구현
    }

    // 플레이어 사망
    public static void Handle_S_PlayerDied(byte[] body)
    {
        if (GameManager.Instance.CurrentState != GameState.Playing) return;

        var pkt = S_PlayerDied.Parser.ParseFrom(body);

        bool isMe = pkt.PlayerId == NetworkManager.Instance.MyPlayerId;
        Debug.Log($"[Game] S_PlayerDied: playerId={pkt.PlayerId}");

        if (isMe)
        {
            // PlayerStats.Die가 ApplyServerDamage 경로에서 먼저 호출됐을 수 있음 - 멱등이라 안전
            PlayerRegistry.Instance.NotifyLocalPlayerDied();
        }
        else
        {
            PlayerRegistry.Instance.NotifyRemotePlayerDied(pkt.PlayerId);
            // TODO: 해당 플레이어 오브젝트 사망 애니메이션
            // 사망 애니메이션은 죽는 모션 다음에 나와야하니까 일단 '누구 죽음' event를 발생시켜야함
        }
    }

    // 웨이브 시작 (서버 권위)
    public static void Handle_S_WaveStart(byte[] body)
    {
        if (GameManager.Instance.CurrentState != GameState.Playing) return;

        var pkt = S_WaveStart.Parser.ParseFrom(body);

        Debug.Log($"[Game] S_WaveStart: waveIndex={pkt.WaveIndex}");

        // TODO: SpawnManager에 waveIndex에 해당하는 WaveData 스폰 요청
        // TODO: 웨이브 시작 UI 연출
    }

    // 타이머 동기화 (서버 권위)
    public static void Handle_S_TimerSync(byte[] body)
    {
        if (GameManager.Instance.CurrentState != GameState.Playing) return;

        var pkt = S_TimerSync.Parser.ParseFrom(body);

        // TODO: HUD 타이머 표시 갱신 (pkt.Remaining)
        // StageManager의 타이머를 서버 값으로 보정
    }

    // 특정 플레이어 하차 알림
    public static void Handle_S_PlayerExited(byte[] body)
    {
        if (GameManager.Instance.CurrentState != GameState.Playing) return;

        var pkt = S_PlayerExited.Parser.ParseFrom(body);

        Debug.Log($"[Game] S_PlayerExited: playerId={pkt.PlayerId} ({pkt.ExitedCount}/{pkt.TotalCount})");

        // TODO: HUD에 하차 카운트 표시 갱신
    }

    // 특정 플레이어 탑승 알림
    public static void Handle_S_PlayerBoarded(byte[] body)
    {
        if (GameManager.Instance.CurrentState != GameState.Playing) return;

        var pkt = S_PlayerBoarded.Parser.ParseFrom(body);

        Debug.Log($"[Game] S_PlayerBoarded: playerId={pkt.PlayerId} ({pkt.BoardedCount}/{pkt.TotalCount})");

        // TODO: HUD에 탑승 카운트 표시 갱신
    }

    // 게임 클리어
    public static void Handle_S_GameClear(byte[] body)
    {
        if (GameManager.Instance.CurrentState != GameState.Playing) return;

        Debug.Log("[Game] S_GameClear");

        GameManager.Instance.ClearGame();

        // TODO: 클리어 UI 표시
        // TODO: 결과 화면 (처치 수, 플레이 시간 등)
    }

    // 게임 오버
    public static void Handle_S_GameOver(byte[] body)
    {
        if (GameManager.Instance.CurrentState != GameState.Playing) return;

        Debug.Log("[Game] S_GameOver");

        GameManager.Instance.EndGame();

        // TODO: 게임오버 UI 표시
    }

    // 상호작용 결과 (자판기, 상점 등)
    public static void Handle_S_InteractResult(byte[] body)
    {
        if (GameManager.Instance.CurrentState != GameState.Playing) return;

        var pkt = S_InteractResult.Parser.ParseFrom(body);

        Debug.Log($"[Game] S_InteractResult: success={pkt.Success}, hp={pkt.CurrentHp}, gold={pkt.Gold}");

        if (!pkt.Success) return;

        // TODO: pkt.CurrentHp로 HP바 갱신
        // TODO: pkt.Gold로 골드 UI 갱신
    }

    // 변이 획득 확정: 본인이면 효과 적용, 남이면 외관 표현만
    public static void Handle_S_MutationResult(byte[] body)
    {
        if (GameManager.Instance.CurrentState != GameState.Playing) return;

        var pkt = S_MutationResult.Parser.ParseFrom(body);

        Debug.Log($"[Game] S_MutationResult: playerId={pkt.PlayerId}, mutationId={pkt.MutationId}");

        MutationManager.Instance.ApplyMutationFromServer(pkt.PlayerId, pkt.MutationId);
    }
}
