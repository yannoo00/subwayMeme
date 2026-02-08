using System.Collections.Generic;
using UnityEngine;

public class SpawnManager : MonoBehaviour
{
    public static SpawnManager Instance { get; private set; }

    [Header("Spawn Settings")]
    [SerializeField] private Transform[] _spawnPoints;

    [Header("Stage Data")]
    [SerializeField] private StageData[] _stageDataList;  // 스테이지별 데이터


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
    }

    private void OnEnable()
    {
        CombatEvents.OnEnemyDied += HandleEnemyDied;
    }

    private void OnDisable()
    {
        CombatEvents.OnEnemyDied -= HandleEnemyDied;
    }






    // 스테이지 번호에 맞는 웨이브 스폰
    public void SpawnWaveForStage(int stageNumber)
    {
        // 스테이지 번호에 맞는 StageData 찾기
        if (_stageDataList == null || stageNumber >= _stageDataList.Length)
        {
            Debug.LogWarning($"[SpawnManager] 스테이지 {stageNumber}에 해당하는 데이터가 없습니다!");
            return;
        }

        StageData stageData = _stageDataList[stageNumber];
        WaveData wave = stageData.GetRandomWave();

        if (wave == null) return;

        SpawnWave(wave);
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
        // 리스트에서 제거
        if (_aliveEnemies.Contains(enemy))
        {
            _aliveEnemies.Remove(enemy);
            Debug.Log($"[SpawnManager] 적 사망. 남은 적: {_aliveEnemies.Count}");

            // 모든 적이 처치되면 이벤트 발생
            if (_aliveEnemies.Count == 0)
            {
                StageEvents.AllEnemiesDefeated();
                Debug.Log("[SpawnManager] 모든 적 처치!");
            }
        }
    }



    //temporary logic
    private Vector3 GetSpawnPosition()
    {
        // 스폰 포인트가 설정되어 있으면 랜덤하게 선택
        if (_spawnPoints != null && _spawnPoints.Length > 0)
        {
            int randomIndex = Random.Range(0, _spawnPoints.Length);
            return _spawnPoints[randomIndex].position;
        }

        // 스폰 포인트가 없으면 SpawnManager 위치 기준으로 랜덤 오프셋
        Vector3 randomOffset = new Vector3(
            Random.Range(-5f, 5f),
            0f,
            Random.Range(-5f, 5f)
        );
        return transform.position + randomOffset;
    }
}
