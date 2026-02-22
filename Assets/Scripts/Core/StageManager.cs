using UnityEngine;


public enum MapType
{
    Station,
    Subway
}


public class StageManager : MonoBehaviour
{
    public static StageManager Instance { get; private set; }

    [Header("Stage Settings")]
    [SerializeField] private MapGenerator _mapGenerator;

    private StageMap _stageMap;
    private MapType _currentMapType;
    private bool _isSubwayActive = false;

    public MapType CurrentMapType => _currentMapType;
    public bool IsSubwayActive => _isSubwayActive;
    public StageNode CurrentNode => _stageMap?.currentNode;


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
        StageEvents.OnAllEnemiesDefeated += HandleAllEnemiesDefeated;
    }

    private void OnDisable()
    {
        StageEvents.OnAllEnemiesDefeated -= HandleAllEnemiesDefeated;
    }


    // 게임 1트 시작
    public void TryGame()
    {
        // 맵 생성
        int seed = Random.Range(0, int.MaxValue);
        _stageMap = _mapGenerator.GenerateMap(seed);

        // 출발역 설정 (임시로 0층 0번 노드 고정)
        _stageMap.currentNode = _stageMap.floors[0][0];

        StartStation();
    }


    // 역 도착: currentNode의 타입에 따라 역 로직 처리
    public void StartStation()
    {
        _isSubwayActive = false;
        _currentMapType = MapType.Station;

        Debug.Log($"[StageManager] 역 도착 - floor: {_stageMap.currentNode.floor}, type: {_stageMap.currentNode.type}");

        StageEvents.StationArrived(_stageMap.currentNode);

        // 최종 층 도달 시 게임 클리어
        if (_stageMap.currentNode.floor >= _stageMap.floors.Count - 1)
        {
            GameClear();
        }
    }


    // 지하철 출발: currentNode.floor 기준으로 웨이브 스폰
    public void StartSubway()
    {
        _isSubwayActive = true;
        _currentMapType = MapType.Subway;

        Debug.Log($"[StageManager] 지하철 출발 - floor: {_stageMap.currentNode.floor}");

        StageEvents.SubwayStarted(_stageMap.currentNode);
        //floor에 맞는 wave를 시작한다.
        SpawnManager.Instance.  SpawnWaveForStage(_stageMap.currentNode.floor);
    }


    // 플레이어가 다음 역을 선택했을 때 호출 (UI에서 호출)
    public void MoveToNode(StageNode nextNode)
    {
        _stageMap.currentNode = nextNode;
        nextNode.visited = true;

        StartSubway();
    }


    private void HandleAllEnemiesDefeated()
    {
        if (!_isSubwayActive) return;

        Debug.Log("[StageManager] 지하철 내 모든 적 처치!");

        // 적 전멸 → 역 상호작용 잠금 해제 (진행은 타이머가 담당)
        StageEvents.InteractionUnlocked();
    }


    private void GameClear()
    {
        Debug.Log("[StageManager] 최종 역 도달! 게임 클리어!");
        // TODO: 게임 클리어 처리 (결과 화면 등)
    }
}
