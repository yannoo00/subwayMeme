# 스테이지 진행 및 적 스폰 시스템

## 전체 흐름

게임은 Station(역)과 Subway(지하철) 두 씬을 번갈아 전환하며 진행된다. StageManager가 전체 흐름을 제어하고, SpawnManager가 적 스폰과 생존 추적을 담당한다. 두 매니저 모두 DontDestroyOnLoad 싱글톤이므로 씬이 전환되어도 유지된다.

## 게임 시작

GameManager가 StageManager._TryGame()을 호출하면 게임이 시작된다. 이 메서드는 랜덤 시드로 StageMap을 생성하고, Station 씬을 로드한 뒤 노선도 UI를 열어 플레이어가 첫 번째 경로를 선택하게 한다.

```csharp
public void _TryGame()
{
    int seed = Random.Range(0, int.MaxValue);
    _stageMap = _mapGenerator.GenerateMap(seed);

    StageEvents.MapGenerated(_stageMap);

    _currentMapType = MapType.Station;
    SceneLoader.Instance.LoadStation(() =>
    {
        StageEvents.MapOpenRequested(MapOpenReason.RouteSelection, _stageMap.floors[0]);
    });
}
```

## 지하철 출발 (StartSubway)

플레이어가 노선도에서 다음 역을 선택하면 MoveToNode()가 호출되고, 내부적으로 StartSubway()로 이어진다. StartSubway는 Subway 씬을 로드하고, 로드가 완료된 콜백에서 SubwayStarted 이벤트를 발행한 뒤 SubwayMovingTimer 코루틴을 시작한다.

```csharp
public void StartSubway()
{
    _isSubwayActive = true;
    _currentMapType = MapType.Subway;

    if (_stayingCoroutine != null) StopCoroutine(_stayingCoroutine);

    SceneLoader.Instance.LoadSubway(() =>
    {
        StageEvents.SubwayStarted(_stageMap.currentNode);
        _movingCoroutine = StartCoroutine(SubwayMovingTimer());
    });
}
```

씬 로드가 완료된 뒤 이벤트를 발행하는 이유는, SpawnManager가 씬 내의 EnemySpawnPoint 태그 오브젝트를 탐색해야 하기 때문이다. 씬 로드 전에 이벤트가 발행되면 스폰 포인트를 찾지 못한다.

## 적 스폰

SpawnManager는 OnEnable에서 StageEvents.OnSubwayStarted를 구독한다. SubwayStarted 이벤트가 오면 해당 StageNode의 stageData에서 랜덤 WaveData를 하나 꺼내 스폰한다.

```csharp
private void HandleSubwayStarted(StageNode node)
{
    if (node.stageData == null) return;
    SpawnWave(node.stageData.GetRandomWave());
}

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
}

public void SpawnEnemy(GameObject prefab)
{
    Vector3 spawnPosition = GetSpawnPosition();
    GameObject enemy = Instantiate(prefab, spawnPosition, Quaternion.identity);
    _aliveEnemies.Add(enemy);
    CombatEvents.EnemySpawned(enemy);
}
```

스폰 위치는 씬 내에서 "EnemySpawnPoint" 태그를 가진 오브젝트들을 FindGameObjectsWithTag로 찾아 랜덤으로 선택한다. SpawnManager가 DontDestroyOnLoad에 있어 인스펙터에서 직접 참조할 수 없기 때문에 태그 탐색을 사용한다.

## 이동 타이머와 분기 (_subwayMovingDuration: 30초)

SubwayMovingTimer는 _subwayMovingDuration(기본 30초) 대기 후 SpawnManager의 AliveEnemyCount를 확인한다.

```csharp
private IEnumerator SubwayMovingTimer()
{
    yield return new WaitForSeconds(_subwayMovingDuration);

    if (SpawnManager.Instance.AliveEnemyCount > 0)
    {
        SkipStation();
    }
    else
    {
        StartStation();
    }
}
```

적이 모두 죽어 있으면 역에 정상 도착하고, 적이 남아있으면 역을 스킵한다.

## 역 스킵 (SkipStation)

타이머 종료 시 적이 살아있으면 SkipStation이 호출된다. 현재 노드의 다음 노드 중 하나를 랜덤으로 선택해 currentNode를 갱신하고, 타이머를 다시 시작한다. 이후 StationSkipped 이벤트를 발행한다.

```csharp
private void SkipStation()
{
    var nextNodes = _stageMap.currentNode.nextNodes;
    StageNode nextNode = nextNodes[Random.Range(0, nextNodes.Count)];
    _stageMap.currentNode = nextNode;
    nextNode.visited = true;

    _movingCoroutine = StartCoroutine(SubwayMovingTimer());

    StageEvents.StationSkipped(_stageMap.currentNode);
}
```

SpawnManager는 OnStationSkipped를 구독하고 있어, 스킵 이벤트가 오면 기존 _aliveEnemies를 그대로 유지한 채 새 WaveData를 추가로 스폰한다. 이전 적과 새 적이 동시에 존재하게 된다.

```csharp
private void HandleStationSkipped(StageNode node)
{
    if (node.stageData == null) return;
    SpawnWave(node.stageData.GetRandomWave());
}
```

## 역 도착 (StartStation)

타이머 종료 시 적이 없으면 StartStation이 호출된다. Station 씬을 로드하고, 콜백에서 StationArrived 이벤트를 발행한다. 마지막 floor면 GameClear, 아니면 SubwayStayingTimer를 시작한다.

```csharp
public void StartStation()
{
    _isSubwayActive = false;
    _currentMapType = MapType.Station;

    if (_movingCoroutine != null) StopCoroutine(_movingCoroutine);

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
```

Station 씬이 로드될 때 SpawnManager는 SceneManager.sceneLoaded 콜백으로 이를 감지하고 _aliveEnemies 리스트를 초기화한다. 정상 도착 시 이미 적이 없으므로 실질적인 영향은 없지만, 예외 상황에서의 null 참조를 방지하는 안전장치다.

```csharp
private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
{
    if (scene.name == "Station")
    {
        _aliveEnemies.Clear();
    }
}
```

## 적 사망 처리

적이 사망하면 CombatEvents.OnEnemyDied가 발행되고, SpawnManager의 HandleEnemyDied가 리스트에서 해당 적을 제거한다. 리스트가 비면 StageEvents.AllEnemiesDefeated를 발행한다.

```csharp
private void HandleEnemyDied(GameObject enemy)
{
    if (_aliveEnemies.Contains(enemy))
    {
        _aliveEnemies.Remove(enemy);

        if (_aliveEnemies.Count == 0)
        {
            StageEvents.AllEnemiesDefeated();
        }
    }
}
```

현재 StageManager는 OnAllEnemiesDefeated를 구독하고 있지만 로그만 출력할 뿐 별도 동작은 없다. 분기 판단은 타이머 종료 시점에 AliveEnemyCount를 직접 확인하는 방식으로 이루어진다.

## 역 대기 및 재탑승 (_subwayStayingDuration: 30초)

역에 도착하면 SubwayStayingTimer가 시작된다. _subwayStayingDuration(기본 30초) 후 자동으로 StartSubway를 호출한다.

```csharp
private IEnumerator SubwayStayingTimer()
{
    yield return new WaitForSeconds(_subwayStayingDuration);
    StartSubway();
}
```

플레이어가 타이머 만료 전에 탑승을 시도하면 SubwayEntrance가 HandlePlayerBoarding()을 호출한다. 이 메서드는 대기 타이머를 중단하고 노선도 UI를 열어 다음 역을 선택하게 한다.

```csharp
public void HandlePlayerBoarding()
{
    if (_stayingCoroutine != null) StopCoroutine(_stayingCoroutine);
    var nextNodes = _stageMap.currentNode.nextNodes;
    StageEvents.MapOpenRequested(MapOpenReason.RouteSelection, nextNodes);
}
```

플레이어가 노선도에서 역을 선택하면 MoveToNode가 호출되고, 다시 StartSubway로 이어져 루프가 반복된다.

## 요약

전체 루프를 단순화하면 다음과 같다.

```
_TryGame()
    → Station 로드, 노선도 UI 열기
    → 플레이어가 역 선택 → MoveToNode() → StartSubway()

StartSubway()
    → Subway 로드 → SubwayStarted 이벤트
    → SpawnManager: 웨이브 스폰
    → SubwayMovingTimer 시작

SubwayMovingTimer 종료
    → 적 있음: SkipStation() → 다음 노드 선택 → StationSkipped 이벤트 → 추가 스폰 → 타이머 재시작
    → 적 없음: StartStation()

StartStation()
    → Station 로드 → StationArrived 이벤트
    → 마지막 floor면 GameClear
    → 아니면 SubwayStayingTimer 시작

SubwayStayingTimer 종료 또는 플레이어 탑승
    → StartSubway() (루프 반복)
```
