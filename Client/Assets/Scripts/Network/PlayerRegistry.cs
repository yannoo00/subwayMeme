using System.Collections.Generic;
using GameProto;
using UnityEngine;
using UnityEngine.SceneManagement;

// 게임 내 원격 플레이어 오브젝트를 관리하는 싱글톤
// S_EnterGame으로 받은 목록을 보관하다가 씬 로드 시점에 스폰
// 이후 씬 전환마다 기존 오브젝트를 새 씬의 SpawnPoint로 이동
public class PlayerRegistry : MonoBehaviour
{
    public static PlayerRegistry Instance { get; private set; }

    [Header("Prefab")]
    [SerializeField] private GameObject _remotePlayerPrefab;

    private readonly Dictionary<int, NetworkPlayer> _remotePlayers = new();

    // S_EnterGame에서 받은 플레이어 목록을 씬 로드 전까지 임시 보관
    // sceneLoaded에서 스폰 후 Clear()
    private readonly List<GamePlayerInfo> _pendingInfos = new();

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

    // S_EnterGame 수신 시 호출: 씬 로드 전이므로 목록만 저장
    public void CachePlayers(IEnumerable<GamePlayerInfo> players)
    {
        _pendingInfos.Clear();
        foreach (var info in players)
        {
            if (info.PlayerId == NetworkManager.Instance.MyPlayerId) continue;
            _pendingInfos.Add(info);
        }
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

    // PlayerId로 원격 플레이어 조회 (Step 2에서 S_Move 처리 시 사용)
    public NetworkPlayer Get(int playerId)
    {
        _remotePlayers.TryGetValue(playerId, out var np);
        return np;
    }

    // === Private 메서드 ===

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Vector3 spawnPos = FindSpawnPosition();

        // 첫 씬 로드: _pendingInfos의 정보로 오브젝트 스폰
        if (_pendingInfos.Count > 0)
        {
            foreach (var info in _pendingInfos)
                SpawnRemotePlayer(info, spawnPos);

            _pendingInfos.Clear();
            return;
        }

        // 이후 씬 전환: 이미 스폰된 오브젝트를 새 씬의 SpawnPoint로 이동
        foreach (var np in _remotePlayers.Values)
            np.transform.position = spawnPos;
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

    // 새로 로드된 씬에서 "SpawnPoint" 태그 오브젝트를 찾아 위치 반환
    // 없으면 원점 반환
    private Vector3 FindSpawnPosition()
    {
        GameObject spawnPoint = GameObject.FindWithTag("SpawnPoint");
        if (spawnPoint == null)
            Debug.LogWarning("[PlayerRegistry] SpawnPoint 태그 오브젝트를 찾지 못했습니다. 원점에 스폰합니다.");
        return spawnPoint != null ? spawnPoint.transform.position : Vector3.zero;
    }
}
