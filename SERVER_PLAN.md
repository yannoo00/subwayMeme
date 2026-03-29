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

| 항목 | 권한 |
|------|------|
| 플레이어 이동 | 클라 입력 → 서버 브로드캐스트 |
| 공격 이벤트 | 클라 입력 → 서버 브로드캐스트 |
| 데미지 판정 | 서버 권위 |
| 적 스폰 | 서버 권위 |
| 스테이지 타이머 | 서버 권위 |
| 무기 애니메이션 | 순수 클라 표현 (서버 무관) |

## 패킷 프로토콜

### 헤더 구조
```
[size: 2byte][packetId: 2byte][body: Protobuf bytes]
```

패킷 정의는 `Common/Protos/`의 .proto 파일로 작성.
`protoc`으로 서버(C#)와 클라(Unity C#) 코드를 동시에 자동 생성.

### 패킷 목록 (예정)

```protobuf
// 공통
C_Connected
S_Connected

// 로비 (Lobby Server)
C_CreateRoom      // 방 만들기
C_JoinRoom        // 방 참가
C_LeaveRoom       // 방 나가기
S_RoomList        // 방 목록 응답
S_RoomCreated     // 방 생성 완료
S_PlayerJoined    // 다른 플레이어 입장 알림
S_GameReady       // 게임 시작 준비 (접속할 Game Server 포트 전달)

// 게임 세션 (Game Server)
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

### 3단계: 로비 시스템 ← 다음 시작점
- [ ] LobbyServer/Lobby/LobbyPlayer.cs — 로비 내 플레이어 상태
- [ ] LobbyServer/Lobby/Room.cs — 대기방 (플레이어 목록, 상태)
- [ ] LobbyServer/Lobby/RoomManager.cs — 방 목록 관리 (thread-safe)
- [ ] LobbyPacketHandler의 TODO 채우기 (C_CreateRoom, C_JoinRoom, C_GetRooms 실제 로직)
- [ ] 인원 충족 시 GameServer 프로세스 spawn (ProcessManager.cs)
- [ ] 클라에 Game Server 포트 전달 (S_GameReady)

### 4단계: 게임 세션 동기화
- [ ] GameServer 프로젝트 구현 시작 (Program.cs — 인자로 포트/방ID 수신)
- [ ] 플레이어 위치 동기화
- [ ] 공격 이벤트 브로드캐스트
- [ ] 적 스폰 서버 권위로 이전
- [ ] 스테이지 타이머 서버 관리

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
