using UnityEngine;

// 적 사망 시 클라이언트 측 드랍 아이템 생성
// 네트워크 동기화 없음 - 클라이언트마다 독립적으로 실행
public class DropSpawner : MonoBehaviour
{
    public static DropSpawner Instance { get; private set; }

    [Header("드랍 프리팹")]
    [SerializeField] private GameObject _mutationPickupPrefab;   // DropItemPickup 컴포넌트
    [SerializeField] private GameObject _evolutionPointPrefab;   // CurrencyPickup 컴포넌트
    [SerializeField] private GameObject _genePointPrefab;        // CurrencyPickup 컴포넌트


    // === 생명주기 ===

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        CombatEvents.OnEnemyDied += HandleEnemyDied;
    }

    private void OnDisable()
    {
        CombatEvents.OnEnemyDied -= HandleEnemyDied;
    }


    // === 드랍 처리 ===

    private void HandleEnemyDied(GameObject enemyObj)
    {
        var enemy = enemyObj.GetComponent<Enemy>();
        if (enemy?.Data == null) return;

        Vector3 pos = enemyObj.transform.position;
        EnemyData data = enemy.Data;

        if (data.grade == EnemyGrade.Elite)
            SpawnEliteDrop(data.eliteDrop, pos);
        else
            TrySpawnNormalDrop(data, pos);
    }

    private void SpawnEliteDrop(DropEntry drop, Vector3 pos)
    {
        if (drop == null || drop.mutation == null)
        {
            Debug.LogWarning("[DropSpawner] Elite 드랍 설정이 없습니다.");
            return;
        }

        if (_mutationPickupPrefab == null)
        {
            Debug.LogWarning("[DropSpawner] _mutationPickupPrefab이 설정되지 않았습니다.");
            return;
        }

        var go = Instantiate(_mutationPickupPrefab, pos, Quaternion.identity);
        go.GetComponent<DropItemPickup>()?.Init(drop.mutation);
    }

    private void TrySpawnNormalDrop(EnemyData data, Vector3 pos)
    {
        if (data.dropTable == null || data.dropTable.Length == 0) return;
        if (Random.value > data.dropChance) return;

        DropEntry selected = RollDropTable(data.dropTable);
        if (selected != null) SpawnDrop(selected, pos);
    }

    // 가중치 룰렛
    private DropEntry RollDropTable(DropEntry[] table)
    {
        float total = 0f;
        foreach (var e in table) total += e.weight;

        float roll = Random.Range(0f, total);
        float cumulative = 0f;

        foreach (var e in table)
        {
            cumulative += e.weight;
            if (roll <= cumulative) return e;
        }

        return null;
    }

    private void SpawnDrop(DropEntry drop, Vector3 pos)
    {
        switch (drop.type)
        {
            case DropType.Mutation:
                if (_mutationPickupPrefab == null || drop.mutation == null) break;
                var mutGo = Instantiate(_mutationPickupPrefab, pos, Quaternion.identity);
                mutGo.GetComponent<DropItemPickup>()?.Init(drop.mutation);
                break;

            case DropType.EvolutionPoint:
                if (_evolutionPointPrefab == null) break;
                var evoGo = Instantiate(_evolutionPointPrefab, pos, Quaternion.identity);
                evoGo.GetComponent<CurrencyPickup>()?.Init(DropType.EvolutionPoint, drop.pointAmount);
                break;

            case DropType.GenePoint:
                if (_genePointPrefab == null) break;
                var geneGo = Instantiate(_genePointPrefab, pos, Quaternion.identity);
                geneGo.GetComponent<CurrencyPickup>()?.Init(DropType.GenePoint, drop.pointAmount);
                break;

            case DropType.Weapon:
                if (drop.weaponPickupPrefab == null) break;
                Instantiate(drop.weaponPickupPrefab, pos, Quaternion.identity);
                break;
        }
    }
}
