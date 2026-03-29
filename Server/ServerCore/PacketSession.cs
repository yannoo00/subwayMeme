using System;
using System.Net;

namespace ServerCore
{
    
    //  패킷 헤더 구조
    //  [size: 2byte] [packetId: 2byte] [body: N byte]
    //   헤더 포함 전체 크기, protobuf 직렬화 바이트, body 
    
    public abstract class PacketSession : Session
    {
        public static readonly int HEADER_SIZE = 4; // size(2) + packetId(2)

        // Session.OnRecv()를 구현 — 버퍼에서 완성된 패킷을 꺼내 OnRecvPacket 호출
        // 반환값: 이번에 처리 완료한 총 바이트 수 (RecvBuffer에 알려줌)
        public sealed override int OnRecv(ArraySegment<byte> buffer)
        {
            int processLen = 0;

            while (true)
            {
                // 헤더조차 안 왔으면 대기
                if (buffer.Count < HEADER_SIZE)
                    break;

                // 헤더에서 전체 패킷 크기 읽기 (Little-Endian)
                ushort dataSize = BitConverter.ToUInt16(buffer.Array, buffer.Offset);

                // 패킷 전체가 아직 안 왔으면 대기
                if (buffer.Count < dataSize)
                    break;

                // 패킷 ID 읽기
                ushort packetId = BitConverter.ToUInt16(buffer.Array, buffer.Offset + 2);

                // body 구간만 잘라서 전달 (헤더 4바이트 제외)
                OnRecvPacket(
                    packetId,
                    new ArraySegment<byte>(buffer.Array, buffer.Offset + HEADER_SIZE, dataSize - HEADER_SIZE)
                );

                // 처리한 만큼 커서 이동
                processLen += dataSize;
                buffer = new ArraySegment<byte>(buffer.Array, buffer.Offset + dataSize, buffer.Count - dataSize);
            }

            return processLen;
        }

        // 자식 클래스(LobbySession 등)가 구현:
        // 완성된 패킷 1개의 id와 body bytes를 받아 protobuf 역직렬화 후 처리
        public abstract void OnRecvPacket(ushort id, ArraySegment<byte> body);
    }
}
