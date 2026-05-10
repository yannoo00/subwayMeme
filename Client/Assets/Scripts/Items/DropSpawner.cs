using UnityEngine;

// 적 사망 시 클라이언트 측 드롭 아이템 생성
// 네트워크 동기화 없음 - 클라이언트마다 독립적으로 실행
public class DropSpawner : MonoBehaviour
{
    public static DropSpawner Instance { get; private set; }

    [Header("드롭 프리팹")]
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


    // === 드롭 처리 ===

    private void HandleEnemyDied(GameObject enemyObj)
    {
        var enemy = enemyObj.GetComponent<Enemy>();
        if (enemy?.Data == null) return;

        Vector3 pos = enemyObj.transform.position;
        EnemyData data = enemy.Data;

        TrySpawnCurrency(data, pos);

        if (data.grade != EnemyGrade.Normal)
            SpawnMutationDrop(data.eliteDrop, pos);
    }

    private void TrySpawnCurrency(EnemyData data, Vector3 pos)
    {
        if (data.evolutionPointChance > 0f && Random.value <= data.evolutionPointChance)
        {
            var go = Instantiate(_evolutionPointPrefab, pos, Quaternion.identity);
            go.GetComponent<CurrencyPickup>()?.Init(CurrencyType.EvolutionPoint, data.evolutionPointAmount);
        }

        if (data.genePointChance > 0f && Random.value <= data.genePointChance)
        {
            var go = Instantiate(_genePointPrefab, pos, Quaternion.identity);
            go.GetComponent<CurrencyPickup>()?.Init(CurrencyType.GenePoint, data.genePointAmount);
        }
    }

    private void SpawnMutationDrop(MutationDefinition mutation, Vector3 pos)
    {
        if (mutation == null)
        {
            Debug.LogWarning("[DropSpawner] Elite 변이 드롭이 설정되지 않았습니다.");
            return;
        }

        if (_mutationPickupPrefab == null)
        {
            Debug.LogWarning("[DropSpawner] _mutationPickupPrefab이 설정되지 않았습니다.");
            return;
        }

        var go = Instantiate(_mutationPickupPrefab, pos, Quaternion.identity);
        go.GetComponent<DropItemPickup>()?.Init(mutation);
    }
}
