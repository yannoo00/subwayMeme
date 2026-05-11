using System.Collections.Generic;
using GameProto;
using UnityEngine;
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

    // === Public 메서드 ===

    // S_EnterGame 수신 시 호출: 씬이 이미 로드된 상태이므로 즉시 스폰
    // self도 여기서 함께 처리 - SelectedCharacter가 결정된 시점이라 PlayerCharacterBinder가 정상 동작
    public void SpawnPlayers(IEnumerable<GamePlayerInfo> players)
    {
        Vector3 spawnPos = FindSpawnPosition();
        foreach (var info in players)
        {
            if (info.PlayerId == NetworkManager.Instance.MyPlayerId)
                SpawnLocalPlayer(info, spawnPos);
            else
                SpawnRemotePlayer(info, spawnPos);
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
        if (_remotePlayers.TryGetValue(playerId, out var np))
        {
            Destroy(np.gameObject);
            _remotePlayers.Remove(playerId);
            Debug.Log($"[PlayerRegistry] 원격 플레이어 제거: id={playerId}");
        }
    }

    // PlayerId로 원격 플레이어 조회
    public NetworkPlayer Get(int playerId)
    {
        _remotePlayers.TryGetValue(playerId, out var np);
        return np;
    }

    // === Private 메서드 ===

    // 씬 전환 시 기존 스폰된 오브젝트를 새 씬의 SpawnPoint로 이동
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
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
        np.Init(info.PlayerId, info.PlayerName);
        _remotePlayers[info.PlayerId] = np;

        Debug.Log($"[PlayerRegistry] 원격 플레이어 스폰: {info.PlayerName} (id={info.PlayerId})");
    }

    private Vector3 FindSpawnPosition()
    {
        GameObject spawnPoint = GameObject.FindWithTag("PlayerSpawnPoint");
        if (spawnPoint == null)
            Debug.LogWarning("[PlayerRegistry] PlayerSpawnPoint 태그 오브젝트를 찾지 못했습니다. 원점에 스폰합니다.");
        return spawnPoint != null ? spawnPoint.transform.position : Vector3.zero;
    }
}
