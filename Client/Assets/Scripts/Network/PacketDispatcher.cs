using System;
using System.Collections.Generic;
using UnityEngine;

// MonoBehaviour 불필요 - Unity 생명주기 없이 딕셔너리 조회만 하므로 순수 C# 싱글톤
public class PacketDispatcher
{
    public static readonly PacketDispatcher Instance = new();

    private readonly Dictionary<ushort, Action<byte[]>> _handlers = new();

    private PacketDispatcher() { }

    // NetworkManager.Awake()에서 등록
    public void Register(ushort packetId, Action<byte[]> handler)
    {
        _handlers[packetId] = handler;
    }

    // ServerSession 수신 스레드 → MainThreadDispatcher → 여기서 호출 (메인 스레드)
    public void Dispatch(ushort packetId, byte[] body)
    {
        if (_handlers.TryGetValue(packetId, out var handler))
            handler?.Invoke(body);
        else
            Debug.LogWarning($"[PacketDispatcher] 핸들러 없음: id={packetId}");
    }
}
