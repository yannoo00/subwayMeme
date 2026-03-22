  using System;

  namespace ServerCore
  {
      public class RecvBuffer
      {
          private ArraySegment<byte> _buffer;
          private int _readPos;
          private int _writePos;

          public RecvBuffer(int bufferSize)
          {
              _buffer = new ArraySegment<byte>(new byte[bufferSize]);
          }

          // 처리 가능한 데이터 크기 (Write - Read)
          public int DataSize => _writePos - _readPos;

          // 새 데이터를 받을 수 있는 남은 공간
          public int FreeSize => _buffer.Count - _writePos;

          // 네트워크에서 데이터를 받아 쓸 공간 (Write 포인터부터 끝까지)
          public ArraySegment<byte> WriteSegment
          {
              get => new ArraySegment<byte>(_buffer.Array, _buffer.Offset + _writePos, FreeSize);
          }

          // 처리할 데이터 구간 (Read 포인터부터 Write 포인터까지)
          public ArraySegment<byte> ReadSegment
          {
              get => new ArraySegment<byte>(_buffer.Array, _buffer.Offset + _readPos, DataSize);
          }

          // 데이터를 count만큼 읽었다고 표시 (Read 포인터 전진)
          public bool OnRead(int count)
          {
              if (count > DataSize) return false;
              _readPos += count;
              return true;
          }

          // 데이터를 count만큼 받았다고 표시 (Write 포인터 전진)
          public bool OnWrite(int count)
          {
              if (count > FreeSize) return false;
              _writePos += count;
              return true;
          }

          // 버퍼 정리: 읽은 데이터를 버리고 남은 데이터를 앞으로 당김
          // 빈 공간이 부족할 때 호출
          public void Clean()
          {
              int dataSize = DataSize;
              if (dataSize == 0)
              {
                  // 처리할 데이터가 없으면 포인터만 리셋
                  _readPos = _writePos = 0;
              }
              else
              {
                  // 아직 처리 안 한 데이터를 맨 앞으로 복사
                  Array.Copy(_buffer.Array, _buffer.Offset + _readPos,
                             _buffer.Array, _buffer.Offset, dataSize);
                  _readPos = 0;
                  _writePos = dataSize;
              }
          }
      }
  }