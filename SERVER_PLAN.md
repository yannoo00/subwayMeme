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
| 서버 간 통신 | localhost HTTP (REST) | Lobby ↔ Game Server 내부 통신 |

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
    ├── LobbyServer/          ← 항상 실행, 방 관리 담당
    │   └── LobbyServer.csproj
    └── GameServer/           ← 게임방 1개당 1 프로세스, 게임 종료 시 자동 종료
        └── GameServer.csproj
```

## 아키텍처

```
[Mac Mini]
│
├── Lobby Server (항상 실행, 포트 7770)
│   ├── 플레이어 접속 및 인증
│   ├── 방 목록 관리
│   └── 게임 시작 시 Game Server 프로세스 spawn
│
├── Game Server #1 (포트 7771) ← 방 1개
├── Game Server #2 (포트 7772) ← 방 1개
└── Game Server #N ...          ← 게임 종료 시 자동 종료

[클라이언트 접속 흐름]
1. Lobby Server 접속 (TCP)
2. 방 생성 / 참가
3. 게임 시작 → Lobby가 Game Server spawn
4. Lobby 응답: "7771 포트로 접속해"
5. Game Server에 직접 접속 → 게임 진행
```

**Game Server는 요청 시 spawn**: 방이 만들어질 때 `Process.Start()`로 실행,
게임 종료 시 스스로 종료. 항상 대기 중인 프로세스 없음.

Lobby ↔ Game Server 내부 통신은 localhost HTTP (REST)로 충분.

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

| 항목 | 권한 | 비고 |
|------|------|------|
| 플레이어 이동 | 클라 입력 → 서버 릴레이 | C_Move → S_Move |
| 공격 판정 | 클라 보고 + 서버 검증 | C_Attack에 히트 대상 포함, 서버가 쿨타임/거리 검증 |
| 적 AI | 호스트 클라 NavMesh 실행 → 서버 릴레이 | C_EnemySync → S_EnemySync |
| 적 HP/사망 | 서버 권위 | S_EnemyDamaged, S_EnemyDied |
| 적 스폰 타이밍 | 서버 권위 | S_EnemySpawn |
| 적 공격 → 플레이어 피격 | 호스트 보고 → 서버 판정 | C_EnemyAttack → S_PlayerDamaged |
| 스테이지 타이머 | 서버 권위 | S_TimerSync |
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
C_EnterGame / S_EnterGame          // 게임서버 접속 (로비 playerId 사용)
S_PlayerEntered / S_PlayerLeft     // 입장/퇴장 알림
S_HostChanged                      // 호스트 변경

// 게임 시작/종료
C_Ready / S_GameStart              // 씬 로딩 완료 → 게임 시작 (seed 포함)
S_GameClear / S_GameOver           // 클리어 / 게임오버

// 이동
C_Move / S_Move                    // 플레이어 위치 동기화

// 전투 (클라 보고 + 서버 검증)
C_Attack / S_Attack                // 공격 입력 / 애니메이션 브로드캐스트
S_EnemyDamaged / S_EnemyDied       // 적 피격/사망 (서버 권위)
S_PlayerDamaged / S_PlayerDied     // 플레이어 피격/사망

// 적 (호스트 AI + 서버 권위)
S_EnemySpawn                       // 서버가 스폰 명령
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

## 서버 프로젝트 구조

```
Server/
├── ServerCore/                   ← Lobby/Game Server 공용 SAEA 라이브러리
│   ├── ServerCore.csproj
│   ├── Listener.cs               ← AcceptAsync 처리
│   ├── Session.cs                ← 클라이언트 연결 단위 (Send/Recv)
│   ├── RecvBuffer.cs             ← 수신 링버퍼 (패킷 단편화 처리)
│   └── SendBuffer.cs             ← 송신 버퍼
│
├── LobbyServer/
│   ├── LobbyServer.csproj
│   ├── Program.cs
│   ├── Lobby/
│   │   ├── Room.cs               ← 대기방
│   │   ├── RoomManager.cs        ← 방 목록 관리
│   │   └── LobbyPlayer.cs        ← 로비 내 플레이어
│   ├── ProcessManager.cs         ← Game Server 프로세스 spawn/kill
│   ├── Packet/
│   │   ├── Generated/            ← protoc 자동 생성
│   │   └── LobbyPacketHandler.cs
│   └── DB/
│       └── DBManager.cs
│
└── GameServer/
    ├── GameServer.csproj
    ├── Program.cs                ← 인자로 포트/방ID 수신
    ├── Game/
    │   ├── GameRoom.cs           ← 게임 룸 (플레이어, 적, 타이머)
    │   ├── GamePlayer.cs         ← 서버 측 플레이어 상태
    │   ├── EnemyController.cs    ← 서버 권위 적 관리
    │   └── StageTimer.cs         ← 스테이지 타이머
    ├── Packet/
    │   ├── Generated/            ← protoc 자동 생성
    │   └── GamePacketHandler.cs
    └── LobbyReporter.cs          ← 게임 종료 시 Lobby에 HTTP 보고
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
- [ ] EnemyManager.cs — 적 HP 서버 권위 관리, 스폰 ID 발급
- [ ] StageTimer.cs — S_WaveStart / S_TimerSync 서버 주도 발송
- [ ] C_Attack 검증 로직 (쿨타임/거리)
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
  - [x] S_HostChanged → NetworkManager.IsHost 갱신, TODO: NavMesh 권한 전환
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
