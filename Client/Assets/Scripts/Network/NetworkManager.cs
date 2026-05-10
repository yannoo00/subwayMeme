using System;
using System.Threading.Tasks;
using Google.Protobuf;
using LobbyProto;
using GameProto;
using UnityEngine;

// 서버 연결 관리 + 로컬 플레이어 상태 보관
// 씬 전환 후에도 유지되어야 하므로 DontDestroyOnLoad
public class NetworkManager : MonoBehaviour
{
    // === Inspector 변수 ===

    [Header("Server Settings")]
    [SerializeField] private string _serverHost = "127.0.0.1";
    [SerializeField] private int    _lobbyPort  = 7770;

    // === Private 변수 ===

    public static NetworkManager Instance { get; private set; }

    // 씬 간 유지되는 로컬 플레이어 상태
    public int    MyPlayerId     { get; set; }
    public string MyPlayerName   { get; set; }
    public bool   IsHost         { get; set; }
    public int    GameServerPort { get; set; }

    // 현재 연결 대상에 맞는 디스패처를 반환
    // ServerSession이 수신한 패킷을 올바른 핸들러로 전달하기 위해 사용
    public PacketDispatcher CurrentDispatcher { get; private set; }

    private const int HEADER_SIZE = 4;

    // === Unity 생명주기 ===

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        RegisterHandlers();
        CurrentDispatcher = PacketDispatcher.Lobby; // 시작 시 로비 연결 상태
    }

    // === Public 메서드 ===

    // 로비 서버 접속 후 C_Connected 자동 전송
    public async Task ConnectToLobbyAsync(string playerName)
    {
        MyPlayerName      = playerName;
        CurrentDispatcher = PacketDispatcher.Lobby;
        await ServerSession.Instance.ConnectAsync(_serverHost, _lobbyPort);
        SendLobby(PacketId.CConnected, new C_Connected { PlayerName = playerName });
    }

    // 게임 서버 접속 후 C_EnterGame 자동 전송
    // GameServer 프로세스 시작 직후에 호출되므로 준비될 때까지 재시도
    public async Task ConnectToGameAsync()
    {
        CurrentDispatcher = PacketDispatcher.Game;

        const int maxRetries  = 10;
        const int retryDelayMs = 500;

        bool connected = false;
        for (int i = 0; i < maxRetries; i++)
        {
            connected = await ServerSession.Instance.ConnectAsync(_serverHost, GameServerPort);
            if (connected) break;

            Debug.Log($"[NetworkManager] 게임 서버 재시도 {i + 1}/{maxRetries}...");
            await Task.Delay(retryDelayMs);
        }

        if (!connected)
        {
            Debug.LogError("[NetworkManager] 게임 서버 접속 실패 (최대 재시도 횟수 초과)");
            return;
        }

        SendGame(GamePacketId.CEnterGame, new C_EnterGame
        {
            PlayerId   = MyPlayerId,
            PlayerName = MyPlayerName,
        });
    }

    // 로비 패킷 송신
    public void SendLobby(PacketId id, IMessage message)
        => ServerSession.Instance.Send(MakePacket((ushort)id, message));

    // 게임 패킷 송신
    public void SendGame(GamePacketId id, IMessage message)
        => ServerSession.Instance.Send(MakePacket((ushort)id, message));

    // === Private 메서드 ===

    // 로비/게임 핸들러를 각각 별도 디스패처에 등록
    // 두 프로토콜이 같은 패킷 ID를 사용해도 서로 간섭하지 않음
    private void RegisterHandlers()
    {
        var lobby = PacketDispatcher.Lobby;
        lobby.Register((ushort)PacketId.SConnected,      LobbyPacketHandler.Handle_S_Connected);
        lobby.Register((ushort)PacketId.SRoomCreated,    LobbyPacketHandler.Handle_S_RoomCreated);
        lobby.Register((ushort)PacketId.SRoomJoined,     LobbyPacketHandler.Handle_S_RoomJoined);
        lobby.Register((ushort)PacketId.SRoomList,       LobbyPacketHandler.Handle_S_RoomList);
        lobby.Register((ushort)PacketId.SPlayerJoined,   LobbyPacketHandler.Handle_S_PlayerJoined);
        lobby.Register((ushort)PacketId.SPlayerLeft,     LobbyPacketHandler.Handle_S_PlayerLeft);
        lobby.Register((ushort)PacketId.SCreatorChanged, LobbyPacketHandler.Handle_S_CreatorChanged);
        lobby.Register((ushort)PacketId.SGameReady,      LobbyPacketHandler.Handle_S_GameReady);
        lobby.Register((ushort)PacketId.SError,          LobbyPacketHandler.Handle_S_Error);
        lobby.Register((ushort)PacketId.SPlayerCharacterSelected, LobbyPacketHandler.Handle_S_PlayerCharacterSelected);

        var game = PacketDispatcher.Game;
        game.Register((ushort)GamePacketId.SEnterGame,     ClientGamePacketHandler.Handle_S_EnterGame);
        game.Register((ushort)GamePacketId.SPlayerEntered, ClientGamePacketHandler.Handle_S_PlayerEntered);
        game.Register((ushort)GamePacketId.SPlayerLeft,    ClientGamePacketHandler.Handle_S_PlayerLeft);
        game.Register((ushort)GamePacketId.SHostChanged,   ClientGamePacketHandler.Handle_S_HostChanged);
        game.Register((ushort)GamePacketId.SGameStart,     ClientGamePacketHandler.Handle_S_GameStart);
        game.Register((ushort)GamePacketId.SMove,          ClientGamePacketHandler.Handle_S_Move);
        game.Register((ushort)GamePacketId.SAttack,        ClientGamePacketHandler.Handle_S_Attack);
        game.Register((ushort)GamePacketId.SEnemySpawn,    ClientGamePacketHandler.Handle_S_EnemySpawn);
        game.Register((ushort)GamePacketId.SEnemySync,     ClientGamePacketHandler.Handle_S_EnemySync);
        game.Register((ushort)GamePacketId.SEnemyDamaged,  ClientGamePacketHandler.Handle_S_EnemyDamaged);
        game.Register((ushort)GamePacketId.SEnemyDied,     ClientGamePacketHandler.Handle_S_EnemyDied);
        game.Register((ushort)GamePacketId.SPlayerDamaged, ClientGamePacketHandler.Handle_S_PlayerDamaged);
        game.Register((ushort)GamePacketId.SPlayerDied,    ClientGamePacketHandler.Handle_S_PlayerDied);
        game.Register((ushort)GamePacketId.SWaveStart,     ClientGamePacketHandler.Handle_S_WaveStart);
        game.Register((ushort)GamePacketId.STimerSync,     ClientGamePacketHandler.Handle_S_TimerSync);
        game.Register((ushort)GamePacketId.SPlayerExited,  ClientGamePacketHandler.Handle_S_PlayerExited);
        game.Register((ushort)GamePacketId.SAllExited,     ClientGamePacketHandler.Handle_S_AllExited);
        game.Register((ushort)GamePacketId.SPlayerBoarded, ClientGamePacketHandler.Handle_S_PlayerBoarded);
        game.Register((ushort)GamePacketId.SAllBoarded,    ClientGamePacketHandler.Handle_S_AllBoarded);
        game.Register((ushort)GamePacketId.SGameClear,     ClientGamePacketHandler.Handle_S_GameClear);
        game.Register((ushort)GamePacketId.SGameOver,      ClientGamePacketHandler.Handle_S_GameOver);
        game.Register((ushort)GamePacketId.SInteractResult,ClientGamePacketHandler.Handle_S_InteractResult);
        game.Register((ushort)GamePacketId.SMutationResult,ClientGamePacketHandler.Handle_S_MutationResult);
    }

    // 서버의 MakePacket과 동일한 헤더 구조: [size 2byte][packetId 2byte][body]
    private static byte[] MakePacket(ushort id, IMessage message)
    {
        byte[] body      = message.ToByteArray();
        ushort totalSize = (ushort)(HEADER_SIZE + body.Length);
        byte[] packet    = new byte[totalSize];

        Array.Copy(BitConverter.GetBytes(totalSize), 0, packet, 0, 2);
        Array.Copy(BitConverter.GetBytes(id),        0, packet, 2, 2);
        Array.Copy(body,                             0, packet, HEADER_SIZE, body.Length);

        return packet;
    }
}
