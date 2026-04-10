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
    }

    // === Public 메서드 ===

    // 로비 서버 접속 후 C_Connected 자동 전송
    public async Task ConnectToLobbyAsync(string playerName)
    {
        MyPlayerName = playerName;
        await ServerSession.Instance.ConnectAsync(_serverHost, _lobbyPort);
        SendLobby(PacketId.CConnected, new C_Connected { PlayerName = playerName });
    }

    // 게임 서버 접속 후 C_EnterGame 자동 전송
    // S_GameReady 수신 후 씬 로드가 완료된 시점에 호출
    public async Task ConnectToGameAsync()
    {
        await ServerSession.Instance.ConnectAsync(_serverHost, GameServerPort);
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

    // 수신 패킷 ID → 핸들러 함수 등록
    // Dispatch는 항상 메인 스레드에서 호출되므로 스레드 안전
    private void RegisterHandlers()
    {
        var d = PacketDispatcher.Instance;

        // 로비 패킷
        d.Register((ushort)PacketId.SConnected,      LobbyPacketHandler.Handle_S_Connected);
        d.Register((ushort)PacketId.SRoomCreated,    LobbyPacketHandler.Handle_S_RoomCreated);
        d.Register((ushort)PacketId.SRoomList,       LobbyPacketHandler.Handle_S_RoomList);
        d.Register((ushort)PacketId.SPlayerJoined,   LobbyPacketHandler.Handle_S_PlayerJoined);
        d.Register((ushort)PacketId.SPlayerLeft,     LobbyPacketHandler.Handle_S_PlayerLeft);
        d.Register((ushort)PacketId.SCreatorChanged, LobbyPacketHandler.Handle_S_CreatorChanged);
        d.Register((ushort)PacketId.SGameReady,      LobbyPacketHandler.Handle_S_GameReady);
        d.Register((ushort)PacketId.SError,          LobbyPacketHandler.Handle_S_Error);

        // 게임 패킷
        d.Register((ushort)GamePacketId.SEnterGame,     ClientGamePacketHandler.Handle_S_EnterGame);
        d.Register((ushort)GamePacketId.SPlayerEntered, ClientGamePacketHandler.Handle_S_PlayerEntered);
        d.Register((ushort)GamePacketId.SPlayerLeft,    ClientGamePacketHandler.Handle_S_PlayerLeft);
        d.Register((ushort)GamePacketId.SHostChanged,   ClientGamePacketHandler.Handle_S_HostChanged);
        d.Register((ushort)GamePacketId.SGameStart,     ClientGamePacketHandler.Handle_S_GameStart);
        d.Register((ushort)GamePacketId.SMove,          ClientGamePacketHandler.Handle_S_Move);
        d.Register((ushort)GamePacketId.SAttack,        ClientGamePacketHandler.Handle_S_Attack);
        d.Register((ushort)GamePacketId.SEnemySpawn,    ClientGamePacketHandler.Handle_S_EnemySpawn);
        d.Register((ushort)GamePacketId.SEnemySync,     ClientGamePacketHandler.Handle_S_EnemySync);
        d.Register((ushort)GamePacketId.SEnemyDamaged,  ClientGamePacketHandler.Handle_S_EnemyDamaged);
        d.Register((ushort)GamePacketId.SEnemyDied,     ClientGamePacketHandler.Handle_S_EnemyDied);
        d.Register((ushort)GamePacketId.SPlayerDamaged, ClientGamePacketHandler.Handle_S_PlayerDamaged);
        d.Register((ushort)GamePacketId.SPlayerDied,    ClientGamePacketHandler.Handle_S_PlayerDied);
        d.Register((ushort)GamePacketId.SWaveStart,     ClientGamePacketHandler.Handle_S_WaveStart);
        d.Register((ushort)GamePacketId.STimerSync,     ClientGamePacketHandler.Handle_S_TimerSync);
        d.Register((ushort)GamePacketId.SPlayerExited,  ClientGamePacketHandler.Handle_S_PlayerExited);
        d.Register((ushort)GamePacketId.SAllExited,     ClientGamePacketHandler.Handle_S_AllExited);
        d.Register((ushort)GamePacketId.SPlayerBoarded, ClientGamePacketHandler.Handle_S_PlayerBoarded);
        d.Register((ushort)GamePacketId.SAllBoarded,    ClientGamePacketHandler.Handle_S_AllBoarded);
        d.Register((ushort)GamePacketId.SGameClear,     ClientGamePacketHandler.Handle_S_GameClear);
        d.Register((ushort)GamePacketId.SGameOver,      ClientGamePacketHandler.Handle_S_GameOver);
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
