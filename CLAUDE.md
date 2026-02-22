# Subway Roguelike - 프로젝트 가이드

## 프로젝트 개요
Unity 6.0 기반 3D 로그라이크 게임. 지하철 테마의 던전 탐험 게임.

## 학습 방식
이 프로젝트는 Unity와 C#을 배우면서 직접 구현하는 것이 목표입니다.

**Claude의 역할:**
- 코드 제안 및 설명 (직접 작성보다 가이드 제공)
- 설계 방향 제안
- 개념 설명 및 질문 답변
- 에러 디버깅 도움
- 반복적인 작업은 요청 시 직접 구현
- **코드나 구현 방식 제안 시 반드시 "왜" 그렇게 하는지 이유 설명**

**사용자가 직접 하는 것:**
- 코드 작성 및 타이핑
- Unity Editor에서 컴포넌트 설정
- 프리팹 생성 및 씬 구성
- 테스트 및 디버깅 시도

## 기술 스택
- **엔진**: Unity 6.0 (6000.0.34f1)
- **언어**: C# (.NET Standard 2.1)
- **IDE**: Visual Studio Code / Visual Studio

## 프로젝트 구조
```
Assets/
├── Scripts/           # C# 스크립트
│   ├── Core/          # 게임 핵심 시스템 (GameManager, EventSystem)
│   ├── Player/        # 플레이어 관련 (이동, 전투, 인벤토리)
│   ├── Enemy/         # 적 AI 및 행동
│   ├── Dungeon/       # 맵 생성 시스템 (노선도 그래프, 맵 생성기)
│   ├── Items/         # 아이템 시스템
│   └── UI/            # UI 관련 스크립트
├── Prefabs/           # 프리팹
├── Scenes/            # 씬 파일
├── Materials/         # 머티리얼
├── Textures/          # 텍스처
├── Models/            # 3D 모델
├── Audio/             # 사운드 및 음악
└── Resources/         # 런타임 로드 에셋
```

## 코딩 컨벤션

### 네이밍 규칙
- **클래스명**: PascalCase (예: `PlayerController`)
- **public 변수**: camelCase (예: `moveSpeed`)
- **private 변수**: _camelCase (예: `_currentHealth`)
- **상수**: UPPER_SNAKE_CASE (예: `MAX_HEALTH`)
- **메서드명**: PascalCase (예: `TakeDamage()`)

### 스크립트 구조
```csharp
using UnityEngine;

public class ExampleClass : MonoBehaviour
{
    // === Inspector 변수 ===
    [Header("Settings")]
    [SerializeField] private float _speed = 5f;

    // === Private 변수 ===
    private Rigidbody _rb;

    // === Unity 생명주기 ===
    private void Awake() { }
    private void Start() { }
    private void Update() { }
    private void FixedUpdate() { }

    // === Public 메서드 ===
    public void DoSomething() { }

    // === Private 메서드 ===
    private void HelperMethod() { }
}
```

### 중요 규칙
1. `[SerializeField]` 사용하여 Inspector 노출 (public 변수 지양)
2. 모든 MonoBehaviour에 `RequireComponent` 어트리뷰트 고려
3. 매직 넘버 대신 상수 또는 SerializeField 사용
4. 주석은 "왜"를 설명 (코드가 "무엇"을 설명)

## 게임 기획

### 게임 목표
**종착역까지 도달하기** - 지하철을 타고 여러 역을 거쳐 마지막 종착역에 도착

### 핵심 게임 루프
```
[지하철 탑승] → [지하철 내 전투] → [역 도착] → [역 파밍/회복] → [재탑승] → 반복
                    ↓                  ↓              ↓
               던전/전투          문 열림         상점/회복
               주요 이벤트        하차 가능       장비 정리
                                                    ↓
                                            문 닫히기 전 탑승!
```

### 맵 구성
| 맵 | 역할 | 특징 |
|----|------|------|
| 지하철 내부 | 전투/던전 | 이동 중 적 출현, 주요 이벤트 |
| 지하철 역 | 휴식/파밍 | 안전 지대, 상점, 회복 |

### 지하철 시스템
- **이동 중**: 지하철 내부에서 전투/이벤트 진행
- **역 도착**: 문 열림 → 하차하여 파밍/회복
- **재탑승**: 문 닫히기 전에 반드시 탑승!
- **놓침 페널티**: 지하철 놓치면 역에 대량의 적 출현 → n분 생존 시 다음 지하철 탑승 가능

### 플레이어 캐릭터
- 다양한 캐릭터 선택 가능 (각자 고유 능력/스탯)

### 적 분류
| 등급 | 설명 |
|------|------|
| 소형몹 | 약함, 다수 출현 |
| 중형몹 | 보통, 특수 패턴 보유 |
| 대형몹 | 강함, 높은 체력 |
| 보스몹 | 매우 강함, 고유 패턴, 큰 보상 |

### 무기 시스템
- **근접**: 칼, 몽둥이, 도끼 등
- **원거리**: 총, 활, 투척 무기 등
- 다양한 무기 확장 예정

### 사망/부활 시스템
- Permadeath: 죽으면 처음부터
- 다크소울식 자원 회수: 죽은 위치까지 도달하면 잃었던 자원 일부 회수

### 전투 시스템
- **시점**: 숄더뷰 (3인칭)
- **근접 공격**: 다양한 근접 무기
- **원거리 공격**: 다양한 원거리 무기

## 스테이지 진행 시스템 상세

### 맵 타입 (MapType)
게임은 두 가지 맵 타입을 순환하며 진행됩니다:
- **Station (역)**: 안전 지대, 상호작용 및 휴식 공간
- **Subway (지하철)**: 전투 공간, 적 스폰 영역

### 게임 진행 흐름도

```
[메인 메뉴]
    ↓ (게임 시작)
[출발 역] ← 최초 시작 지점 (MapType: Station)
    ↓ (지하철 탑승)
┌──────────────────────────────────────────┐
│ [지하철 내부] (MapType: Subway)           │
│   ↓ 출발 시 적 스폰                        │
│   ↓ n분 경과                              │
│ [역 도착] (MapType: Station)              │
│   ↓ 문 열림 → 하차 가능                   │
│   ↓ 역에서 n분 경과                       │
│   ↓                                      │
│   ├─ [탑승 성공] → 다음 구간으로          │
│   │     └─ 남은 적도 함께 탑승            │
│   │                                      │
│   └─ [탑승 실패] → 패널티 발동            │
│         └─ 강력한 몬스터 침입             │
│         └─ n분 생존 시 다음 지하철 탑승   │
└──────────────────────────────────────────┘
    ↓ (반복)
[최종 역] → 게임 클리어!
```

### 스테이지별 상세 로직

#### 1. 메인 메뉴
- **상태**: `GameState.Menu`
- **씬**: `MainMenu.scene`
- **기능**:
  - 게임 시작 버튼
  - 설정, 종료 등
- **다음 단계**: "게임 시작" 클릭 시 → 출발 역으로 이동

#### 2. 출발 역 (최초 진입)
- **상태**: `GameState.Station`
- **맵 타입**: `MapType.Station`
- **씬**: `Station_Start.scene` (또는 일반 역 씬)
- **특징**:
  - 게임 시작 후 최초로 진입하는 안전 지대
  - 적 없음
  - 튜토리얼 또는 기본 설명 제공 가능
- **플레이어 행동**:
  - 역 탐색
  - 지하철 탑승 준비
  - 지하철 탑승 시 → 지하철 내부로 전환

#### 3. 지하철 내부 (전투 구간)
- **상태**: `GameState.Subway`
- **맵 타입**: `MapType.Subway`
- **씬**: `Subway_Combat.scene`
- **진행 순서**:

  **3-1. 지하철 출발**
  - 플레이어가 지하철에 탑승하면 출발
  - `StageEvents.OnSubwayDeparted?.Invoke()` 발생
  - 이전 역에서 남은 적이 있다면 함께 탑승 (스폰)

  **3-2. 적 스폰**
  - 출발과 동시에 적 스폰 시작
  - `SpawnManager`가 현재 스테이지에 맞는 WaveData 기반으로 적 생성
  - 스폰 위치: 지하철 내부 Spawn Points

  **3-3. 전투 진행**
  - 플레이어는 지하철 내에서 적과 전투
  - 적을 모두 처치하지 않아도 다음 역 도착 가능

  **3-4. 역 도착 타이머**
  - 지하철 출발 후 n분(예: 3분) 경과 시 자동으로 역 도착
  - 타이머 UI 표시: "다음 역까지: 02:35"
  - 타이머 종료 시 → 역 도착 이벤트 발생

#### 4. 역 도착 (휴식/상호작용 구간)
- **상태**: `GameState.Station`
- **맵 타입**: `MapType.Station`
- **씬**: `Station_Normal.scene`
- **진행 순서**:

  **4-1. 문 열림**
  - 역 도착 시 지하철 문 자동 개방
  - `StageEvents.OnStationArrived?.Invoke(stationNumber)` 발생
  - 플레이어는 역으로 하차 가능 (선택 사항)
  - **중요**: 지하철에서 처치하지 못한 적은 그대로 지하철 내부에 남아있음

  **4-2. 역 내부 상태**
  - **적이 0명인 경우**:
    - 역 내 상호작용 요소 활성화 (자판기, 상점, 회복 아이템 등)
    - 플레이어는 자유롭게 휴식, 강화, 아이템 구매 가능
    - UI: "역에서 휴식 중... 다음 출발까지: 01:30"

  - **적이 남아있는 경우**:
    - 역 내 상호작용 요소 비활성화 (잠금 상태)
    - UI 경고: "적이 남아있습니다!"
    - 적은 계속해서 플레이어를 쫓아옴 (navmesh가 역에도 bake 되어있음)

  **4-3. 재출발 타이머**
  - 역 도착 후 n분(예: 2분) 경과 시 지하철 재출발
  - 타이머 UI 표시: "지하철 출발까지: 01:45"
  - 출발 10초 전 경고: "곧 출발합니다!"
  - 타이머 종료 시 → 지하철 문 닫힘 + 출발

#### 5. 지하철 재탑승 판정
- **5-1. 탑승 성공**
  - 플레이어가 출발 시간 전에 지하철 내부에 있는 경우
  - 지하철에 남아있던 적도 함께 탑승 (계속 스폰 상태 유지)
  - 새로운 적 추가 스폰 (다음 구간 WaveData)
  - → 다음 지하철 구간으로 진행

- **5-2. 탑승 실패 (놓침 페널티)**
  - 플레이어가 출발 시간까지 지하철 내부에 없는 경우
  - `StageEvents.OnTrainMissed?.Invoke()` 발생
  - **패널티 발동**:
    - 역으로 강력한 몬스터 침입 (특수 Wave 스폰)
    - UI: "지하철을 놓쳤습니다! 다음 지하철까지 생존하세요!"
    - 생존 타이머 시작: n분(예: 3분)

  - **생존 성공**:
    - n분 동안 살아남으면 다음 지하철 도착
    - 침입한 몬스터 자동 소멸
    - 다시 정상 흐름으로 복귀 (지하철 탑승 가능)

  - **생존 실패**:
    - 플레이어 사망 → 게임 오버

#### 5.1 반복
- 지하철 탑승, 적과의 전투, 역 도착으로 이어지는 3, 4, 5 과정을 반복한다. 
- 등장하는 역의 수는 그 때 그 때 다르다. 

#### 6. 최종 역 (게임 클리어)
- **상태**: `GameState.GameClear`
- **조건**: 마지막 스테이지(예: 10번째 역) 도착
- **씬**: `Station_Final.scene` (또는 일반 역 씬 + 클리어 UI)
- **처리**:
  - `StageEvents.OnGameCleared?.Invoke()` 발생
  - 승리 UI 표시
  - 통계 화면 (처치한 적 수, 플레이 시간 등)
  - 메인 메뉴로 돌아가기

### 스테이지 관리 핵심 변수

#### StageManager가 관리해야 할 데이터:
```csharp
// 현재 맵 타입
public enum MapType
{
    Station,  // 역
    Subway    // 지하철
}

// 현재 스테이지 정보
private int _currentStageNumber;      // 현재 스테이지 번호 (0 = 출발역, 1 = 첫 지하철, ...)
private MapType _currentMapType;      // 현재 맵 타입
private int _totalStages = 20;        // 총 스테이지 수 (출발역 + 지하철*10 + 역*9 + 최종역)

// 타이머
private float _subwayTimer;           // 지하철 내 역 도착까지 남은 시간
private float _stationTimer;          // 역 내 재출발까지 남은 시간
private float _survivalTimer;         // 놓침 패널티 시 생존 타이머

// 설정값
[SerializeField] private float _subwayDuration = 180f;     // 지하철 주행 시간 (3분)
[SerializeField] private float _stationDuration = 120f;    // 역 대기 시간 (2분)
[SerializeField] private float _survivalDuration = 180f;   // 생존 요구 시간 (3분)
```

### 씬 구조

#### 최소 필요 씬:
```
Scenes/
├── Persistent.scene          # DontDestroyOnLoad 오브젝트 (GameManager, EventSystem)
├── MainMenu.scene            # 메인 메뉴
├── Subway.scene              # 지하철 내부 (재사용)
└── Station.scene             # 일반 역 (재사용, 출발역/경유역/최종역 모두 사용)
```

#### 씬 전환 흐름:
```
MainMenu → Station (출발역)
       ↓
Station ↔ Subway (반복)
       ↓
Station (최종역) → MainMenu
```

### 적 처리 규칙

#### 지하철 → 역 전환 시:
- 지하철에 남은 적은 **유지** (Destroy 하지 않음)
- SpawnManager는 `_activeEnemies` 리스트에 계속 보관
- 플레이어가 역에 있을 때 적은 지하철 내부에만 존재
- 역 내 상호작용 가능 여부는 `_activeEnemies.Count == 0`로 판정

#### 역 → 지하철 전환 시:
- 이전에 남은 적은 그대로 유지
- 새로운 WaveData 기반 적 추가 스폰
- 두 그룹의 적이 함께 존재

#### 지하철 놓침 시:
- 지하철 내부의 적은 **제거** (지하철이 떠났으므로)
- 역에 특수 Wave(강력한 몬스터) 스폰
- 생존 성공 시 특수 Wave 제거

### 상호작용 요소 (역 내부)

#### 자판기 (Vending Machine):
- **기능**: HP 회복, 버프 아이템 구매
- **활성화 조건**: 지하철 내 남은 적 == 0
- **비활성화 시**: "지하철에 적이 남아있습니다" 메시지 표시

#### 상점 (Shop):
- **기능**: 무기 구매/업그레이드, 패시브 아이템 구매
- **활성화 조건**: 지하철 내 남은 적 == 0
- **화폐**: 적 처치로 획득한 골드

#### 회복 아이템:
- **기능**: 무료 소량 회복
- **활성화 조건**: 지하철 내 남은 적 == 0
- **제한**: 역당 1회 사용 가능

### 난이도 조절

#### 스테이지별 난이도 상승:
- 스테이지가 진행될수록 WaveData의 적 수 증가
- 후반부로 갈수록 강력한 적 등장 비율 증가
- 보스몹은 특정 스테이지(예: 5번째, 10번째 지하철)에서만 등장

#### 놓침 패널티 난이도:
- 침입하는 몬스터는 현재 스테이지보다 1-2단계 높은 난이도
- 생존 타이머는 고정 (3분) 또는 스테이지에 따라 증가

## 맵 생성 시스템 (Slay the Spire 방식 기반)

### 개요
매 플레이(Try)마다 다른 맵을 경험할 수 있도록 동적으로 노선도를 생성합니다.
Slay the Spire의 맵 생성 알고리즘을 지하철 테마에 맞게 변형합니다.
아래 내용에서 제안하는 상세 코드와 구현 방식은 예시일 뿐입니다. 

**핵심 차이점**: Slay the Spire에서는 노드 간 이동이 단순 클릭이지만,
이 게임에서는 **역(노드) 사이를 지하철(엣지)로 이동하며 전투**합니다.
즉, 노드 = 역, 엣지 = 지하철 구간(전투 발생).

### 대응 관계

| Slay the Spire | 지하철 게임 | 설명 |
|----------------|------------|------|
| Grid (7x15) | 노선도 격자 (5x10) | 맵 템플릿 |
| Room (Node) | 역 (Station) | 방문 가능한 장소 |
| Path | 지하철 구간 | 두 역 사이 이동 (전투 발생) |
| Floor | 같은 깊이의 역들 | 동일 레이어 |
| Location | 역 타입 | 일반역, 상점역, 휴게역 등 |
| Boss Room | 종착역 | 보스 전투 |
| Act I/II/III | 노선 (1호선/2호선/3호선) | 환승으로 구분 |

### 맵 생성 알고리즘

#### 1단계: 격자 생성
불규칙 이등변삼각형 격자(Irregular Isometric Grid)를 생성합니다.

```
격자 크기: 5열 x 10층

   열1  열2  열3  열4  열5
층10  ○    ○    ○    ○    ○    ← 보스 직전 (휴게역 고정)
층9   ○    ○    ○    ○    ○
층8   ○    ○    ○    ○    ○
층7   ○    ○    ○    ○    ○
층6   ○    ○    ○    ○    ○    ← 보물역 고정
층5   ○    ○    ○    ○    ○    ← 이 층부터 급행역/휴게역 배치 가능
층4   ○    ○    ○    ○    ○
층3   ○    ○    ○    ○    ○
층2   ○    ○    ○    ○    ○
층1   ○    ○    ○    ○    ○    ← 일반역 고정 (시작점)
           ↑
         [종착역 (보스)]  ← 층10 위에 연결
```

- 격자 크기는 `MapGenerationConfig` ScriptableObject로 조절 가능
- 나중에 Act(노선)마다 다른 격자 크기 설정 가능

#### 2단계: 경로 생성
1층에서 시작하여 최상층까지 경로를 생성합니다. 이 과정을 N번 반복합니다.

**알고리즘**:
```
반복 (pathCount = 6회):
  1. 1층에서 랜덤 노드 선택
  2. 위층의 가장 가까운 3개 노드 중 하나와 연결 (Path 생성)
  3. 연결된 노드에서 다시 위층으로 연결
  4. 최상층까지 반복
```

**경로 생성 규칙**:
1. **처음 2개 시작점은 반드시 다른 노드** → 최소 2개 시작 경로 보장
2. **경로는 서로 교차할 수 없음** → 노선도의 시각적 정합성 유지

```
생성 예시 (6회 반복 후):

       [○]
      /    \
  [○]      [○]
   |     /   |
  [○]─[○]  [○]
   |         |
  [○]      [○]
    \     /
     [○]
     [○]    ← 시작점 최소 2개
```

#### 3단계: 미연결 노드 제거
경로가 하나도 연결되지 않은 노드는 삭제합니다.

```
생성 전:  ○  ○  ○  ○  ○   (5개)
생성 후:  ○  ○     ○       (3개, 연결 없는 2개 제거)
```

#### 4단계: 역 타입(Location) 할당

##### 고정 배치:
| 층 | 역 타입 | 이유 |
|----|---------|------|
| 1층 | 일반역 (Normal) | 게임 시작, 기본 전투로 워밍업 |
| 6층 | 보물역 (Treasure) | 중반부 보상, 동기 부여 |
| 10층 (최상층) | 휴게역 (RestSite) | 보스 전 마지막 준비 |

##### 확률 기반 배치 (나머지 층):
| 역 타입 | 확률 | 비고 |
|---------|------|------|
| 일반역 (Normal) | 45% | 가장 흔한 전투 구간 |
| 이벤트역 (Event) | 22% | 랜덤 이벤트 (전투 50%, 비전투 50%) |
| 보물역 (Treasure) | 8% | 무료 아이템 |
| 휴게역 (RestSite) | 12% | HP 회복 |
| 상점역 (Merchant) | 5% | 아이템 구매 |
| 급행역 (Elite) | 8% (후반 16%) | 강력한 적 |

##### 할당 규칙 오버라이드:
규칙에 위배되면 확률을 다시 굴려서 규칙을 만족할 때까지 반복합니다.

1. **급행역/휴게역은 N층 이하 배치 불가** (기본: 5층 미만 불가)
   - 초반에 너무 강한 적이나 회복 기회를 방지
2. **급행역/상점역/휴게역은 연속 배치 불가**
   - 같은 경로에서 2개가 직접 연결되면 안 됨 (예: 휴게역→휴게역 ❌)
3. **같은 노드에서 나가는 경로의 목적지는 모두 다른 타입**
   - 분기점에서 선택의 의미를 보장 (예: 일반역/상점역/급행역 중 선택)
   - 분기가 많을수록 다양한 역 타입이 보장됨
4. **보스 직전 층(9층)에 휴게역 불가**
   - 10층이 이미 휴게역 고정이므로 연속 방지


### 맵 데이터 구조



### 게임 흐름과 맵의 통합

#### 플레이어의 한 판 진행 예시:
```
[게임 시작]
    ↓
StageMapGenerator.Generate(seed)     ← 맵 생성
    ↓
노선도 UI 표시 (선택 가능한 경로 하이라이트)
    ↓
플레이어가 다음 역 선택               ← 분기점에서 선택
    ↓
[지하철 구간 시작] (MapType: Subway)  ← 선택한 역으로 가는 전투 구간
    ↓ 타이머 종료
[선택한 역 도착] (MapType: Station)   ← 역 타입에 따라 처리
    ↓
역 타입별 로직 실행:
  - Normal: 적 잔존 여부에 따라 상호작용
  - Merchant: 상점 오픈
  - RestSite: 회복 UI
  - Event: 랜덤 이벤트
  - Elite: 강력한 적 경고
    ↓
다음 역 선택 (노선도 UI)
    ↓
반복...
    ↓
10층 도착 (휴게역) → 마지막 준비
    ↓
[종착역 (보스)] 도착 → 보스 전투 → 클리어!
```


### Act 시스템 (노선 환승)

게임을 3개 Act로 나누어 환승 개념을 구현합니다.

```
Act 1 (1호선): 10층 → 보스 → 환승
Act 2 (2호선): 10층 → 보스 → 환승
Act 3 (3호선): 10층 → 최종 보스 → 게임 클리어
```

- 각 Act마다 새로운 맵 생성 (`StageMapGenerator.Generate()` 재호출)
- Act가 올라갈수록:
  - 급행역 확률 증가 (8% → 16%)
  - 일반역 확률 감소 (45% → 37%)
  - 적 난이도 전반적 상승
- 각 노선마다 다른 테마, 적 타입, BGM 적용 가능

### 시드(Seed) 시스템

```csharp
// 같은 시드 = 같은 맵 생성 (재현 가능)
public StageMap Generate(int seed)
{
    Random.InitState(seed);
    // ... 맵 생성
}
```

- 친구와 같은 시드로 경쟁 가능
- 버그 리포트 시 시드 공유로 재현 용이
- "오늘의 시드" 같은 데일리 챌린지 가능

### 파일 구조

```
Scripts/Dungeon/
├── StageNode.cs              # 개별 역 노드 데이터
├── StageMap.cs               # 전체 노선도 (그래프)
├── StageMapGenerator.cs      # 맵 생성 알고리즘
└── MapGenerationConfig.cs    # 생성 설정 ScriptableObject
```

## 핵심 시스템 설계

### GameManager (싱글톤)
- 게임 상태 관리 (메뉴, 탑승중, 역, 일시정지, 게임오버)
- 씬 전환 관리
- 전역 이벤트 관리
- 런 데이터 관리 (현재 역, 진행 상황)

### StageManager
- 현재 칸 타입 결정 (전투/이벤트/휴식/보스)
- 역 도착 타이머 관리
- 무정차 이벤트 처리

### Player System
- 이동: CharacterController 기반, 숄더뷰
- 전투: 근접/원거리 공격
- 스탯: HP, 공격력, 방어력, 이동속도
- 인벤토리: 무기, 아이템 관리

### Enemy System
- 적 스폰 관리
- AI: 추적, 공격, 패턴
- 보스 전용 로직

### 로그라이크 요소
- Permadeath (영구 사망)
- 무작위 칸 타입/적 배치
- 런마다 달라지는 이벤트
- 선형 구조 (지하철 노선)

## 아키텍처 명세

### 아키텍처 타입
- **하이브리드**: 계층 구조(명령) + 이벤트 버스(알림)

### 레이어 정의

#### Core Layer (Scripts/Core/)
| 클래스 | 싱글톤 | 역할 |
|--------|--------|------|
| GameManager | O | 게임 상태 관리, 흐름 제어 |
| GameEvents | static | 이벤트 버스, 시스템 간 알림 |
| SceneLoader | O | 씬 전환 처리 |

#### Gameplay Layer (Scripts/Gameplay/)
| 클래스 | 싱글톤 | 역할 |
|--------|--------|------|
| StageManager | O | 지하철/역 시스템, 타이머 |
| SpawnManager | O | 적 스폰 관리 |
| CombatManager | O | 전투 판정, 데미지 계산 |

#### Entity Layer
| 폴더 | 클래스 | 싱글톤 | 역할 |
|------|--------|--------|------|
| Scripts/Player/ | PlayerController | X | 이동, 입력 처리 |
| Scripts/Player/ | PlayerStats | X | HP, 스탯 관리 |
| Scripts/Player/ | PlayerCombat | X | 공격 처리 |
| Scripts/Player/ | Inventory | X | 아이템/무기 관리 |
| Scripts/Enemy/ | EnemyAI | X | 적 행동, 추적 |
| Scripts/Enemy/ | EnemyStats | X | 적 스탯 |
| Scripts/Enemy/ | EnemyAttack | X | 적 공격 |

#### UI Layer (Scripts/UI/)
| 클래스 | 싱글톤 | 역할 |
|--------|--------|------|
| UIManager | O | UI 전체 관리 |
| HUDController | X | 인게임 HUD |
| MenuController | X | 메뉴 화면 |
| PopupController | X | 팝업 |

### 통신 규칙

#### 직접 호출 사용 시
- 상위 → 하위 명령: `GameManager.Instance.StartGame()` → `StageManager.Instance.StartRun()`
- 1:1 관계: Player → 자신의 Weapon
- 명확한 제어 흐름

#### 이벤트 사용 시
- 하위 → 상위 알림: `GameEvents.OnPlayerDied?.Invoke()`
- 1:N 알림: Player 데미지 → UI, Audio, 이펙트 동시 반응
- 시스템 간 느슨한 결합 필요 시

### GameEvents 이벤트 목록

```
// 게임 흐름
OnGameStart: Action
OnGameOver: Action
OnGamePaused: Action
OnGameResumed: Action

// 스테이지
OnStationArrived: Action<int> (역 번호)
OnTrainDeparting: Action (출발 임박)
OnTrainMissed: Action (지하철 놓침)

// 플레이어
OnHealthChanged: Action<int, int> (현재, 최대)
OnPlayerDied: Action
OnPlayerDamaged: Action<int> (데미지량)

// 전투
OnEnemyKilled: Action<Enemy>
OnBossSpawned: Action

// 아이템
OnItemPickedUp: Action<Item>
OnWeaponChanged: Action<Weapon>
```

### 이벤트 구독 규칙
- `OnEnable()`에서 구독
- `OnDisable()`에서 해제 (메모리 누수 방지)

### 멀티플레이 대비 규칙
1. Player는 싱글톤 금지 (여러 플레이어 존재 가능)
2. 서버 권한 로직: GameManager, StageManager, SpawnManager
3. 클라이언트 소유: PlayerController, PlayerStats
4. 이벤트로 통신하여 네트워크 전환 용이하게

## 개발 우선순위

### 전략: 시스템 우선, 전투는 마지막
전투 시스템(AI, 공격 메커니즘)을 마지막에 구현하더라도, 게임의 전체 흐름과 시스템은 먼저 완성할 수 있습니다.
**핵심 아이디어**: 적 스폰 → (임시 처리로 처치) → 이벤트 발생 → 스테이지 진행 로직을 먼저 완성

### 1단계: 스테이지 시스템 핵심 구현 (최우선)
**목표**: 지하철 ↔ 역을 오가는 게임의 핵심 루프 완성

#### 1-1. StageManager 확장
- MapType 열거형 추가 (Station, Subway)
- 스테이지 순서 정의 (출발역 → 지하철 → 역 → ... → 최종역)
- 타이머 시스템:
  - 지하철 주행 타이머 (n분 후 역 도착)
  - 역 대기 타이머 (n분 후 재출발)
  - 생존 타이머 (지하철 놓칠 시)
- 현재 스테이지 번호 및 타입 관리

#### 1-2. 스테이지 전환 로직
- `StartStation()`: 역 시작 로직
- `StartSubway()`: 지하철 출발 로직
- `CheckPlayerBoarding()`: 플레이어 탑승 여부 판정
- `TriggerMissedTrainPenalty()`: 놓침 패널티 처리
- `CheckGameClear()`: 최종 역 도착 시 클리어 판정

#### 1-3. 이벤트 추가
StageEvents에 다음 이벤트 추가:
```csharp
OnSubwayDeparted: Action              // 지하철 출발
OnStationArrived: Action<int>         // 역 도착 (역 번호)
OnTrainMissed: Action                 // 지하철 놓침
OnGameCleared: Action                 // 게임 클리어
OnInteractionLocked: Action           // 상호작용 잠금 (적 남음)
OnInteractionUnlocked: Action         // 상호작용 해제 (적 전멸)
```

### 2단계: 씬 관리 시스템
**목표**: 메뉴 → 역 → 지하철 씬 전환이 원활하게 작동

#### 2-1. SceneLoader 구현
- `Scripts/Core/SceneLoader.cs` 생성
- 씬 전환 메서드:
  - `LoadMainMenu()`
  - `LoadStation(int stationNumber)`
  - `LoadSubway()`
  - `RestartGame()`
- Additive Loading으로 Persistent 씬 유지
- 로딩 화면 (선택 사항)

#### 2-2. 씬 생성 및 구성
```
Scenes/
├── Persistent.scene      # GameManager, EventSystem (DontDestroyOnLoad)
├── MainMenu.scene        # 메인 메뉴 UI
├── Subway.scene          # 지하철 내부 (스폰 포인트 배치)
└── Station.scene         # 역 (상호작용 요소 배치)
```

#### 2-3. 씬 간 데이터 유지
- GameManager에서 런 데이터 관리:
  - 현재 스테이지 번호
  - 플레이어 HP, 인벤토리
  - 골드, 통계
- DontDestroyOnLoad로 플레이어 오브젝트 유지 (또는 데이터만 저장 후 재생성)

### 3단계: 게임 흐름 구현
**목표**: 메인 메뉴에서 시작해서 최종 역까지 도달하는 전체 흐름 완성

#### 3-1. 메인 메뉴
- MainMenu 씬에 UI 배치
- "게임 시작" 버튼 → SceneLoader.LoadStation(0) 호출
- GameManager 상태를 Menu → Station으로 전환

#### 3-2. 출발 역
- Station 씬 로드
- 지하철 오브젝트 배치 (탑승 가능)
- 플레이어가 지하철에 진입하면 StageManager.StartSubway() 호출

#### 3-3. 지하철 → 역 순환
- **지하철 내부**:
  - 적 스폰 (SpawnManager, 기존 시스템 활용)
  - 타이머 카운트다운 (UI 표시)
  - 타이머 종료 시 SceneLoader.LoadStation() 호출

- **역 내부**:
  - 적 남음 여부 확인 (`SpawnManager.ActiveEnemyCount > 0`)
  - 상호작용 요소 활성화/비활성화
  - 재출발 타이머 카운트다운
  - 타이머 종료 시 탑승 판정 → SceneLoader.LoadSubway()

#### 3-4. 놓침 패널티
- 플레이어가 역에 있을 때 재출발 타이머 종료 시:
  - 특수 Wave 스폰 (강력한 몬스터)
  - 생존 타이머 시작
  - 생존 성공 시 다음 지하철 도착 (정상 흐름 복귀)

#### 3-5. 게임 클리어
- 최종 스테이지 도착 시:
  - `StageManager.CheckGameClear()` 호출
  - 승리 UI 표시
  - 메인 메뉴로 돌아가기 버튼

### 4단계: UI 시스템
**목표**: 게임 상태를 플레이어에게 명확히 전달

#### 4-1. HUD (인게임)
- **HP 바**: PlayerStats 연동
- **스테이지 정보**: "3번째 역" 또는 "지하철 3호선"
- **타이머 표시**:
  - 지하철: "다음 역까지: 02:35"
  - 역: "출발까지: 01:20"
  - 놓침 패널티: "생존: 02:45"
- **남은 적 수**: "남은 적: 5" (적이 있을 때만 표시)
- **상호작용 잠금 경고**: "지하철에 적이 남아있습니다!"

#### 4-2. 게임 오버 UI
- "Game Over" 텍스트
- 재시작 버튼
- 메인 메뉴 버튼
- 통계 (처치한 적, 도달한 역)

#### 4-3. 게임 클리어 UI
- "Clear!" 애니메이션
- 최종 통계
- 메인 메뉴 버튼

#### 4-4. 상호작용 UI
- 자판기: "E키를 눌러 구매" (활성화 시)
- 자판기: "적을 모두 처치하세요" (비활성화 시)
- 지하철 탑승: "E키를 눌러 탑승"

### 5단계: 전투 없이 테스트하기
**목표**: 전투 시스템 없이도 게임 흐름 테스트 가능하게

#### 5-1. 치트 키 구현
GameManager 또는 별도 DebugManager에 추가:
```csharp
void Update()
{
    if (Input.GetKeyDown(KeyCode.K))  // K: Kill All Enemies
    {
        SpawnManager.Instance.KillAllEnemies();
    }

    if (Input.GetKeyDown(KeyCode.G))  // G: Add Gold
    {
        // 골드 추가 (아이템 시스템 구현 시)
    }

    if (Input.GetKeyDown(KeyCode.N))  // N: Next Stage
    {
        StageManager.Instance.ForceNextStage();
    }
}
```

#### 5-2. 자동 사망 옵션 (선택)
EnemyStats에 테스트용 설정 추가:
```csharp
[Header("Test Settings")]
[SerializeField] private bool _autoDeathForTesting = false;
[SerializeField] private float _autoDeathDelay = 3f;
```

#### 5-3. Inspector 버튼
SpawnManager에 에디터 전용 메서드 추가:
```csharp
[ContextMenu("Kill All Enemies")]
public void KillAllEnemies() { /* ... */ }

[ContextMenu("Spawn Test Wave")]
public void SpawnTestWave() { /* ... */ }
```

### 6단계: 아이템 시스템 기초
**목표**: 골드, 아이템 드랍, 인벤토리 기본 구조

#### 6-1. 골드 시스템
- PlayerStats에 골드 변수 추가
- 적 처치 시 골드 드랍
- UI에 골드 표시

#### 6-2. 아이템 드랍
```
Scripts/Items/
├── ItemData.cs          # ScriptableObject
├── ItemPickup.cs        # 월드에 드랍된 아이템
└── Inventory.cs         # 플레이어 인벤토리
```

#### 6-3. 상점 시스템
- 역 내 Shop 오브젝트 배치
- 골드로 아이템 구매
- 상호작용 잠금 규칙 적용 (적 남음 시 비활성화)

### 7단계: 적 AI 및 전투 구현 (최종 단계)
**목표**: 게임의 마지막 퍼즐 조각 완성

#### 7-1. 적 AI
```
Scripts/Enemy/
├── EnemyStats.cs (기존)
├── EnemyAI.cs (새로 추가)
└── EnemyAttack.cs (새로 추가)
```
- NavMeshAgent 기반 추적
- 상태 머신: Idle → Chase → Attack → Death

#### 7-2. PlayerController 개선
- CharacterController로 전환 (현재는 Transform 직접 수정)
- 중력, 충돌 처리

#### 7-3. 전투 피드백
- 히트 이펙트 (파티클)
- 사운드 (공격, 피격, 사망)
- 카메라 쉐이크

#### 7-4. 원거리 무기
- RangedWeapon 클래스
- Projectile 시스템

### 8단계: 폴리싱
- 사운드 및 음악
- 시각 효과 개선
- 밸런싱
- 버그 수정

---


## Unity 특이사항
- Unity 6는 URP (Universal Render Pipeline) 기본 사용
- 새 Input System 권장 (`UnityEngine.InputSystem`)
- Assembly Definition 파일로 컴파일 시간 최적화 고려

## 빌드 및 테스트
- 주요 플랫폼: Windows, Mac
- Play Mode 테스트 자주 실행

## 참고 자료
- [Unity Documentation](https://docs.unity3d.com/)
- [Unity Learn](https://learn.unity.com/)
- 로그라이크 레퍼런스: Hades, Dead Cells, Enter the Gungeon