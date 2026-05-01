using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[System.Serializable]
public struct EnemyPrefabEntry
{
    public string typeName;   // game.proto의 enemy_type 값과 일치 (EnemyData 이름)
    public GameObject prefab;
}

public class SpawnManager : MonoBehaviour
{
    public static SpawnManager Instance { get; private set; }

    private List<GameObject> _aliveEnemies = new List<GameObject>();
    // 현재 진행 중인 SpawnGroup 코루틴 목록
    // 웨이브 하나당 SpawnGroup 수만큼 코루틴이 동시에 실행됨
    private List<Coroutine> _activeGroupCoroutines = new List<Coroutine>();

    [Header("Network Enemy Prefabs")]
    [SerializeField] private EnemyPrefabEntry[] _enemyPrefabs = new EnemyPrefabEntry[0];

    // 서버에서 부여한 enemyId와 GameObject를 매핑해 두는 테이블
    // 네트워크 패킷으로 적을 찾거나 제거할 때 ID로 빠르게 접근하기 위해 별도 관리
    private Dictionary<int, GameObject> _networkEnemies = new Dictionary<int, GameObject>();

    // 방장이 10Hz 주기로 적 위치를 브로드캐스트하는 루프 코루틴 참조
    private Coroutine _enemySyncCoroutine;

    public int AliveEnemyCount => _aliveEnemies.Count;


    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }


    private void OnEnable()
    {
        CombatEvents.OnEnemyDied += HandleEnemyDied;
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }


    private void OnDisable()
    {
        CombatEvents.OnEnemyDied -= HandleEnemyDied;
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }


    // 씬 전환 시 적 리스트와 진행 중인 모든 스폰 코루틴 정리
    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        StopAllGroupCoroutines();
        _aliveEnemies.Clear();
        _networkEnemies.Clear();
        if (_enemySyncCoroutine != null)
        {
            StopCoroutine(_enemySyncCoroutine);
            _enemySyncCoroutine = null;
        }
        Debug.Log($"[SpawnManager] {scene.name} 씬 로드 - 초기화");
    }


    // 웨이브 시작: 기존 코루틴 중단 후 WaveData의 SpawnGroup들을 동시에 예약
    // 각 그룹은 자신의 delay만큼 기다렸다가 독립적으로 스폰
    public void StartWave(WaveData wave)
    {
        if (wave == null) return;

        StopAllGroupCoroutines();

        foreach (var group in wave.spawnGroups)
        {
            Coroutine c = StartCoroutine(SpawnGroupAfterDelay(group));
            _activeGroupCoroutines.Add(c);
        }

        Debug.Log($"[SpawnManager] 웨이브 '{wave.waveName}' 시작 - {wave.spawnGroups.Length}개 그룹 예약");
    }


    // delay 후 해당 그룹의 적을 스폰
    private IEnumerator SpawnGroupAfterDelay(SpawnGroup group)
    {
        yield return new WaitForSeconds(group.delay);

        foreach (var info in group.enemies)
        {
            for (int i = 0; i < info.count; i++)
            {
                SpawnEnemy(info.prefab);
            }
        }
    }


    private void StopAllGroupCoroutines()
    {
        foreach (var c in _activeGroupCoroutines)
        {
            if (c != null) StopCoroutine(c);
        }
        _activeGroupCoroutines.Clear();
    }


    public void SpawnEnemy(GameObject prefab)
    {
        if (prefab == null)
        {
            Debug.LogWarning("[SpawnManager] Enemy Prefab이 null입니다!");
            return;
        }

        if (prefab.GetComponent<Enemy>() == null)
        {
            Debug.LogWarning($"[SpawnManager] {prefab.name}에 Enemy 컴포넌트가 없습니다!");
            return;
        }

        // 방장: 서버에 스폰 요청 전송, S_EnemySpawn으로 돌아오면 SpawnNetworkEnemy()에서 생성
        if (NetworkManager.Instance.IsHost)
        {
            SendEnemySpawnRequest(prefab);
            return;
        }
        // 비호스트: StartWave를 직접 호출해서는 안 됨 - S_EnemySpawn 수신 대기
    }


    private void SendEnemySpawnRequest(GameObject prefab)
    {
        var enemyComp = prefab.GetComponent<Enemy>();
        if (enemyComp == null || enemyComp.Data == null)
        {
            Debug.LogWarning($"[SpawnManager] {prefab.name}에서 EnemyData를 찾을 수 없습니다.");
            return;
        }

        Vector3 pos = GetSpawnPosition();
        NetworkManager.Instance.SendGame(GameProto.GamePacketId.CEnemySpawned, new GameProto.C_EnemySpawned
        {
            EnemyType = enemyComp.Data.enemyName,
            PosX = pos.x,
            PosY = pos.y,
            PosZ = pos.z,
            MaxHp = enemyComp.Data.maxHealth
        });
        Debug.Log($"[SpawnManager] C_EnemySpawned 전송 - type: {enemyComp.Data.enemyName}, pos: {pos}");
    }


    // 서버 패킷으로 전달된 enemyId와 enemyType을 기반으로 적을 생성
    // enemyId는 서버가 부여한 고유 식별자이며 DespawnNetworkEnemy 호출 시 사용됨
    public void SpawnNetworkEnemy(int enemyId, string enemyType, Vector3 pos)
    {
        GameObject prefab = null;
        foreach (var entry in _enemyPrefabs)
        {
            if (entry.typeName == enemyType)
            {
                prefab = entry.prefab;
                break;
            }
        }

        if (prefab == null)
        {
            Debug.LogWarning($"[SpawnManager] enemyType '{enemyType}'에 해당하는 프리팹을 찾을 수 없습니다.");
            return;
        }

        GameObject enemy = Instantiate(prefab, pos, Quaternion.identity);
        enemy.GetComponent<Enemy>().NetworkId = enemyId;
        _aliveEnemies.Add(enemy);
        _networkEnemies[enemyId] = enemy;

        // 비호스트는 AI와 NavMeshAgent를 끔
        // NetworkEnemy가 Transform.position을 직접 Lerp로 제어하는데,
        // NavMeshAgent가 활성화되어 있으면 위치를 덮어써 보간이 무시됨
        if (!NetworkManager.Instance.IsHost)
        {
            var ai = enemy.GetComponent<EnemyAI>();
            if (ai != null) ai.enabled = false;
            var move = enemy.GetComponent<EnemyMove>();
            if (move != null) move.enabled = false;
            enemy.AddComponent<NetworkEnemy>();
        }

        // 방장이면 씬 내 첫 적 스폰 시점에 동기화 루프 시작
        if (NetworkManager.Instance.IsHost && _enemySyncCoroutine == null)
            _enemySyncCoroutine = StartCoroutine(EnemySyncLoop());

        CombatEvents.EnemySpawned(enemy);
        Debug.Log($"[SpawnManager] 네트워크 적 스폰 완료 - enemyId: {enemyId}, type: {enemyType}");
    }


    // enemyId로 NetworkEnemy 컴포넌트를 반환 - 비호스트의 보간 업데이트에 사용됨
    public NetworkEnemy GetNetworkEnemy(int enemyId)
    {
        if (_networkEnemies.TryGetValue(enemyId, out var go))
            return go?.GetComponent<NetworkEnemy>();
        return null;
    }


    // enemyId로 Enemy 컴포넌트를 반환 - 호스트/비호스트 모두 사용 가능
    public Enemy GetEnemyById(int enemyId)
    {
        if (_networkEnemies.TryGetValue(enemyId, out var go))
            return go?.GetComponent<Enemy>();
        return null;
    }


    // 서버 패킷으로 전달된 enemyId에 해당하는 적을 제거
    // Die()를 호출해 EnemyAI.OnDeath() 흐름을 그대로 따르게 함
    public void DespawnNetworkEnemy(int enemyId)
    {
        if (!_networkEnemies.ContainsKey(enemyId))
        {
            Debug.LogWarning($"[SpawnManager] enemyId {enemyId}에 해당하는 네트워크 적을 찾을 수 없습니다.");
            return;
        }

        GameObject enemyObj = _networkEnemies[enemyId];
        if (enemyObj != null)
        {
            Enemy enemy = enemyObj.GetComponent<Enemy>();
            if (enemy != null) enemy.Die();
        }

        _networkEnemies.Remove(enemyId);
        Debug.Log($"[SpawnManager] 네트워크 적 제거 처리 완료 - enemyId: {enemyId}");
    }


    // 100ms 간격으로 모든 적 위치를 비호스트에게 브로드캐스트
    // 첫 적이 스폰될 때 시작되고, 씬 전환 시 정리됨
    private IEnumerator EnemySyncLoop()
    {
        var wait = new WaitForSeconds(0.1f); // 100ms = 10Hz
        while (true)
        {
            if (_networkEnemies.Count > 0)
                SendEnemySync();
            yield return wait;
        }
    }


    // 현재 살아있는 모든 적의 위치, 회전, 상태를 한 패킷에 묶어 전송
    // Dead 상태 적은 이미 DespawnNetworkEnemy로 처리되므로 동기화 불필요
    private void SendEnemySync()
    {
        var pkt = new GameProto.C_EnemySync();

        foreach (var kv in _networkEnemies)
        {
            int id = kv.Key;
            GameObject go = kv.Value;
            if (go == null) continue;

            var enemyComp = go.GetComponent<Enemy>();
            var ai = go.GetComponent<EnemyAI>();
            if (enemyComp == null || ai == null) continue;

            // Dead 상태 적은 이미 사망 처리 중이므로 위치 동기화 대상에서 제외
            if (ai.CurrentState == EnemyAI.State.Dead) continue;

            int stateInt = (int)ai.CurrentState;
            Vector3 pos = go.transform.position;
            float rotY = go.transform.eulerAngles.y;

            var syncData = new GameProto.EnemySyncData
            {
                EnemyId = id,
                PosX = pos.x,
                PosY = pos.y,
                PosZ = pos.z,
                RotY = rotY,
                State = stateInt
            };
            pkt.Enemies.Add(syncData);
        }

        NetworkManager.Instance.SendGame(GameProto.GamePacketId.CEnemySync, pkt);
    }


    public void ClearAllEnemies()
    {
        foreach (var enemy in _aliveEnemies)
        {
            if (enemy != null) Destroy(enemy);
        }
        _aliveEnemies.Clear();
        Debug.Log("[SpawnManager] 모든 적 제거됨");
    }


    private void HandleEnemyDied(GameObject enemy)
    {
        if (_aliveEnemies.Contains(enemy))
        {
            _aliveEnemies.Remove(enemy);
            Debug.Log($"[SpawnManager] 적 사망. 남은 적: {_aliveEnemies.Count}");

            if (_aliveEnemies.Count == 0)
            {
                StageEvents.AllEnemiesDefeated();
                Debug.Log("[SpawnManager] 모든 적 처치!");
            }
        }
    }


    private Vector3 GetSpawnPosition()
    {
        GameObject[] spawnPoints = GameObject.FindGameObjectsWithTag("EnemySpawnPoint");

        if (spawnPoints.Length == 0)
        {
            Debug.LogWarning("[SpawnManager] EnemySpawnPoint 태그 오브젝트가 없습니다. Vector3.zero 반환");
            return Vector3.zero;
        }

        return spawnPoints[Random.Range(0, spawnPoints.Length)].transform.position;
    }
}
