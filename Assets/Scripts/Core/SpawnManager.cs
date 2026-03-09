using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SpawnManager : MonoBehaviour
{
    public static SpawnManager Instance { get; private set; }

    private List<GameObject> _aliveEnemies = new List<GameObject>();
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
        StageEvents.OnSubwayStarted += HandleSubwayStarted;
        StageEvents.OnStationSkipped += HandleStationSkipped;
        // Station 씬 로드 시 적 리스트 정리 (Single 모드 전환으로 적 오브젝트는 자동 소멸하지만 리스트에 null 참조 남음)
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }


    private void OnDisable()
    {
        CombatEvents.OnEnemyDied -= HandleEnemyDied;
        StageEvents.OnSubwayStarted -= HandleSubwayStarted;
        StageEvents.OnStationSkipped -= HandleStationSkipped;
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }


    private void HandleSubwayStarted(StageNode node)
    {
        if (node.stageData == null)
        {
            Debug.LogWarning($"[SpawnManager] floor {node.floor} 노드에 StageData가 없습니다.");
            return;
        }
        SpawnWave(node.stageData.GetRandomWave());
    }


    // 역 스킵 시 기존 적은 건드리지 않고 새 웨이브 추가 스폰
    private void HandleStationSkipped(StageNode node)
    {
        if (node.stageData == null)
        {
            Debug.LogWarning($"[SpawnManager] floor {node.floor} 노드에 StageData가 없습니다.");
            return;
        }
        SpawnWave(node.stageData.GetRandomWave());
    }


    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "Station")
        {
            _aliveEnemies.Clear();
            Debug.Log("[SpawnManager] Station 씬 로드 - 적 리스트 초기화");
        }
    }


    // WaveData에 정의된 적들을 스폰
    public void SpawnWave(WaveData wave)
    {
        if (wave == null || wave.enemies == null) return;

        foreach (var spawnInfo in wave.enemies)
        {
            for (int i = 0; i < spawnInfo.count; i++)
            {
                SpawnEnemy(spawnInfo.prefab);
            }
        }

        Debug.Log($"[SpawnManager] 웨이브 '{wave.waveName}' 스폰 완료. 총 {_aliveEnemies.Count}마리");
    }


    // 특정 프리팹으로 적 1마리 스폰
    public void SpawnEnemy(GameObject prefab)
    {
        if (prefab == null)
        {
            Debug.LogWarning("[SpawnManager] Enemy Prefab이 null입니다!");
            return;
        }

        Vector3 spawnPosition = GetSpawnPosition();
        GameObject enemy = Instantiate(prefab, spawnPosition, Quaternion.identity);
        _aliveEnemies.Add(enemy);

        CombatEvents.EnemySpawned(enemy);
    }


    public void ClearAllEnemies()
    {
        foreach (var enemy in _aliveEnemies)
        {
            if (enemy != null)
            {
                Destroy(enemy);
            }
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


    // SpawnManager는 DontDestroyOnLoad 씬에 있고 SpawnPoint는 Subway 씬에 있으므로
    // 인스펙터 직접 참조 불가 -> 태그로 동적 탐색
    private Vector3 GetSpawnPosition()
    {
        GameObject[] spawnPoints = GameObject.FindGameObjectsWithTag("EnemySpawnPoint");

        int randomIndex = Random.Range(0, spawnPoints.Length);
        
        return spawnPoints[randomIndex].transform.position;
    }
}
