# 서버 구현 계획

## 목표
- 멀티플레이 매치메이킹 + 실제 인게임 플레이 동기화
- 플레이어 영구 데이터(캐릭터, 골드, 해금 등)는 로컬 JSON으로 저장
- 서버는 게임 세션 관리 및 동기화 담당

## 기술 스택
- **서버**: C# 콘솔 앱 + `SocketAsyncEventArgs` (크로스플랫폼)
  - Windows 내부: IOCP, macOS 내부: kqueue 자동 사용
  - 코드는 동일하게 작성 가능
- **클라**: Unity NetworkManager (TCP Socket)
- **DB**: MySQL + Dapper (계정, 매치 기록 등 최소한만)

## 아키텍처

```
Unity Client
    ↕ TCP (커스텀 패킷)
C# Server (SocketAsyncEventArgs)
    ↕
MySQL DB
```

## 로컬 저장 vs 서버 저장

| 데이터 | 저장 위치 |
|---|---|
| 보유 캐릭터 | 로컬 JSON |
| 해금 내역 | 로컬 JSON |
| 누적 골드 (메타) | 로컬 JSON |
| 계정 정보 | 서버 DB |
| 매치 기록 / 결과 | 서버 DB |
| 인게임 상태 (위치, HP 등) | 서버 메모리 (세션 중만) |

## 권한 분리

| 항목 | 권한 |
|---|---|
| 플레이어 이동 | 클라 입력 → 서버 브로드캐스트 |
| 공격 이벤트 | 클라 입력 → 서버 브로드캐스트 |
| 데미지 판정 | 서버 권위 |
| 적 스폰 | 서버 권위 (Host 클라가 아닌 서버가 결정) |
| 스테이지 타이머 | 서버 권위 |
| 무기 애니메이션 | 순수 클라 표현 (서버 무관) |

## 패킷 프로토콜

### 헤더 구조
```
[size: 2byte][packetId: 2byte][data: ...]
```

### 패킷 ID 목록 (예정)
```
// 공통
C_Connected
S_Connected

// 매치메이킹
C_EnterQueue      // 대기열 입장
C_LeaveQueue      // 대기열 취소
S_MatchFound      // 매치 성립, Room 정보 전달
S_PlayerJoined    // 다른 플레이어 입장 알림

// 게임 세션
C_PlayerMove      // 위치/회전 전송
S_PlayerMove      // 다른 플레이어 위치 브로드캐스트
C_PlayerAttack    // 공격 입력
S_PlayerAttack    // 공격 브로드캐스트
S_EnemySpawn      // 서버가 적 스폰 지시
S_EnemyMove       // 적 위치 동기화
S_TakeDamage      // 데미지 결과 전달
S_StageTimer      // 타이머 동기화

// 게임 흐름
S_StageStart      // 스테이지 시작
S_StageEnd        // 스테이지 종료
S_GameOver        // 게임오버
```

## 클라 준비 사항 (NetworkManager)

```
Assets/Scripts/Network/
├── NetworkManager.cs       // 연결, 송수신 관리
├── PacketHandler.cs        // 패킷 ID -> 처리 함수 매핑
├── SendBuffer.cs           // 송신 버퍼
├── RecvBuffer.cs           // 수신 버퍼 (패킷 경계 파싱)
└── Packets/
    └── PacketDefinitions.cs // 패킷 구조체 공용 정의
```

### 주의사항
- SocketAsyncEventArgs 콜백은 워커 스레드에서 호출됨
- Unity API는 메인 스레드에서만 호출 가능
- UnityMainThreadDispatcher 또는 ConcurrentQueue로 메인 스레드 전달 필요

## 구현 순서

### 클라 (서버 작업 전 완성)
- [ ] 게임오버 / 재시작 처리
- [ ] 로컬 JSON 저장 시스템 (PlayerPersistentData)

### 서버 + 클라 동시 진행
1. **소켓 통신 파이프라인**
   - 서버: SocketAsyncEventArgs Accept / Send / Recv 뼈대
   - 클라: NetworkManager 연결 / 패킷 송수신
   - 목표: 패킷 한 번 주고받기

2. **패킷 프로토콜**
   - 헤더 구조 구현
   - 직렬화 / 역직렬화
   - PacketHandler 등록 구조

3. **매치메이킹**
   - 대기열 입장 / 취소
   - 인원 충족 시 Room 생성 + 클라 통보

4. **게임 세션 동기화**
   - 플레이어 위치 동기화
   - 공격 이벤트 브로드캐스트
   - 스테이지 타이머 서버 관리
   - 적 스폰 서버 권위로 이전

## 서버 프로젝트 구조 (예정)

```
SubwayServer/
├── Program.cs
├── Network/
│   ├── Listener.cs           // Accept 처리
│   ├── Session.cs            // 클라이언트 연결 단위
│   ├── RecvBuffer.cs
│   └── SendBuffer.cs
├── Game/
│   ├── Room.cs               // 게임 방 (세션 묶음)
│   ├── RoomManager.cs        // 방 생성/관리
│   ├── Player.cs             // 서버 측 플레이어 상태
│   └── MatchMaker.cs         // 대기열 / 매칭 로직
├── Packet/
│   ├── PacketDefinitions.cs  // 클라와 공유
│   └── PacketHandler.cs
└── DB/
    └── DBManager.cs
```
