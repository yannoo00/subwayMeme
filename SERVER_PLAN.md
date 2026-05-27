# 서버 구현 계획

## 목표
- 멀티플레이 매치메이킹 + 실제 인게임 플레이 동기화
- 플레이어 영구 데이터(캐릭터, 골드, 해금 등)는 로컬 JSON으로 저장
- 서버는 게임 세션 관리 및 동기화 담당

## 기술 스택

| 항목 | 기술 | 비고 |
|------|------|------|
| 서버 네트워크 | C# + SocketAsyncEventArgs | Windows: IOCP, macOS: kqueue 자동 사용 |
| 직렬화 | Protobuf (Google.Protobuf) | .proto 파일로 서버/클라 동시 생성 |
| 클라 네트워크 | TcpClient + async/await | 클라는 연결 1개라 SAEA 불필요 |
| DB | MySQL + Dapper | 계정, 매치 기록 최소한만 |
| 서버 간 통신 | localhost TCP + Protobuf (internal.proto) | LobbyServer ↔ GameServer 내부 채널 (loopback 7772) |

## 레포 구조

```
subwayMeme/                   ← git root
├── Client/                   ← Unity 프로젝트
├── Common/
│   └── Protos/               ← .proto 파일 (패킷 정의 단일 관리)
│       ├── lobby.proto
│       └── game.proto
└── Server/
    ├── ServerCore/           ← SAEA 네트워크 코어 (두 서버 공용 라이브러리)
    │   └── ServerCore.csproj
    ├── LobbyServer/          ← 항상 실행, 방 관리 + GameServer 내부 채널 연결
    │   └── LobbyServer.csproj
    └── GameServer/           ← 항상 실행, 단일 프로세스 안에서 여러 룸 동시 운영
        └── GameServer.csproj
```

## 아키텍처

```
[Mac Mini]
│
├── LobbyServer (항상 실행, 포트 7770)
│   ├── 외부 클라 접속 (Listener)
│   ├── 방 목록 관리
│   └── GameServer 내부 채널(7772) 에 능동 접속 (Connector)
│
└── GameServer (항상 실행, 단일 프로세스)
    ├── 포트 7771 (IPAddress.Any)    ← 외부 클라이언트 접속용
    │   └── GameSession → GameRoomManager 가 roomId 로 라우팅
    └── 포트 7772 (IPAddress.Loopback) ← LobbyServer 전용 내부 채널 (외부 접근 차단)
        └── InternalSession (L2G_ / G2L_ 패킷 처리)

[클라이언트 접속 흐름]
1. LobbyServer 접속 (TCP)
2. 방 생성 / 참가
3. 게임 시작
   3-1. LobbyServer → GameServer: L2G_CreateRoom 전송 → GameRoom 인스턴스 생성
   3-2. LobbyServer → 방 안 모든 클라: S_GameReady (port=7771, roomId)
4. 각 클라가 GameServer:7771 에 직접 접속
5. C_EnterGame { roomId } 전송 → GameSession 이 해당 GameRoom 에 바인딩 → 게임 진행
```

**GameServer 는 미리 실행**: 부팅 후 두 개의 Listener 를 열고 대기.
LobbyServer 가 보내는 룸 생성 요청마다 `GameRoomManager` 가 roomId 키로 GameRoom 인스턴스를 추가한다.
한 프로세스 안에서 N 개의 룸이 독립 상태(플레이어, 적, 타이머 등) 로 공존.

LobbyServer ↔ GameServer 내부 통신은 같은 SAEA 파이프라인 (`Connector` + `PacketSession`) 을 재사용해
TCP + Protobuf 로 처리한다. 패킷 정의는 `internal.proto` (L2G_CreateRoom, G2L_RoomEnded).
외부 노출을 막기 위해 GameServer 의 내부 포트는 loopback (127.0.0.1) 만 바인딩.

## 로컬 저장 vs 서버 저장

| 데이터 | 저장 위치 |
|--------|-----------|
| 보유 캐릭터 | 로컬 JSON |
| 해금 내역 | 로컬 JSON |
| 누적 골드 (메타) | 로컬 JSON |
| 계정 정보 | 서버 DB |
| 매치 기록 / 결과 | 서버 DB |
| 인게임 상태 (위치, HP 등) | 서버 메모리 (세션 중만) |

## 권한 분리

### 원칙
- **서버 권위**: 여러 클라이언트가 동시에 같은 상태를 변경할 수 있는 것 (충돌 가능성)
- **호스트 권위 + 서버 릴레이**: 단일 출처(호스트)에서만 결정이 나오는 것

| 항목 | 권한 | 비고 |
|------|------|------|
| 플레이어 이동 | 클라 입력 → 서버 릴레이 | C_Move → S_Move |
| 공격 판정 | 클라 보고, 검증 없음 | C_Attack → S_Attack 애니메이션 브로드캐스트 + 서버 HP 차감 |
| 적 AI | 호스트 클라 NavMesh 실행 → 서버 릴레이 | C_EnemySync → S_EnemySync |
| 적 HP/사망 | **서버 권위** (충돌 가능) | 여러 플레이어가 동시에 같은 적 공격 가능, S_EnemyDamaged/S_EnemyDied |
| 적 스폰 | 호스트 결정 → 서버 ID 발급 후 릴레이 | C_EnemySpawned → 서버가 ID 할당 → S_EnemySpawn 브로드캐스트 |
| 적 공격 → 플레이어 피격 | 호스트 보고 → **서버 판정** (충돌 가능) | 여러 적이 동시에 같은 플레이어 공격 가능, C_EnemyAttack → S_PlayerDamaged |
| 스테이지 타이머 | 서버 권위 | S_TimerSync (1초 주기 전송, 클라는 로컬 타이머 돌리고 서버값으로 보정) |
| 경로 선택 | 방장만 | C_SelectRoute |
| 하차/탑승 | 개인 요청 → 서버가 전원 확인 후 진행 | 전원 완료 시 S_AllExited / S_AllBoarded |
| 맵 seed | 서버 생성 | S_GameStart에 포함 |
| 호스트 이탈 | 서버가 다른 클라를 새 호스트로 지정 | S_HostChanged |
| 무기 애니메이션 | 순수 클라 표현 (서버 무관) | |

## 패킷 프로토콜

### 헤더 구조
```
[size: 2byte][packetId: 2byte][body: Protobuf bytes]
```

패킷 정의는 `Common/Protos/`의 .proto 파일로 작성.
`protoc`으로 서버(C#)와 클라(Unity C#) 코드를 동시에 자동 생성.

### 패킷 목록

#### 로비 (lobby.proto / LobbyProto)
```
C_Connected / S_Connected          // 접속
C_CreateRoom / S_RoomCreated       // 방 생성
C_JoinRoom / S_PlayerJoined        // 방 참가
C_LeaveRoom / S_PlayerLeft         // 방 퇴장
C_GetRooms / S_RoomList            // 방 목록
C_StartGame / S_GameReady          // 게임 시작 (GameServer 포트 전달)
S_CreatorChanged / S_Error         // 방장 위임 / 에러
```

#### 게임 (game.proto / GameProto)
```
// 접속/초기화
C_EnterGame / S_EnterGame          // 게임서버 접속 (로비 playerId + roomId 동봉)
S_PlayerEntered / S_PlayerLeft     // 입장/퇴장 알림
S_HostChanged                      // 호스트 변경

// 게임 시작/종료
C_Ready / S_GameStart              // 씬 로딩 완료 → 게임 시작 (seed 포함)
S_GameClear / S_GameOver           // 클리어 / 게임오버

// 이동
C_Move / S_Move                    // 플레이어 위치 동기화

// 전투 (클라 보고, 서버 HP 계산)
C_Attack / S_Attack                // 공격 입력 / 애니메이션 브로드캐스트 (거리/쿨타임 검증 없음)
S_EnemyDamaged / S_EnemyDied       // 적 피격/사망 (서버 권위 - 동시 공격 충돌 처리)
S_PlayerDamaged / S_PlayerDied     // 플레이어 피격/사망 (서버 권위 - 동시 피격 충돌 처리)

// 적 (호스트 AI, 서버 릴레이)
C_EnemySpawned                     // 호스트가 스폰 보고 → 서버가 ID 발급 후 S_EnemySpawn 릴레이
S_EnemySpawn                       // 서버가 전체 브로드캐스트 (enemyId 포함)
C_EnemySync / S_EnemySync          // 호스트가 AI 결과 보고 → 서버 릴레이
C_EnemyAttack                      // 호스트가 적 공격 보고 → 서버가 S_PlayerDamaged

// 스테이지 진행
S_WaveStart / S_TimerSync          // 웨이브 시작 / 타이머 동기화
S_StationArrived / S_SubwayStarted // 역 도착 / 지하철 출발
C_ExitSubway / S_PlayerExited / S_AllExited     // 하차 (개인 요청 → 전원 확인)
C_BoardSubway / S_PlayerBoarded / S_AllBoarded  // 탑승 (개인 요청 → 전원 확인)
C_SelectRoute                      // 방장 경로 선택
S_StationSkipped                   // 역 스킵 (미하차)

// 상호작용
C_Interact / S_InteractResult      // 상점/회복 등
```

#### 서버 간 내부 채널 (internal.proto / InternalProto)
LobbyServer ↔ GameServer 전용 통신 (외부 노출 X, localhost loopback 바인딩).
LobbyServer를 클라이언트처럼 GameServer에 TCP 연결시키고, 기존 PacketSession 파이프라인을 재사용해 패킷 송수신한다.

```
L2G_CreateRoom    // Lobby → Game: 게임 룸 생성 요청 (room_id, player_count, host_player_id)
G2L_RoomEnded     // Game → Lobby: 게임 종료 알림 (room_id) — 로비측 룸 정리용
```

기존 멀티프로세스(GameServer 1방=1프로세스) 구조를 단일 GameServer 프로세스 + 다중 룸 구조로 전환하기 위해 도입.
`ProcessManager`의 프로세스 spawn 역할을 `L2G_CreateRoom` 패킷이 대체한다.

## 서버 프로젝트 구조

```
Server/
├── ServerCore/                       ← 두 서버 공용 SAEA 라이브러리
│   ├── ServerCore.csproj
│   ├── Listener.cs                   ← AcceptAsync 처리 (수동 연결 대기)
│   ├── Connector.cs                  ← ConnectAsync 처리 (능동 연결, LobbyServer → GameServer)
│   ├── Session.cs                    ← 연결 단위 추상 (Send/Recv)
│   ├── PacketSession.cs              ← [size][id][body] 헤더 파싱 후 OnRecvPacket 위임
│   ├── RecvBuffer.cs                 ← 수신 링버퍼 (단편화 처리)
│   └── SendBuffer.cs                 ← 송신 버퍼
│
├── LobbyServer/
│   ├── LobbyServer.csproj
│   ├── Program.cs                    ← Listener(7770) + Connector(GameServer 7772)
│   ├── LobbyConfig.cs                ← appsettings.json 로드, GameServer 호스트/포트 보관
│   ├── LobbySession.cs               ← 외부 클라 세션
│   ├── GameServerSession.cs          ← GameServer 내부 채널 세션 (능동 접속)
│   ├── Room.cs                       ← 대기방
│   ├── RoomManager.cs                ← 방 목록 관리 (thread-safe)
│   └── Packet/
│       ├── Generated/                ← protoc 자동 생성 (Lobby.cs, Internal.cs)
│       ├── LobbyPacketHandler.cs     ← 외부 클라 패킷 (C_ → S_)
│       └── InternalPacketHandler.cs  ← 내부 채널 수신 (G2L_)
│
└── GameServer/
    ├── GameServer.csproj
    ├── Program.cs                    ← Listener(7771 클라용) + Listener(7772 loopback 내부)
    ├── GameRoom.cs                   ← 룸 단위 상태 (플레이어, 적, 스테이지, Enemies 보유)
    ├── GameRoomManager.cs            ← roomId 키 Dictionary 로 다중 룸 관리
    ├── GamePlayer.cs                 ← 서버 측 플레이어 상태
    ├── GameSession.cs                ← 외부 클라 세션 (C_EnterGame 수신 시 Room 동적 바인딩)
    ├── InternalSession.cs            ← LobbyServer 와의 내부 채널 세션 (수신)
    ├── EnemyManager.cs               ← 룸별 적 HP 관리 (GameRoom 이 소유)
    └── Packet/
        ├── Generated/                ← protoc 자동 생성 (Game.cs, Internal.cs)
        ├── GamePacketHandler.cs      ← 외부 클라 패킷 (C_ → S_)
        └── InternalPacketHandler.cs  ← 내부 채널 수신 (L2G_)
```

## 클라이언트 네트워크 구조

```
Client/Assets/Scripts/Network/
├── ServerSession.cs        ← TcpClient + async/await, 수신 루프
├── PacketDispatcher.cs     ← 패킷 ID → 핸들러 매핑
├── MainThreadDispatcher.cs ← 워커 스레드 → Unity 메인 스레드 전달
└── Packets/
    └── Generated/          ← protoc 자동 생성 (Common/Protos와 동일 소스)
```

**주의사항**
- 수신 콜백은 별도 async 컨텍스트에서 호출됨
- Unity API는 메인 스레드에서만 호출 가능
- `ConcurrentQueue` + `Update()`로 메인 스레드에 작업 전달

## 구현 순서

### 0단계: 사전 준비 (클라 단독)
- [ ] 게임오버 / 재시작 처리
- [ ] 로컬 JSON 저장 시스템 (PlayerPersistentData)

### 1단계: 소켓 파이프라인 ✅ 완료
- [x] ServerCore: Session / Listener / RecvBuffer / SendBuffer
- [x] ServerCore: PacketSession.cs — [size 2byte][packetId 2byte] 헤더 파싱, sealed OnRecv, 완성된 패킷만 OnRecvPacket으로 올림
- [x] LobbyServer: ServerCore 참조, 에코 서버로 접속 수락 확인

### 2단계: Protobuf 세팅 ✅ 완료
- [x] Common/Protos/lobby.proto 작성 (PacketId 열거형 + 전체 로비 패킷 정의)
- [x] Common/Protos/gen.bat — protoc 빌드 스크립트 (ASCII 인코딩 필수, 절대경로 사용)
- [x] LobbyServer.csproj에 Google.Protobuf 3.29.3 NuGet 추가
- [x] protoc 실행 → LobbyServer/Packet/Generated/Lobby.cs 생성 확인
- [x] LobbyServer/Packet/LobbyPacketHandler.cs — PacketId 배열 디스패처 + MakePacket 유틸
- [x] LobbySession: Session → PacketSession 상속으로 전환
- [x] dotnet build 통과 확인

### 3단계: 로비 시스템 ✅ 완료
- [x] LobbyServer/Lobby/LobbyPlayer.cs — 로비 내 플레이어 상태
- [x] LobbyServer/Lobby/Room.cs — 대기방 (플레이어 목록, 상태)
- [x] LobbyServer/Lobby/RoomManager.cs — 방 목록 관리 (thread-safe)
- [x] LobbyPacketHandler의 TODO 채우기 (C_CreateRoom, C_JoinRoom, C_GetRooms 실제 로직)
- [x] ProcessManager.cs — GameServer 프로세스 spawn + 포트 할당
- [x] S_GameReady로 클라에 Game Server 포트 전달

### 4단계: 게임 세션 동기화 (서버) ← 진행 중
- [x] game.proto 작성 (GameProto 네임스페이스, 전체 패킷 정의 완료)
- [x] gen.bat 업데이트 (game.proto → GameServer + Client 양쪽 생성)
- [x] GameServer.csproj에 Google.Protobuf NuGet 추가
- [x] GamePacketHandler.cs — PacketId 배열 디스패처 + MakePacket 유틸
- [x] GameSession.cs — GamePacketHandler 연결, OnDisconnected 호스트 migration 처리
- [x] GamePlayer.cs — 서버 측 플레이어 상태 (PlayerId, IsHost, IsReady, Hp)
- [x] GameRoom.cs — thread-safe 싱글턴, result record 패턴 (RemoveResult 등 6종)
  - [x] Add / Remove (호스트 이탈 시 자동 migration)
  - [x] MarkReady → 전원 준비 시 seed 생성
  - [x] MarkExited / MarkBoarded / SelectRoute → 하차/탑승 전원 확인
  - [x] ApplyPlayerDamage → 서버 권위 플레이어 HP 관리
  - [x] ResetStageState → 역 이동 후 카운터 리셋
- [x] GamePacketHandler 핸들러 전체 구현
  - [x] C_EnterGame → S_EnterGame + S_PlayerEntered
  - [x] C_Ready → S_GameStart (seed 포함)
  - [x] C_Move → S_Move 릴레이
  - [x] C_Attack → S_Attack + S_EnemyDamaged 브로드캐스트
  - [x] C_EnemySync → S_EnemySync 릴레이 (호스트 검증)
  - [x] C_EnemyAttack → S_PlayerDamaged / S_PlayerDied (서버 HP 적용)
  - [x] C_ExitSubway → S_PlayerExited / S_AllExited
  - [x] C_BoardSubway + C_SelectRoute → S_PlayerBoarded / S_AllBoarded
  - [x] C_Interact → S_InteractResult
- [x] 호스트 이탈 시 S_HostChanged 브로드캐스트
- [x] dotnet build 통과 확인
- [ ] EnemyManager.cs — 적 HP 서버 권위 관리, 스폰 ID 발급, C_EnemySpawned 수신 → S_EnemySpawn 릴레이 (웨이브 종료 판정 불필요)
- [ ] StageTimer.cs — S_TimerSync 1초 주기 전송 (S_WaveStart는 호스트 SpawnManager가 트리거, 서버는 타이머 동기화만 담당)
- ~~C_Attack 검증 로직 (쿨타임/거리) — PVE 게임 특성상 불필요, 드랍~~
- [ ] LobbyReporter.cs — 게임 종료 시 LobbyServer에 HTTP 보고

### 5단계: Unity 클라이언트 연동 ← 진행 중
#### 5-1. 네트워크 파이프라인
- [x] `MainThreadDispatcher.cs` — ConcurrentQueue + Update() 로 수신 패킷을 메인 스레드에 전달
- [x] `ServerSession.cs` — TcpClient + async/await 수신 루프, [size][id][body] 헤더 파싱, ReadExactAsync로 단편화 처리
- [x] `PacketDispatcher.cs` — 패킷 ID → Action 핸들러 딕셔너리 (순수 C# 싱글톤)
- [x] `NetworkManager.cs` — MonoBehaviour 싱글톤, 연결 관리, 로컬 상태(PlayerId/IsHost/Port), 송신 유틸

#### 5-2. 로비 패킷 처리
- [x] `LobbyPacketHandler.cs` (Unity) — S_ 수신 처리 (핵심 로직 완료, UI 연동 TODO)
  - [x] S_Connected → NetworkManager.MyPlayerId 저장
  - [x] S_RoomCreated / S_PlayerJoined / S_PlayerLeft → TODO: 방 UI 갱신
  - [x] S_RoomList → TODO: 방 목록 UI 갱신
  - [x] S_CreatorChanged → TODO: 방장 표시 갱신
  - [x] S_GameReady → GameServerPort 저장, TODO: 게임씬 로드 후 ConnectToGameAsync

#### 5-3. 게임 패킷 처리
- [x] `ClientGamePacketHandler.cs` (Unity) — S_ 수신 처리 (핵심 로직 완료, 게임오브젝트 연동 TODO)
  - [x] S_EnterGame → IsHost 저장, TODO: 플레이어 오브젝트 초기화
  - [x] S_PlayerEntered / S_PlayerLeft → TODO: 다른 플레이어 오브젝트 생성/제거
  - [x] S_HostChanged → NetworkManager.IsHost 갱신, TODO: NavMesh 권한 전환 (개발 후순위 — 호스트 이탈 처리는 기본 기능 완성 후 구현)
  - [x] S_GameStart → TODO: MapGenerator seed 전달, GameManager.StartGame()
  - [x] S_Move → TODO: 다른 플레이어 위치 Lerp
  - [x] S_Attack → TODO: 공격 애니메이션 재생
  - [x] S_EnemySpawn / S_EnemySync / S_EnemyDamaged / S_EnemyDied → TODO: 적 상태 반영
  - [x] S_PlayerDamaged / S_PlayerDied → TODO: 피격 UI, GameManager.EndGame()
  - [x] S_WaveStart / S_TimerSync → TODO: 웨이브/타이머 UI
  - [x] S_AllExited → SceneLoader.LoadStation() 연결 완료
  - [x] S_AllBoarded → SceneLoader.LoadSubway() 연결 완료
  - [x] S_GameClear / S_GameOver → TODO: 결과 UI, GameManager.EndGame()

#### 5-4. 게임씬 연동 ← 다음 작업
- [ ] SceneLoader에 로비씬 추가 (현재 Station/Subway만 있음)
- [ ] S_GameReady → 씬 로드 완료 콜백에서 ConnectToGameAsync() 호출 연결
- [ ] 로비 UI: 방 생성/참가/목록 버튼 → C_ 패킷 송신
- [ ] 게임씬: 다른 플레이어 오브젝트 프리팹 및 PlayerManager 구현
- [ ] PlayerController → C_Move / C_Attack 송신 연결
- [ ] 호스트 전용: C_EnemySync / C_EnemyAttack 송신 연결
- [ ] 씬 로드 완료 시 C_Ready 송신
- [ ] 게임씬 로드 후 GameServer 접속 → C_EnterGame 송신
- [ ] 씬 로딩 완료 시 C_Ready 송신
- [ ] PlayerController → 이동/공격 입력 시 C_Move / C_Attack 송신
- [ ] 호스트 전용: NavMesh 결과 → C_EnemySync, 적 공격 → C_EnemyAttack 송신

### 6단계: 서버 구조 리팩터링 (Phase 1 + Phase 2) - 완료
멀티프로세스 GameServer 구조에서 단일 GameServer + 다중 룸 구조로 전환.
WebGL 배포 시 GameServer 의 고정 포트만 노출하면 되도록 사전 정비.

- [x] Phase 1: GameRoom / EnemyManager 싱글톤 해체
  - [x] GameRoom 인스턴스화 (`GameRoom.Instance` 제거)
  - [x] EnemyManager 를 GameRoom 의 `Enemies` 프로퍼티로 소유 (룸별 독립 상태)
  - [x] GameSession 생성자 주입 → 모든 핸들러 `gs.Room.X` 로 접근
- [x] Phase 2: 단일 GameServer 프로세스 + 다중 룸
  - [x] `internal.proto` (L2G_CreateRoom, G2L_RoomEnded) 정의
  - [x] `C_EnterGame` 에 `room_id` 필드 추가
  - [x] `GameRoomManager` (Dictionary<int, GameRoom>) 도입
  - [x] `GameServer/Program.cs` 재작성: args 제거 + Listener 2개 (7771 클라용, 7772 loopback 내부)
  - [x] `InternalSession` + `InternalPacketHandler` (game/lobby 양쪽)
  - [x] `ServerCore/Connector.cs` 신설 (Listener 의 능동 연결 버전)
  - [x] `LobbyServer/GameServerSession.cs` + `LobbyConfig.cs`
  - [x] `RoomManager.StartGame` 에서 ProcessManager.Spawn 제거 → 고정 포트 반환
  - [x] `LobbyPacketHandler.Handle_C_StartGame` 에서 L2G_CreateRoom 송신 후 S_GameReady
  - [x] `GameSession.Handle_C_EnterGame` 에서 roomId 로 GameRoomManager 조회 → BindRoom
  - [x] 클라 `NetworkManager.MyRoomId` 추가 + `C_EnterGame.RoomId` 동봉
  - [x] `ProcessManager.cs` 삭제
- [x] 빈 룸 자동 정리: `GameSession.OnDisconnected` 에서 `Room.Remove` 결과 0명 시 `GameRoomManager.RemoveRoom` 호출
- [x] G2L_RoomEnded 송신: 룸 정리 시 GameServer → LobbyServer 로 알림 (`InternalSession.LobbyConnection` 단일 참조 활용)
- [x] LobbyServer 측 G2L 처리: `RoomManager.RemoveRoom` 신설 + `InternalPacketHandler.Handle_G2L_RoomEnded` 에서 호출
- [x] `GameRoom.RoomId` 필드 추가 (룸이 자기 ID 를 알고 있어야 정리 시 GameRoomManager 호출 가능)
- [ ] LobbyServer → GameServer 연결 끊김 시 재연결 (현재는 한 번 실패 시 영구 미연결)

### 실행 방식 변경 (Phase 2 이후)
기존엔 LobbyServer 만 실행하면 GameServer 가 자동 spawn 됐지만, 이제는 두 프로세스를 **각각 수동으로 실행**해야 한다.

```bash
# 1. GameServer 먼저 (LobbyServer 부팅 시 GameServer:7772 에 연결 시도)
cd Server/GameServer && dotnet run

# 2. 다른 터미널에서 LobbyServer
cd Server/LobbyServer && dotnet run
```

순서가 바뀌면 LobbyServer 콘솔에 `[Connector] Connect 실패: ConnectionRefused` 출력 후,
첫 C_StartGame 처리 시 클라에 "게임서버 연결 없음" 에러 응답.

## 주요 구현 메모

### gen.bat 관련
- `.bat` 파일은 반드시 **ASCII 인코딩**으로 저장 (한글 주석 금지)
- `%~dp0`는 trailing `\` 포함 → `--proto_path="%PROTO_DIR%"` 에서 `\"` 로 끝나면 protoc(C++ 런타임)가 닫는 따옴표로 인식 못 함
- 해결: PROTO_DIR도 절대경로 하드코딩, trailing `\` 없이

### PacketSession 구조
- `Session.OnRecv()` → `PacketSession`이 sealed로 구현 (자식이 오버라이드 불가)
- 자식(LobbySession)은 `OnRecvPacket(ushort id, ArraySegment<byte> body)` 만 구현
- while 루프로 한 번의 Recv 콜백에 붙어온 여러 패킷 한번에 처리

### LobbyPacketHandler 구조
- `Handlers[]` 배열, 인덱스 = PacketId 값 (O(1) 조회)
- `MakePacket(PacketId, IMessage)` — [size 2byte][id 2byte][protobuf body] 조립
- 핸들러 내 TODO 주석 위치: Handle_C_CreateRoom, Handle_C_JoinRoom, Handle_C_LeaveRoom, Handle_C_GetRooms
