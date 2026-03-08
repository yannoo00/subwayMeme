using System.Collections;
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
    [SerializeField] private int _subwayMovingDuration = 30;
    [SerializeField] private int _subwayStayingDuration = 30;

    private StageMap _stageMap;
    private MapType _currentMapType;
    private bool _isSubwayActive = false;

    private Coroutine _movingCoroutine;
    private Coroutine _stayingCoroutine;

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
        DontDestroyOnLoad(gameObject);
    }


    private void OnEnable()
    {
        StageEvents.OnAllEnemiesDefeated += HandleAllEnemiesDefeated;
    }


    private void OnDisable()
    {
        StageEvents.OnAllEnemiesDefeated -= HandleAllEnemiesDefeated;
    }


    // 타이머 종료 시 적 생존 여부에 따라 분기
    private IEnumerator SubwayMovingTimer()
    {
        yield return new WaitForSeconds(_subwayMovingDuration);

        if (SpawnManager.Instance.AliveEnemyCount > 0)
        {
            // 적이 남아있으면 Station 스킵, 현재 씬 유지
            SkipStation();
        }
        else
        {
            // 모든 적 처치 시 정상적으로 역 도착
            StartStation();
        }
    }


    private IEnumerator SubwayStayingTimer()
    {
        yield return new WaitForSeconds(_subwayStayingDuration);

        //플레이어가 탑승했는지를 체크
        //탑승하지 않았으면 일단 게임 오버 (나중에 이 부분의 로직을 구현)
        //탑승했으면 StartSubway
        StartSubway();
    }


    // 게임 1트 시작
    // GameManager에서만 사용하고, 직접 호출되는 일은 없어야 함
    public void _TryGame()
    {
        int seed = Random.Range(0, int.MaxValue);
        _stageMap = _mapGenerator.GenerateMap(seed);

        StageEvents.MapGenerated(_stageMap);  // MapUI가 받아서 노선도 UI 구성

        // Station 씬 로드 후 시작 루트 선택 UI 표시
        _currentMapType = MapType.Station;
        SceneLoader.Instance.LoadStation(() =>
        {
            StageEvents.MapOpenRequested(MapOpenReason.RouteSelection, _stageMap.floors[0]);
        });
    }


    // 역 도착: Station 씬 로드 후 역 로직 처리
    public void StartStation()
    {
        _isSubwayActive = false;
        _currentMapType = MapType.Station;

        // 지하철 주행 타이머 종료
        if (_movingCoroutine != null) StopCoroutine(_movingCoroutine);

        Debug.Log($"[StageManager] 역 도착 - floor: {_stageMap.currentNode.floor}, type: {_stageMap.currentNode.type}");

        // Station 씬 로드 완료 후 역 이벤트 발생 및 대기 타이머 시작
        SceneLoader.Instance.LoadStation(() =>
        {
            StageEvents.StationArrived(_stageMap.currentNode);

            if (_stageMap.currentNode.floor >= _stageMap.floors.Count - 1)
            {
                GameClear();
                return;
            }

            _stayingCoroutine = StartCoroutine(SubwayStayingTimer());
        });
    }


    // 지하철 출발: Subway 씬 로드 후 웨이브 스폰
    public void StartSubway()
    {
        _isSubwayActive = true;
        _currentMapType = MapType.Subway;

        // 역 대기 타이머 종료
        if (_stayingCoroutine != null) StopCoroutine(_stayingCoroutine);

        Debug.Log($"[StageManager] 지하철 출발 - floor: {_stageMap.currentNode.floor}");

        StageEvents.SubwayStarted(_stageMap.currentNode);

        // Subway 씬 로드 완료 후 스폰 및 주행 타이머 시작
        // 씬 로드 전 스폰 시도 시 SpawnPoint 탐색 실패 가능성이 있어 콜백으로 처리
        SceneLoader.Instance.LoadSubway(() =>
        {
            SpawnManager.Instance.SpawnWaveForStage(_stageMap.currentNode.floor);
            _movingCoroutine = StartCoroutine(SubwayMovingTimer());
        });
    }


    // 플레이어가 다음 역을 선택했을 때 호출 (UI에서 호출)
    public void MoveToNode(StageNode nextNode)
    {
        _stageMap.currentNode = nextNode;
        nextNode.visited = true;

        // 선택한 역으로 이동 = 지하철 출발
        StartSubway();
    }


    // 타이머 종료 시 적이 남아있으면 역을 스킵하고 현재 Subway 씬 유지
    private void SkipStation()
    {
        var nextNodes = _stageMap.currentNode.nextNodes;

        if (nextNodes == null || nextNodes.Count == 0)
        {
            Debug.LogWarning("[StageManager] SkipStation: 다음 노드가 없습니다.");
            return;
        }

        // 플레이어 선택권 없이 랜덤으로 다음 노드 결정
        StageNode nextNode = nextNodes[Random.Range(0, nextNodes.Count)];
        _stageMap.currentNode = nextNode;
        nextNode.visited = true;

        Debug.Log($"[StageManager] 역 스킵 - 다음 노드: floor {nextNode.floor}");

        // 기존 적은 유지하면서 새 웨이브 추가 스폰
        SpawnManager.Instance.SpawnWaveForStage(_stageMap.currentNode.floor);

        // 타이머 재시작
        _movingCoroutine = StartCoroutine(SubwayMovingTimer());

        // UI 경고 이벤트 발생 (HUD 등에서 구독해 경고 표시 가능)
        StageEvents.StationSkipped();
    }


    private void HandleAllEnemiesDefeated()
    {
        if (!_isSubwayActive) return;

        Debug.Log("[StageManager] 지하철 내 모든 적 처치!");
    }


    // 플레이어가 탑승 시도 시 호출 (SubwayEntrance에서 호출)
    public void HandlePlayerBoarding()
    {
        // 역 대기 타이머 중단 (탑승 완료)
        if (_stayingCoroutine != null) StopCoroutine(_stayingCoroutine);

        var nextNodes = _stageMap.currentNode.nextNodes;

        // 맵 UI 열기 (역 선택 모드)
        StageEvents.MapOpenRequested(MapOpenReason.RouteSelection, nextNodes);
    }


    private void GameClear()
    {
        Debug.Log("[StageManager] 최종 역 도달! 게임 클리어!");
        // TODO: 게임 클리어 처리 (결과 화면 등)
    }
}
