using UnityEngine;

public class StationController : MonoBehaviour
{
    // === Inspector 변수 ===
    // station 자체는 똑같고, 소환되는 상호작용(이벤트) 오브젝트만 다르다. 
    // station은 가만히 있으면서 몇 개의 오브젝트만 교체하는 방식으로 구현

    [Header("Interactable Prefabs")]
    [SerializeField] private GameObject _shopPrefab;
    [SerializeField] private GameObject _healPrefab;
    [SerializeField] private GameObject _trapPrefab;
    [SerializeField] private GameObject _boxPrefab;

    [Header("Spawn Point")]
    [SerializeField] private Transform _spawnPoint;


    // === Private 변수 ===

    private GameObject _currentInteractable;


    // === Unity 생명주기 ===

    private void OnEnable()
    {
        StageEvents.OnStationArrived += HandleStationArrived;
    }

    private void OnDisable()
    {
        StageEvents.OnStationArrived -= HandleStationArrived;
    }


    // === 이벤트 처리 ===

    private void HandleStationArrived(StageNode node)
    {
        Debug.Log($"[StationController] 역 도착 - floor: {node.floor}, type: {node.type}");

        //여기 코드 구현 방식 설명
        GameObject prefab = node.type switch
        {
            NodeType.shop => _shopPrefab,
            NodeType.heal => _healPrefab,
            NodeType.trap => _trapPrefab,
            NodeType.box  => _boxPrefab,
            _             => null
        };

        if (prefab == null)
        {
            Debug.LogWarning($"[StationController] 처리되지 않은 NodeType: {node.type}");
            return;
        }

        SpawnInteractable(prefab);
    }


    // === Private 메서드 ===

    private void SpawnInteractable(GameObject prefab)
    {
        // 이전 오브젝트 제거 (역 재방문 등 대비)
        if (_currentInteractable != null)
            Destroy(_currentInteractable);

        Vector3 position   = _spawnPoint != null ? _spawnPoint.position : transform.position;
        Quaternion rotation = _spawnPoint != null ? _spawnPoint.rotation : Quaternion.identity;

        _currentInteractable = Instantiate(prefab, position, rotation);
    }
}
