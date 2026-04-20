using GameProto;
using UnityEngine;

// 게임 서버에서 오는 S_ 패킷 처리
// 항상 메인 스레드에서 호출됨 (UnitySynchronizationContext)
public static class ClientGamePacketHandler
{
    // 게임 서버 입장 완료: 내 역할(호스트 여부)과 현재 플레이어 목록 수신
    public static void Handle_S_EnterGame(byte[] body)
    {
        var pkt = S_EnterGame.Parser.ParseFrom(body);
        NetworkManager.Instance.IsHost = pkt.IsHost;

        Debug.Log($"[Game] S_EnterGame: isHost={pkt.IsHost}, players={pkt.Players.Count}");

        // TODO: 다른 플레이어 오브젝트 생성 (pkt.Players 순회)
        // TODO: 호스트라면 NavMesh / 적 AI 권한 활성화

        // 씬 로드 + 서버 접속 완료 신호 — 서버가 전원 준비 확인 후 S_GameStart 발행
        NetworkManager.Instance.SendGame(GamePacketId.CReady, new C_Ready());
    }

    // 다른 플레이어 추가 입장
    public static void Handle_S_PlayerEntered(byte[] body)
    {
        var pkt = S_PlayerEntered.Parser.ParseFrom(body);

        Debug.Log($"[Game] S_PlayerEntered: playerId={pkt.Player.PlayerId}, name={pkt.Player.PlayerName}");

        // TODO: 해당 플레이어 오브젝트 생성 및 등록
    }

    // 플레이어 퇴장
    public static void Handle_S_PlayerLeft(byte[] body)
    {
        var pkt = S_PlayerLeft.Parser.ParseFrom(body);

        Debug.Log($"[Game] S_PlayerLeft: playerId={pkt.PlayerId}");

        // TODO: 해당 플레이어 오브젝트 제거
    }

    // 호스트 변경: 내가 새 호스트인지 확인 후 권한 전환
    public static void Handle_S_HostChanged(byte[] body)
    {
        var pkt = S_HostChanged.Parser.ParseFrom(body);
        NetworkManager.Instance.IsHost = (pkt.NewHostId == NetworkManager.Instance.MyPlayerId);

        Debug.Log($"[Game] S_HostChanged: newHostId={pkt.NewHostId}, iAmHost={NetworkManager.Instance.IsHost}");

        // TODO: IsHost true 시 NavMesh 실행 권한 활성화, 적 AI 컴포넌트 활성화
    }

    // 게임 시작: 서버가 생성한 seed로 맵 생성 후 게임 시작
    public static void Handle_S_GameStart(byte[] body)
    {
        var pkt = S_GameStart.Parser.ParseFrom(body);

        Debug.Log($"[Game] S_GameStart: seed={pkt.MapSeed}");

        GameManager.Instance.StartGame((int)pkt.MapSeed);
    }

    // 다른 플레이어 이동: 해당 오브젝트 위치 보간 적용
    public static void Handle_S_Move(byte[] body)
    {
        var pkt = S_Move.Parser.ParseFrom(body);

        // TODO: playerId로 다른 플레이어 오브젝트 검색
        // TODO: transform.position Lerp 적용 (pkt.PosX, PosY, PosZ)
        // TODO: transform.rotation 적용 (pkt.RotY)
        // TODO: 애니메이터 파라미터 갱신 (pkt.IsMoving, IsDodging)
    }

    // 다른 플레이어 공격 애니메이션 재생
    public static void Handle_S_Attack(byte[] body)
    {
        var pkt = S_Attack.Parser.ParseFrom(body);

        // TODO: playerId로 다른 플레이어 오브젝트 검색
        // TODO: weaponId에 맞는 공격 애니메이션 재생
    }

    // 적 스폰 (서버 권위)
    public static void Handle_S_EnemySpawn(byte[] body)
    {
        var pkt = S_EnemySpawn.Parser.ParseFrom(body);

        Debug.Log($"[Game] S_EnemySpawn: enemyId={pkt.EnemyId}, type={pkt.EnemyType}");

        // TODO: enemyType에 맞는 프리팹을 (pkt.PosX, PosY, PosZ)에 스폰
        // TODO: enemyId를 키로 Dictionary에 등록 (이후 동기화용)
    }

    // 적 위치/상태 동기화 (호스트 → 서버 → 비호스트)
    public static void Handle_S_EnemySync(byte[] body)
    {
        var pkt = S_EnemySync.Parser.ParseFrom(body);

        // 비호스트만 처리 (호스트는 직접 NavMesh로 계산하므로 무시)
        if (NetworkManager.Instance.IsHost) return;

        // TODO: pkt.Enemies 순회하며 각 enemyId 오브젝트 위치 보간 적용
    }

    // 적 피격 (서버 권위 HP)
    public static void Handle_S_EnemyDamaged(byte[] body)
    {
        var pkt = S_EnemyDamaged.Parser.ParseFrom(body);

        // TODO: enemyId 오브젝트에 피격 이펙트 재생
        // TODO: pkt.CurrentHp로 HP바 갱신 (CurrentHp == -1이면 호스트가 계산)
    }

    // 적 사망
    public static void Handle_S_EnemyDied(byte[] body)
    {
        var pkt = S_EnemyDied.Parser.ParseFrom(body);

        Debug.Log($"[Game] S_EnemyDied: enemyId={pkt.EnemyId}");

        // TODO: enemyId 오브젝트 사망 처리 (애니메이션 → Destroy)
        // TODO: Dictionary에서 제거
    }

    // 내 캐릭터 또는 다른 플레이어 피격
    public static void Handle_S_PlayerDamaged(byte[] body)
    {
        var pkt = S_PlayerDamaged.Parser.ParseFrom(body);

        bool isMe = pkt.PlayerId == NetworkManager.Instance.MyPlayerId;
        Debug.Log($"[Game] S_PlayerDamaged: playerId={pkt.PlayerId}, dmg={pkt.Damage}, hp={pkt.CurrentHp}");

        if (isMe)
        {
            // TODO: 내 HP바 갱신 (pkt.CurrentHp)
            // TODO: 피격 이펙트, 카메라 쉐이크
        }
        else
        {
            // TODO: 해당 플레이어 오브젝트 피격 이펙트
        }
    }

    // 플레이어 사망
    public static void Handle_S_PlayerDied(byte[] body)
    {
        var pkt = S_PlayerDied.Parser.ParseFrom(body);

        bool isMe = pkt.PlayerId == NetworkManager.Instance.MyPlayerId;
        Debug.Log($"[Game] S_PlayerDied: playerId={pkt.PlayerId}");

        if (isMe)
        {
            // TODO: 사망 처리 (게임오버 UI, 또는 관전 모드 전환)
        }
        else
        {
            // TODO: 해당 플레이어 오브젝트 사망 애니메이션
        }
    }

    // 웨이브 시작 (서버 권위)
    public static void Handle_S_WaveStart(byte[] body)
    {
        var pkt = S_WaveStart.Parser.ParseFrom(body);

        Debug.Log($"[Game] S_WaveStart: waveIndex={pkt.WaveIndex}");

        // TODO: SpawnManager에 waveIndex에 해당하는 WaveData 스폰 요청
        // TODO: 웨이브 시작 UI 연출
    }

    // 타이머 동기화 (서버 권위)
    public static void Handle_S_TimerSync(byte[] body)
    {
        var pkt = S_TimerSync.Parser.ParseFrom(body);

        // TODO: HUD 타이머 표시 갱신 (pkt.Remaining)
        // StageManager의 타이머를 서버 값으로 보정
    }

    // 특정 플레이어 하차 알림
    public static void Handle_S_PlayerExited(byte[] body)
    {
        var pkt = S_PlayerExited.Parser.ParseFrom(body);

        Debug.Log($"[Game] S_PlayerExited: playerId={pkt.PlayerId} ({pkt.ExitedCount}/{pkt.TotalCount})");

        // TODO: HUD에 하차 카운트 표시 갱신
    }

    // 전원 하차 완료: 역 씬으로 전환
    public static void Handle_S_AllExited(byte[] body)
    {
        Debug.Log("[Game] S_AllExited: 전원 하차 완료");

        SceneLoader.Instance.LoadStation();
    }

    // 특정 플레이어 탑승 알림
    public static void Handle_S_PlayerBoarded(byte[] body)
    {
        var pkt = S_PlayerBoarded.Parser.ParseFrom(body);

        Debug.Log($"[Game] S_PlayerBoarded: playerId={pkt.PlayerId} ({pkt.BoardedCount}/{pkt.TotalCount})");

        // TODO: HUD에 탑승 카운트 표시 갱신
    }

    // 전원 탑승 + 경로 확정: 지하철 씬으로 전환
    public static void Handle_S_AllBoarded(byte[] body)
    {
        var pkt = S_AllBoarded.Parser.ParseFrom(body);

        Debug.Log($"[Game] S_AllBoarded: nodeIndex={pkt.NodeIndex}");

        // TODO: StageManager에 다음 노드 인덱스 전달
        SceneLoader.Instance.LoadSubway();
    }

    // 게임 클리어
    public static void Handle_S_GameClear(byte[] body)
    {
        Debug.Log("[Game] S_GameClear");

        // TODO: 클리어 UI 표시
        // TODO: 결과 화면 (처치 수, 플레이 시간 등)
    }

    // 게임 오버
    public static void Handle_S_GameOver(byte[] body)
    {
        Debug.Log("[Game] S_GameOver");

        // TODO: 게임오버 UI 표시
        GameManager.Instance.EndGame();
    }

    // 상호작용 결과 (자판기, 상점 등)
    public static void Handle_S_InteractResult(byte[] body)
    {
        var pkt = S_InteractResult.Parser.ParseFrom(body);

        Debug.Log($"[Game] S_InteractResult: success={pkt.Success}, hp={pkt.CurrentHp}, gold={pkt.Gold}");

        if (!pkt.Success) return;

        // TODO: pkt.CurrentHp로 HP바 갱신
        // TODO: pkt.Gold로 골드 UI 갱신
    }
}
