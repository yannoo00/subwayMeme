using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using NativeWebSocket;
using UnityEngine;

// WebSocket 기반 서버 세션. WebGL 빌드 + 데스크탑 빌드 양쪽 지원.
// 브라우저는 raw TCP 못 쓰므로, 데스크탑에서도 동일한 WebSocket 경로를 써서 코드 단일화.
// 서버 앞단에 websockify 가 WebSocket <-> TCP 변환을 담당 (서버 코드 무변경).
public class ServerSession : MonoBehaviour
{
    public static ServerSession Instance { get; private set; }

    private WebSocket _ws;

    // WS 메시지가 우리 패킷 경계와 일치하지 않을 수 있어 누적 후 [size][id][body] 단위로 잘라냄.
    // 서버의 PacketSession.OnRecv 와 같은 역할.
    private readonly List<byte> _recvBuffer = new();

    // 워커 스레드에서 받은 패킷을 메인 스레드(Update)에 전달
    private ConcurrentQueue<(ushort packetId, byte[] body)> _receiveQueue = new();

    private const int HEADER_SIZE = 4;

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
        }
    }

    private void Update()
    {
        // NativeWebSocket 은 WebGL 외 플랫폼에서 백그라운드 스레드로 메시지를 받아 큐에 쌓아둔다.
        // 매 프레임 DispatchMessageQueue 를 호출해야 OnMessage 콜백이 메인 스레드에서 발화됨.
        // WebGL 은 브라우저 WebSocket API 가 자체적으로 메인 스레드에 발화하므로 호출 불필요.
#if !UNITY_WEBGL || UNITY_EDITOR
        _ws?.DispatchMessageQueue();
#endif

        // 큐에 쌓인 패킷을 메인 스레드에서 디스패처에 넘김
        while (_receiveQueue.TryDequeue(out var packet))
        {
            NetworkManager.Instance.CurrentDispatcher.Dispatch(packet.packetId, packet.body);
        }
    }

    private void OnDestroy()
    {
        Disconnect();
    }

    // 호출부(NetworkManager) 시그니처 그대로 유지. 내부에서 ws:// URL 조립.
    // 로비->게임 전환 시 재접속되는 흐름 지원 위해 호출 시점에 기존 연결 먼저 정리.
    public async Task<bool> ConnectAsync(string host, int port)
    {
        try
        {
            Disconnect();
            _recvBuffer.Clear();

            string url = $"ws://{host}:{port}";
            _ws = new WebSocket(url);

            _ws.OnOpen    += () => Debug.Log($"[ServerSession] 접속 성공: {url}");
            _ws.OnMessage += OnWebSocketMessage;
            _ws.OnError   += (err) => Debug.LogError($"[ServerSession] 에러: {err}");
            _ws.OnClose   += (code) => OnDisconnected();

            // Connect 는 핸드셰이크 완료까지 await. 완료 후 State 로 성공 여부 판정.
            await _ws.Connect();
            return _ws.State == WebSocketState.Open;
        }
        catch (Exception e)
        {
            Debug.LogError($"[ServerSession] 접속 실패: {e.Message}");
            return false;
        }
    }

    public void Disconnect()
    {
        if (_ws != null && _ws.State == WebSocketState.Open)
            _ = _ws.Close();
        _ws = null;
    }

    // NetworkManager.MakePacket() 결과 [size 2byte][id 2byte][body] 바이트 배열을 그대로 전송.
    // WebSocket 바이너리 프레임에 실려 websockify 를 통해 서버에 raw TCP 바이트로 전달됨.
    public void Send(byte[] data)
    {
        if (_ws == null || _ws.State != WebSocketState.Open) return;
        _ = _ws.Send(data);
    }

    // === Private 메서드 ===

    // WebSocket 메시지 수신 콜백.
    // WS 메시지 1개에 우리 패킷 0개 / 1개 / 여러 개가 담길 수 있으므로 누적 후 잘라낸다.
    private void OnWebSocketMessage(byte[] data)
    {
        _recvBuffer.AddRange(data);

        while (_recvBuffer.Count >= HEADER_SIZE)
        {
            // 헤더 파싱 (서버와 동일 Little-Endian)
            ushort totalSize = (ushort)(_recvBuffer[0] | (_recvBuffer[1] << 8));

            // 패킷 전체가 아직 안 왔으면 다음 메시지 대기
            if (_recvBuffer.Count < totalSize) break;

            ushort packetId = (ushort)(_recvBuffer[2] | (_recvBuffer[3] << 8));

            int bodySize = totalSize - HEADER_SIZE;
            byte[] body  = bodySize > 0
                ? _recvBuffer.GetRange(HEADER_SIZE, bodySize).ToArray()
                : Array.Empty<byte>();

            _receiveQueue.Enqueue((packetId, body));

            // List<byte>.RemoveRange 는 O(n). 트래픽 늘면 ring buffer 로 교체 고려.
            _recvBuffer.RemoveRange(0, totalSize);
        }
    }

    private void OnDisconnected()
    {
        Debug.Log("[ServerSession] 서버 연결 끊김");
        // TODO: 재연결 시도 또는 로비씬으로 이동
    }
}
