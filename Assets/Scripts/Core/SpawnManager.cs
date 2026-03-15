using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SpawnManager : MonoBehaviour
{
    public static SpawnManager Instance { get; private set; }

    private List<GameObject> _aliveEnemies = new List<GameObject>();
    // 현재 진행 중인 SpawnGroup 코루틴 목록
    // 웨이브 하나당 SpawnGroup 수만큼 코루틴이 동시에 실행됨
    private List<Coroutine> _activeGroupCoroutines = new List<Coroutine>();

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

        if (prefab.GetComponent<EnemyStats>() == null)
        {
            Debug.LogWarning($"[SpawnManager] {prefab.name}에 EnemyStats가 없습니다!");
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
