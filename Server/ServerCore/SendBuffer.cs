using System;
using System.Threading;

namespace ServerCore
{
    public class SendBuffer
    {
        private byte[] _buffer;
        private int _usedSize = 0;

        public int FreeSize => _buffer.Length - _usedSize;

        public SendBuffer(int chunkSize)
        {
            _buffer = new byte[chunkSize];
        }

        // 패킷 작성 시작: reserveSize만큼 공간 예약, 쓸 수 있는 구간 반환
        public ArraySegment<byte> Open(int reserveSize)
        {
            return new ArraySegment<byte>(_buffer, _usedSize, reserveSize);
        }

        // 패킷 작성 완료: 실제 사용한 바이트 수 확정, 해당 구간 반환
        public ArraySegment<byte> Close(int usedSize)
        {
            ArraySegment<byte> segment = new ArraySegment<byte>(_buffer, _usedSize, usedSize);
            _usedSize += usedSize;
            return segment;
        }
    }

    public static class SendBufferHelper
    {
        // 스레드마다 독립적인 SendBuffer 유지 (lock 없이 안전)
        public static ThreadLocal<SendBuffer> CurrentBuffer = new ThreadLocal<SendBuffer>(() => null, true);

        public static int ChunkSize { get; set; } = 65535;

        public static ArraySegment<byte> Open(int reserveSize)
        {
            if (CurrentBuffer.Value == null)
                CurrentBuffer.Value = new SendBuffer(ChunkSize);

            // 현재 버퍼에 공간이 부족하면 새 청크 할당
            if (CurrentBuffer.Value.FreeSize < reserveSize)
                CurrentBuffer.Value = new SendBuffer(ChunkSize);

            return CurrentBuffer.Value.Open(reserveSize);
        }

        public static ArraySegment<byte> Close(int usedSize)
        {
            return CurrentBuffer.Value.Close(usedSize);
        }
    }
}
