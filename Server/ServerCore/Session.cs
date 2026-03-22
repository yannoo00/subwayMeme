using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace ServerCore
{
    public abstract class Session
    {
        private static int _nextId = 0;

        public int SessionId { get; private set; }

        private Socket _socket;
        private int _disconnected = 0;

        private RecvBuffer _recvBuffer = new RecvBuffer(65535);

        private SocketAsyncEventArgs _recvArgs = new SocketAsyncEventArgs();
        private SocketAsyncEventArgs _sendArgs = new SocketAsyncEventArgs();

        // 자식 클래스가 구현: 연결/해제/수신/송신 이벤트 처리
        public abstract void OnConnected(EndPoint endPoint);
        public abstract void OnDisconnected(EndPoint endPoint);
        public abstract int  OnRecv(ArraySegment<byte> buffer); // 처리한 바이트 수 반환
        public abstract void OnSend(int numOfBytes);

        public void Start(Socket socket)
        {
            SessionId = Interlocked.Increment(ref _nextId);
            _socket   = socket;

            _recvArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnRecvCompleted);
            _sendArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnSendCompleted);

            RegisterRecv();
        }

        public void Disconnect()
        {
            // Interlocked.Exchange로 중복 호출 방지
            if (Interlocked.Exchange(ref _disconnected, 1) == 1) return;

            OnDisconnected(_socket.RemoteEndPoint);
            _socket.Shutdown(SocketShutdown.Both);
            _socket.Close();
        }

        public void Send(ArraySegment<byte> sendBuff)
        {
            _sendArgs.SetBuffer(sendBuff.Array, sendBuff.Offset, sendBuff.Count);

            bool pending = _socket.SendAsync(_sendArgs);
            if (!pending)
                OnSendCompleted(null, _sendArgs);
        }


        // === Recv ===

        private void RegisterRecv()
        {
            // 빈 공간이 절반 이하면 버퍼 앞으로 정리
            if (_recvBuffer.FreeSize < _recvBuffer.DataSize)
                _recvBuffer.Clean();

            _recvArgs.SetBuffer(_recvBuffer.WriteSegment.Array,
                                _recvBuffer.WriteSegment.Offset,
                                _recvBuffer.WriteSegment.Count);

            bool pending = _socket.ReceiveAsync(_recvArgs);
            // 이미 데이터가 도착해 있으면 콜백이 호출되지 않으므로 직접 처리
            if (!pending)
                OnRecvCompleted(null, _recvArgs);
        }

        private void OnRecvCompleted(object sender, SocketAsyncEventArgs args)
        {
            if (args.BytesTransferred > 0 && args.SocketError == SocketError.Success)
            {
                if (!_recvBuffer.OnWrite(args.BytesTransferred))
                {
                    Disconnect();
                    return;
                }

                // 자식 클래스에서 패킷 파싱 후 처리한 바이트 수 반환
                int processLen = OnRecv(_recvBuffer.ReadSegment);
                if (processLen < 0 || processLen > _recvBuffer.DataSize)
                {
                    Disconnect();
                    return;
                }

                if (!_recvBuffer.OnRead(processLen))
                {
                    Disconnect();
                    return;
                }

                RegisterRecv();
            }
            else
            {
                Disconnect();
            }
        }


        // === Send ===

        private void OnSendCompleted(object sender, SocketAsyncEventArgs args)
        {
            if (args.BytesTransferred > 0 && args.SocketError == SocketError.Success)
            {
                OnSend(args.BytesTransferred);
            }
            else
            {
                Disconnect();
            }
        }
    }
}
