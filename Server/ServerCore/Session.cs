using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace ServerCore
{
    public abstract class Session
    {
        // 전체 세션 ID counter 
        private static int _nextId = 0;

        public int SessionId { get; private set; }

        private Socket _socket; // TCP 소켓
        private int _disconnected = 0; //중복 연결 해제 방지용 플래그

        private RecvBuffer _recvBuffer = new RecvBuffer(65535);

        private SocketAsyncEventArgs _recvArgs = new SocketAsyncEventArgs();
        private SocketAsyncEventArgs _sendArgs = new SocketAsyncEventArgs();

        private object _lock = new object();
        private Queue<ArraySegment<byte>> _sendQueue = new Queue<ArraySegment<byte>>();
        private List<ArraySegment<byte>> _pendingList = new List<ArraySegment<byte>>();

        // 자식 클래스가 구현: 연결/해제/수신/송신 이벤트 처리
        public abstract void OnConnected(EndPoint endPoint);
        public abstract void OnDisconnected(EndPoint endPoint);
        public abstract int  OnRecv(ArraySegment<byte> buffer); // 처리한 바이트 수 반환
        public abstract void OnSend(int numOfBytes);

        public void Start(Socket socket)
        {
            SessionId = Interlocked.Increment(ref _nextId);
            _socket   = socket;

            //EventHandler<TEventArgs>(object sender, TEventArgs e);        
            _recvArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnRecvCompleted);
            _sendArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnSendCompleted);

            RegisterRecv();
        }

        public void Disconnect()
        {
            // Interlocked.Exchange로 중복 호출 방지
            // 이 때 Exchange는 이전 값을 반환하므로 _disconnected가 이미 1이면 종료(이미 연결 해제됨)
            if (Interlocked.Exchange(ref _disconnected, 1) == 1) return;

            OnDisconnected(_socket.RemoteEndPoint);
            _socket.Shutdown(SocketShutdown.Both);
            _socket.Close();
        }

        public void Send(ArraySegment<byte> sendBuff)
        {
            lock (_lock)
            {
                _sendQueue.Enqueue(sendBuff);
                // pendingList가 비어있다 = 현재 전송 중인 것 없음, 바로 전송 시작
                if (_pendingList.Count == 0)
                    RegisterSend();
            }
        }

        private void RegisterSend()
        {
            // 큐에 쌓인 패킷을 전부 pendingList로 옮기고 한 번에 전송
            // SAEA BufferList를 쓰면 여러 segment를 한 번의 SendAsync로 전송 가능
            while (_sendQueue.Count > 0)
                _pendingList.Add(_sendQueue.Dequeue());

            _sendArgs.BufferList = _pendingList;

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
            lock (_lock)
            {
                if (args.BytesTransferred > 0 && args.SocketError == SocketError.Success)
                {
                    // 전송 완료 → pendingList 비움
                    _sendArgs.BufferList = null;
                    _pendingList.Clear();

                    OnSend(args.BytesTransferred);

                    // 완료되는 사이 새 패킷이 들어왔으면 이어서 전송
                    if (_sendQueue.Count > 0)
                        RegisterSend();
                }
                else
                {
                    Disconnect();
                }
            }
        }
    }
}
