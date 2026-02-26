using System.Collections;
using NUnit.Framework.Constraints;
using UnityEditor.ShaderGraph.Internal;
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
    }


    private void OnEnable()
    {
        StageEvents.OnAllEnemiesDefeated += HandleAllEnemiesDefeated;
    }


    private void OnDisable()
    {
        StageEvents.OnAllEnemiesDefeated -= HandleAllEnemiesDefeated;
    }


    private IEnumerator SubwayMovingTimer()
    {
        yield return new WaitForSeconds(_subwayMovingDuration);
        StartStation();
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
    public void TryGame()
    {
        // 맵 생성
        int seed = Random.Range(0, int.MaxValue);
        _stageMap = _mapGenerator.GenerateMap(seed);
        StageEvents.MapGenerated(_stageMap);  // MapUI가 받아서 노선도 UI 구성

        // 출발역 설정 (임시로 0층 0번 노드 고정)
        _stageMap.currentNode = _stageMap.floors[0][0];
        StartStation();
    }


    // 역 도착: currentNode의 타입에 따라 역 로직 처리
    public void StartStation()
    {
        _isSubwayActive = false;
        _currentMapType = MapType.Station;

        // 지하철 주행 타이머 종료
        if (_movingCoroutine != null) StopCoroutine(_movingCoroutine);

        Debug.Log($"[StageManager] 역 도착 - floor: {_stageMap.currentNode.floor}, type: {_stageMap.currentNode.type}");

        StageEvents.StationArrived(_stageMap.currentNode);

        // 최종 층 도달 시 게임 클리어
        if (_stageMap.currentNode.floor >= _stageMap.floors.Count - 1)
        {
            GameClear();
            return;
        }

        // 역 대기 타이머 시작 (시간 내 미탑승 시 패널티)
        _stayingCoroutine = StartCoroutine(SubwayStayingTimer());
    }


    // 지하철 출발: currentNode.floor 기준으로 웨이브 스폰
    public void StartSubway()
    {
        _isSubwayActive = true;
        _currentMapType = MapType.Subway;

        // 역 대기 타이머 종료
        if (_stayingCoroutine != null) StopCoroutine(_stayingCoroutine);

        Debug.Log($"[StageManager] 지하철 출발 - floor: {_stageMap.currentNode.floor}");

        StageEvents.SubwayStarted(_stageMap.currentNode);
        SpawnManager.Instance.SpawnWaveForStage(_stageMap.currentNode.floor);

        // 주행 타이머 시작 (시간 경과 시 자동 역 도착)
        _movingCoroutine = StartCoroutine(SubwayMovingTimer());
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

        // 적 전멸 역 상호작용 잠금 해제 (진행은 타이머가 담당)
        StageEvents.InteractionUnlocked();
    }


    // 플레이어가 탑승 시도 시 호출 (SubwayEntrance에서 호출)
    public void HandlePlayerBoarding()
    {
        // 역 대기 타이머 중단 (탑승 완료)
        if (_stayingCoroutine != null) StopCoroutine(_stayingCoroutine);

        var nextNodes = _stageMap.currentNode.nextNodes;

        //ui에서 이동할 역 선택
        StageEvents.RouteSelectionRequired(nextNodes);
        
    }


    private void GameClear()
    {
        Debug.Log("[StageManager] 최종 역 도달! 게임 클리어!");
        // TODO: 게임 클리어 처리 (결과 화면 등)
    }
}
