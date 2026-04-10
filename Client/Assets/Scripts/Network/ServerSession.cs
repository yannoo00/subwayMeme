using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;




public class ServerSession : MonoBehaviour 
{

    public static ServerSession Instance {get; private set;}

    private TcpClient _client;
    private NetworkStream _stream;
    private CancellationTokenSource _cts;

    private const int HEADER_SIZE = 4;

    private void Awake()
    {
        if( Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnDestroy() 
    {
        Disconnect();    
    }

    public async Task ConnectAsync(string host, int port)
    {
        try
        {
            _cts    = new CancellationTokenSource();
            _client = new TcpClient();

            await _client.ConnectAsync(host, port);
            _stream = _client.GetStream();

            Debug.Log($"[ServerSession] 접속 성공: {host}:{port}");

            // 수신 루프 시작 — async라 스레드풀에서 돌지만 await 필요 없음
            // 예외는 루프 내부에서 처리
            _ = ReceiveLoopAsync(_cts.Token);
        }
        catch (Exception e)
        {
            Debug.LogError($"[ServerSession] 접속 실패: {e.Message}");
        }
    }
    public void Disconnect()
    {
        _cts?.Cancel();
        _stream?.Close();
        _client?.Close();
    }

    // MakePacket()으로 만든 byte[]를 그대로 전달하면 됨
    // 메인 스레드에서 호출되므로 fire-and-forget으로 충분
    public void Send(byte[] data)
    {
        if (_stream == null || !_client.Connected) return;
        _ = _stream.WriteAsync(data, 0, data.Length);
    }

    // === Private 메서드 ===

    private async Task ReceiveLoopAsync(CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested)
            {
                // 1단계: 헤더 4바이트를 정확히 읽기
                byte[] header = await ReadExactAsync(HEADER_SIZE, ct);

                // 2단계: 헤더 파싱 (서버와 동일하게 Little-Endian)
                ushort totalSize = BitConverter.ToUInt16(header, 0);
                ushort packetId  = BitConverter.ToUInt16(header, 2);

                // 3단계: body 읽기 (전체 크기 - 헤더 크기)
                int    bodySize = totalSize - HEADER_SIZE;
                byte[] body     = bodySize > 0
                    ? await ReadExactAsync(bodySize, ct)
                    : Array.Empty<byte>();

                // 4단계: 메인 스레드에서 패킷 처리
                // PacketDispatcher는 다음 단계에서 구현
                MainThreadDispatcher.Instance.Enqueue(() =>
                    PacketDispatcher.Instance.Dispatch(packetId, body));
            }
        }
        catch (OperationCanceledException)
        {
            // Disconnect() 호출로 인한 정상 종료
        }
        catch (Exception e)
        {
            Debug.LogError($"[ServerSession] 수신 오류: {e.Message}");
            MainThreadDispatcher.Instance.Enqueue(OnDisconnected);
        }
    }

    // TCP는 요청한 바이트보다 적게 오는 경우가 있으므로
    // 정확히 count 바이트가 올 때까지 반복해서 읽는 헬퍼
    private async Task<byte[]> ReadExactAsync(int count, CancellationToken ct)
    {
        byte[] buffer   = new byte[count];
        int    received = 0;

        while (received < count)
        {
            int read = await _stream.ReadAsync(buffer, received, count - received, ct);

            // read == 0 이면 서버가 연결을 끊은 것
            if (read == 0) throw new Exception("서버 연결 종료");

            received += read;
        }

        return buffer;
    }

    private void OnDisconnected()
    {
        Debug.Log("[ServerSession] 서버 연결 끊김");
        // TODO: 재연결 시도 또는 로비씬으로 이동
    }
}


