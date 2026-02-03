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
│   ├── Dungeon/       # 던전 생성 및 관리
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
1. 기본 플레이어 이동 및 카메라
2. 간단한 던전 생성
3. 적 AI 기초
4. 전투 시스템
5. 아이템 시스템
6. UI 시스템
7. 사운드
8. 폴리싱

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