using System.Collections.Generic;
using GameProto;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// 게임 내 원격 플레이어 오브젝트를 관리하는 싱글톤
// S_EnterGame 수신 시 즉시 스폰 (씬이 이미 로드된 상태)
// 이후 씬 전환마다 기존 오브젝트를 새 씬의 SpawnPoint로 이동
public class PlayerRegistry : MonoBehaviour
{
    public static PlayerRegistry Instance { get; private set; }

    [Header("Prefab")]
    [SerializeField] private GameObject _localPlayerPrefab;
    [SerializeField] private GameObject _remotePlayerPrefab;

    private readonly Dictionary<int, NetworkPlayer> _remotePlayers = new();
    // 로컬 플레이어 GameObject. S_EnterGame 시점에 스폰되고 게임 종료까지 유지
    private GameObject _localPlayer;

    // 외부에서 로컬 플레이어가 필요할 때 사용 (픽업, 변이 적용 대상 등).
    // FindGameObjectWithTag("Player") 는 RemotePlayer 도 같은 태그라 잘못 잡힐 수 있어 이 참조로 대체
    public GameObject LocalPlayer => _localPlayer;
    public bool LocalPlayerAlive => _localPlayerAlive;

    // 살아있는 플레이어 수. 0이 되면 게임오버
    // 로컬 플레이어는 NetworkPlayer 컴포넌트가 없으므로 생존 여부를 별도 플래그로 추적
    private int  _alivePlayersCount;
    private bool _localPlayerAlive;

    // 관전 모드 상태 - 로컬 플레이어 사망 후 다른 생존자를 따라다님
    private bool _isSpectating;
    private int  _spectateTargetId;

    // === Unity 생명주기 ===

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // 관전 중일 때만 입력 처리 - 좌클릭: 다음 대상, 우클릭: 이전 대상
    private void Update()
    {
        if (!_isSpectating) return;
        if (Mouse.current == null) return;

        if (Mouse.current.leftButton.wasPressedThisFrame)  CycleSpectateTarget(1);
        if (Mouse.current.rightButton.wasPressedThisFrame) CycleSpectateTarget(-1);
    }

    // === Public 메서드 ===

    // S_EnterGame 수신 시 호출: 씬이 이미 로드된 상태이므로 즉시 스폰
    // self도 여기서 함께 처리 - SelectedCharacter가 결정된 시점이라 PlayerCharacterBinder가 정상 동작
    public void SpawnPlayers(IEnumerable<GamePlayerInfo> players)
    {
        // 재플레이 대비: 카운터/관전 상태를 매 게임 시작 시 초기화
        _alivePlayersCount = 0;
        _localPlayerAlive  = true;
        _isSpectating      = false;
        _spectateTargetId  = 0;

        Vector3 spawnPos = FindSpawnPosition();
        foreach (var info in players)
        {
            if (info.PlayerId == NetworkManager.Instance.MyPlayerId)
                SpawnLocalPlayer(info, spawnPos);
            else
                SpawnRemotePlayer(info, spawnPos);

            ++_alivePlayersCount;
        }
    }

    // S_PlayerEntered 수신 시 호출: 게임 도중 새 플레이어 입장
    public void SpawnRemotePlayer(GamePlayerInfo info)
    {
        SpawnRemotePlayer(info, FindSpawnPosition());
    }

    // S_PlayerLeft 수신 시 호출
    public void RemovePlayer(int playerId)
    {
        if (!_remotePlayers.TryGetValue(playerId, out var np)) return;

        // 이미 사망 처리된 플레이어는 생존 카운터에서 빠진 상태이므로 이중 감소 방지
        bool wasAlive = np.IsAlive;
        Transform npTransform = np.transform;

        // 딕셔너리에서 먼저 제거 - AnyPlayerDied 구독자가 GetNearestAlivePlayer 등으로 후속 처리할 때
        // 이 플레이어가 후보에서 빠진 상태여야 함
        _remotePlayers.Remove(playerId);
        Destroy(np.gameObject);
        Debug.Log($"[PlayerRegistry] 원격 플레이어 제거: id={playerId}");

        if (!wasAlive) return;

        --_alivePlayersCount;

        // Destroy 는 프레임 끝에 처리되므로 transform 은 이번 프레임 내 비교 가능
        PlayerEvents.AnyPlayerDied(npTransform);

        if (CheckAllDead()) return;

        // 관전 중이던 대상이 퇴장했으면 다음 생존자로 전환
        if (_isSpectating && playerId == _spectateTargetId)
            CycleSpectateTarget(1);
    }

    // 로컬 플레이어 사망 시 호출 (PlayerStats.Die, Handle_S_PlayerDied)
    // 멱등: 이미 사망 처리됐으면 무시
    public void NotifyLocalPlayerDied()
    {
        if (!_localPlayerAlive) return;
        _localPlayerAlive = false;

        --_alivePlayersCount;
        Debug.Log($"[PlayerRegistry] 로컬 플레이어 사망. 생존: {_alivePlayersCount}");

        // EnemyAI 등 외부 구독자에게 즉시 알림 - 자기 타겟이 죽었으면 5초 코루틴을 기다리지 않고 재타겟
        // IsAlive 플래그를 먼저 내린 뒤 발행하므로 AliveAllPlayerTransforms 는 이 플레이어를 제외함
        if (_localPlayer != null)
            PlayerEvents.AnyPlayerDied(_localPlayer.transform);

        if (CheckAllDead()) return;

        // 다른 생존자가 있으면 관전 모드 진입 - 첫 생존자부터 따라감
        _isSpectating = true;
        CycleSpectateTarget(1);
    }

    // 원격 플레이어 사망 시 호출 (Handle_S_PlayerDied)
    // 멱등: NetworkPlayer.IsAlive로 중복 처리 방지
    public void NotifyRemotePlayerDied(int playerId)
    {
        if (!_remotePlayers.TryGetValue(playerId, out var np)) return;
        if (!np.IsAlive) return;
        np.IsAlive = false;

        --_alivePlayersCount;
        Debug.Log($"[PlayerRegistry] 원격 플레이어 사망: id={playerId}. 생존: {_alivePlayersCount}");

        PlayerEvents.AnyPlayerDied(np.transform);

        if (CheckAllDead()) return;

        // 관전 중이고 죽은 대상이 현재 관전 대상이면 다음 생존자로 전환
        if (_isSpectating && playerId == _spectateTargetId)
            CycleSpectateTarget(1);
    }

    // 생존 플레이어가 0이면 게임오버 확정 (GameManager가 멱등 가드로 중복 방지)
    // 게임이 종료됐으면 true 반환
    private bool CheckAllDead()
    {
        if (_alivePlayersCount > 0) return false;

        GameManager.Instance.EndGame();
        return true;
    }

    // 관전 대상을 dir 방향(1=다음, -1=이전)으로 순환
    // 생존 원격 플레이어가 없으면 무시 (게임오버는 CheckAllDead가 별도 처리)
    private void CycleSpectateTarget(int dir)
    {
        var alive = AliveRemotePlayersSorted();
        if (alive.Count == 0) return;

        // 현재 대상이 목록에 없으면(첫 진입 등) FindIndex가 -1을 반환
        // dir=1이면 (-1+1)%n = 0으로 첫 번째 대상이 선택됨
        int idx = alive.FindIndex(np => np.PlayerId == _spectateTargetId);
        idx = (idx + dir + alive.Count) % alive.Count;

        var target = alive[idx];
        _spectateTargetId = target.PlayerId;
        PlayerEvents.SpectateTargetChanged(target);
    }

    // 살아있는 원격 플레이어를 PlayerId 순으로 정렬해 반환 - 순환 순서 안정성 확보
    private List<NetworkPlayer> AliveRemotePlayersSorted()
    {
        var list = new List<NetworkPlayer>();
        foreach (var np in _remotePlayers.Values)
            if (np.IsAlive) list.Add(np);

        list.Sort((a, b) => a.PlayerId.CompareTo(b.PlayerId));
        return list;
    }

    // PlayerId로 원격 플레이어 조회
    public NetworkPlayer Get(int playerId)
    {
        _remotePlayers.TryGetValue(playerId, out var np);
        return np;
    }

    // 살아있는 모든 플레이어(로컬+원격)의 Transform 열거
    // EnemyAI 가 가장 가까운 타겟을 고르거나, 광역 효과의 대상자 후보를 찾을 때 사용
    // yield 사용으로 매 호출마다 List 할당을 피함 (적이 많을 때 GC 부담 감소)
    public IEnumerable<Transform> AliveAllPlayerTransforms()
    {
        if (_localPlayer != null && _localPlayerAlive)
            yield return _localPlayer.transform;
        foreach (var np in _remotePlayers.Values)
            if (np != null && np.IsAlive) yield return np.transform;
    }

    // 가장 가까운 생존 플레이어 Transform 반환. 없으면 null.
    // 거리 비교만 하므로 sqrMagnitude 사용으로 sqrt 회피
    public Transform GetNearestAlivePlayer(Vector3 from)
    {
        Transform best = null;
        float bestSqr = float.MaxValue;
        foreach (var t in AliveAllPlayerTransforms())
        {
            float d = (t.position - from).sqrMagnitude;
            if (d < bestSqr) { bestSqr = d; best = t; }
        }
        return best;
    }

    // === Private 메서드 ===

    // 씬 전환 시 기존 스폰된 오브젝트를 새 씬의 SpawnPoint로 이동
    // DontDestroyOnLoad라 MainMenu/Lobby 등 모든 씬 로드마다 발화되므로
    // 옮길 대상이 없으면(_localPlayer 미스폰 시점) 조용히 리턴해 SpawnPoint 탐색 자체를 건너뜀
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (_localPlayer == null && _remotePlayers.Count == 0) return;

        Vector3 spawnPos = FindSpawnPosition();

        if (_localPlayer != null)
            _localPlayer.transform.position = spawnPos;

        foreach (var np in _remotePlayers.Values)
            np.transform.position = spawnPos;
    }

    // 로컬 플레이어 스폰 - S_EnterGame 시점에서 호출
    private void SpawnLocalPlayer(GamePlayerInfo info, Vector3 pos)
    {
        if (_localPlayer != null)
        {
            _localPlayer.transform.position = pos;
            Debug.Log($"[PlayerRegistry] 로컬 플레이어가 이미 존재 - 위치만 이동");
            return;
        }

        if (_localPlayerPrefab == null)
        {
            Debug.LogError("[PlayerRegistry] _localPlayerPrefab 미할당");
            return;
        }

        _localPlayer = Instantiate(_localPlayerPrefab, pos, Quaternion.identity);
        Debug.Log($"[PlayerRegistry] 로컬 플레이어 스폰: {info.PlayerName} (id={info.PlayerId})");

        // vcam 등 외부 시스템이 target을 follow할 수 있도록 알림
        PlayerEvents.LocalPlayerSpawned(_localPlayer.transform);
    }

    private void SpawnRemotePlayer(GamePlayerInfo info, Vector3 pos)
    {
        if (_remotePlayers.ContainsKey(info.PlayerId)) return;

        GameObject obj = Instantiate(_remotePlayerPrefab, pos, Quaternion.identity);
        DontDestroyOnLoad(obj);

        var np = obj.GetComponent<NetworkPlayer>();
        np.Init(info.PlayerId, info.PlayerName, info.CharacterId);
        _remotePlayers[info.PlayerId] = np;

        Debug.Log($"[PlayerRegistry] 원격 플레이어 스폰: {info.PlayerName} (id={info.PlayerId}, characterId={info.CharacterId})");
    }

    private Vector3 FindSpawnPosition()
    {
        GameObject spawnPoint = GameObject.FindWithTag("PlayerSpawnPoint");
        if (spawnPoint == null)
        {
            Debug.LogWarning("[PlayerRegistry] PlayerSpawnPoint 태그 오브젝트를 찾지 못했습니다. 원점에 스폰합니다.");

            // 진단: 현재 로드된 씬과 모든 태그 후보 출력
            var active = SceneManager.GetActiveScene();
            Debug.Log($"[Diag] 활성 씬: {active.name}, 로드됨={active.isLoaded}");
            foreach (var go in GameObject.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
            {
                if (go.tag.Contains("Spawn", System.StringComparison.OrdinalIgnoreCase))
                    Debug.Log($"[Diag] tag='{go.tag}' name='{go.name}' active={go.activeInHierarchy}");
            }            
        }
        return spawnPoint != null ? spawnPoint.transform.position : Vector3.zero;
    }
}
