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
    // 주행 타이머 종료 후 플레이어가 하차하지 않을 때 역 스킵까지 대기하는 시간
    // 웨이브 진행 시간/간격은 WaveData에서 관리
    [SerializeField] private int _exitWaitDuration = 15;

    [Header("Survival Mode")]
    [SerializeField] private WaveData _survivalWaveData;
    [SerializeField] private float _survivalDuration = 1200f; // 20분

    private StageMap _stageMap;
    private MapType _currentMapType;
    private bool _isSubwayActive = false;
    private bool _canExit = false;

    private Coroutine _movingCoroutine;
    private Coroutine _stayingCoroutine;
    private Coroutine _exitWaitCoroutine;

    public MapType CurrentMapType => _currentMapType;
    public bool IsSubwayActive => _isSubwayActive;
    public bool CanExit => _canExit;
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




    /*
        게임 흐름:
            게임 서버 시작 -> 일단 station 진입하고 바로 경로 선택 -> subway로 이동
            subway에서 타이머 발동 -> 시간 지나면 역 도착 -> 하차 시간 안에 모든 플레이어가 하차 요청 -> station으로 이동
            -> station에서 웨이브 시작 + station 타입에 따른 이벤트 발동 -> 지하철 도착 시 이동 요청 가능
            -> 모든 플레이어가 요청 보냈을 때, 방장이 다시 경로 선택. 경로에 따라 이동
            -> 반복...
    */


    // 주행 타이머: wave.duration만큼 진행 후 역 도착 이벤트 발행
    private IEnumerator SubwayMovingTimer(float duration)
    {
        float remaining = duration;
        StageEvents.TimerTick(remaining, duration);

        while (remaining > 0f)
        {
            yield return new WaitForSeconds(1f);
            remaining -= 1f;
            StageEvents.TimerTick(remaining, duration);
        }

        _canExit = true;
        StageEvents.StationArrived(_stageMap.currentNode);

        _exitWaitCoroutine = StartCoroutine(ExitWaitTimer());
    }


    // 하차 대기 타이머: 시간 초과 시 역 스킵 발동
    private IEnumerator ExitWaitTimer()
    {
        yield return new WaitForSeconds(_exitWaitDuration);

        if (_canExit)
        {
            _canExit = false;
            SkipStation();
        }
    }


    // 역 웨이브 타이머: wave.duration만큼 진행 후 지하철 도착 이벤트 발행
    private IEnumerator SubwayStayingTimer(float duration)
    {
        float remaining = duration;
        StageEvents.TimerTick(remaining, duration);

        while (remaining > 0f)
        {
            yield return new WaitForSeconds(1f);
            remaining -= 1f;
            StageEvents.TimerTick(remaining, duration);
        }

        StageEvents.SubwayArrived(_stageMap.currentNode);
    }


    // 게임 1트 시작
    // GameManager에서만 사용하고, 직접 호출되는 일은 없어야 함
    public void _TryGame(int seed)
    {
        if (GameManager.Instance.IsSurvivalMode)
        {
            _currentMapType = MapType.Station;
            SceneLoader.Instance.LoadStation(StartSurvivalMode);
            return;
        }

        // 기존 지하철 루프 흐름
        _stageMap = _mapGenerator.GenerateMap(seed);

        StageEvents.MapGenerated(_stageMap);

        _currentMapType = MapType.Station;
        SceneLoader.Instance.LoadStation(() =>
        {
            StageEvents.SubwayArrived(null);
        });
    }


    // 서바이벌 모드: Station 씬에서 20분 웨이브 방어
    private void StartSurvivalMode()
    {
        if (_survivalWaveData != null)
            SpawnManager.Instance.StartWave(_survivalWaveData);
        else
            Debug.LogWarning("[StageManager] SurvivalMode: _survivalWaveData가 설정되지 않았습니다.");

        StageEvents.SurvivalStarted();
        _stayingCoroutine = StartCoroutine(SurvivalTimer(_survivalDuration));
    }


    private IEnumerator SurvivalTimer(float duration)
    {
        float remaining = duration;
        StageEvents.TimerTick(remaining, duration);

        while (remaining > 0f)
        {
            yield return new WaitForSeconds(1f);
            remaining -= 1f;
            StageEvents.TimerTick(remaining, duration);
        }

        GameManager.Instance.ClearGame();
    }


    // 역 하차: SubwayExit 또는 서버(S_AllExited)에서 호출
    // _canExit 검사는 SubwayExit.Interact()에서 처리하므로 여기서는 하지 않음
    // 서바이벌 모드에서는 호출되지 않음
    public void StartStation()
    {
        _canExit = false;
        _isSubwayActive = false;
        _currentMapType = MapType.Station;

        if (_exitWaitCoroutine != null) StopCoroutine(_exitWaitCoroutine);
        if (_movingCoroutine != null) StopCoroutine(_movingCoroutine);

        Debug.Log($"[StageManager] 역 하차 - floor: {_stageMap.currentNode.floor}, type: {_stageMap.currentNode.type}");

        if (_stageMap.currentNode.floor >= _stageMap.floors.Count - 1)
        {
            GameManager.Instance.OnSceneTransitionStart();
            SceneLoader.Instance.LoadStation(() =>
            {
                GameManager.Instance.OnSceneTransitionEnd();
                GameClear();
            });
            return;
        }

        GameManager.Instance.OnSceneTransitionStart();
        SceneLoader.Instance.LoadStation(() =>
        {
            GameManager.Instance.OnSceneTransitionEnd();

            // wave를 선택해 SpawnManager에 전달, duration으로 역 웨이브 타이머 시작
            WaveData wave = _stageMap.currentNode.stageData?.GetRandomWave();
            if (wave != null)
            {
                SpawnManager.Instance.StartWave(wave);
                _stayingCoroutine = StartCoroutine(SubwayStayingTimer(wave.duration));
            }
            else
            {
                Debug.LogWarning($"[StageManager] floor {_stageMap.currentNode.floor} 노드에 WaveData가 없습니다. 기본 타이머로 진행");
                _stayingCoroutine = StartCoroutine(SubwayStayingTimer(30f));
            }
        });
    }


    // 지하철 출발: MoveToNode에서만 호출
    public void StartSubway()
    {
        _isSubwayActive = true;
        _currentMapType = MapType.Subway;

        if (_stayingCoroutine != null) StopCoroutine(_stayingCoroutine);

        Debug.Log($"[StageManager] 지하철 출발 - floor: {_stageMap.currentNode.floor}");

        GameManager.Instance.OnSceneTransitionStart();
        SceneLoader.Instance.LoadSubway(() =>
        {
            GameManager.Instance.OnSceneTransitionEnd();
            StageEvents.SubwayStarted(_stageMap.currentNode);

            // wave를 선택해 SpawnManager에 전달, duration으로 주행 타이머 시작
            WaveData wave = _stageMap.currentNode.stageData?.GetRandomWave();
            if (wave != null)
            {
                SpawnManager.Instance.StartWave(wave);
                _movingCoroutine = StartCoroutine(SubwayMovingTimer(wave.duration));
            }
            else
            {
                Debug.LogWarning($"[StageManager] floor {_stageMap.currentNode.floor} 노드에 WaveData가 없습니다. 기본 타이머로 진행");
                _movingCoroutine = StartCoroutine(SubwayMovingTimer(30f));
            }
        });
    }


    // 맵 이동: 노드 선택 후 StartSubway 호출 (subway 씬 로드)
    // 서바이벌 모드에서는 호출되지 않음
    public void MoveToNode(StageNode nextNode)
    {
        if (GameManager.Instance.IsSurvivalMode) return;

        _stageMap.currentNode = nextNode;
        nextNode.visited = true;

        StartSubway();
    }


    // 하차 미시도로 역 스킵: 기존 Subway 씬 유지, 새 wave 추가
    private void SkipStation()
    {
        var nextNodes = _stageMap.currentNode.nextNodes;

        if (nextNodes == null || nextNodes.Count == 0)
        {
            Debug.LogWarning("[StageManager] SkipStation: 다음 노드가 없습니다.");
            return;
        }

        StageNode nextNode = nextNodes[Random.Range(0, nextNodes.Count)];
        _stageMap.currentNode = nextNode;
        nextNode.visited = true;

        Debug.Log($"[StageManager] 역 스킵 - 다음 노드: floor {nextNode.floor}");

        // 새 wave 선택해 SpawnManager에 추가 (기존 적 유지 + 새 wave 스폰)
        WaveData wave = nextNode.stageData?.GetRandomWave();
        if (wave != null)
        {
            SpawnManager.Instance.StartWave(wave);
            _movingCoroutine = StartCoroutine(SubwayMovingTimer(wave.duration));
        }
        else
        {
            _movingCoroutine = StartCoroutine(SubwayMovingTimer(30f));
        }

        StageEvents.StationSkipped(_stageMap.currentNode);
    }


    // 모든 적 처치: 진행 트리거 아님. 추후 보너스 로직 연결 가능
    private void HandleAllEnemiesDefeated()
    {
        Debug.Log("[StageManager] 모든 적 처치!");
    }


    // 탑승 시도 시 호출 (SubwayEntrance에서 호출)
    // 근데 이제 방장만 받아보는 것임
    public void HandlePlayerBoarding()
    {
        if (_stayingCoroutine != null) StopCoroutine(_stayingCoroutine);

        // 첫 탑승 시 currentNode가 없으므로 floors[0] 전체를 선택지로 사용
        var nextNodes = _stageMap.currentNode != null
            ? _stageMap.currentNode.nextNodes
            : _stageMap.floors[0];

        StageEvents.MapOpenRequested(MapOpenReason.RouteSelection, nextNodes);
    }



    public StageNode GetNodeByIndex(int idx)
    {
        return _stageMap.GetNodeByIndex(idx);
    }

    public int GetIndex(StageNode node)
    {
        return _stageMap.GetIndex(node);
    }



/// <summary>
/// 나중에 작업예정
/// </summary>
    private void GameClear()
    {
        Debug.Log("[StageManager] 최종 역 도달! 게임 클리어!");
    }
}
