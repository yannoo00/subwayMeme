using System;
using System.Net;
using System.Net.Sockets;

namespace YannooNet
{
    public class YListener
    {
        private Socket _listenSocket;

        // 새 연결이 수락될 때 어떤 Session을 만들지 결정하는 팩토리 함수
        private Func<YSession> _sessionFactory;

        public void Init(IPEndPoint endPoint, Func<YSession> sessionFactory)
        {
            _sessionFactory = sessionFactory;

            _listenSocket = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            _listenSocket.Bind(endPoint);

            // backlog: 연결 대기열 최대 크기
            _listenSocket.Listen(backlog: 10);

            // 첫 번째 Accept 등록
            RegisterAccept();
        }

        private void RegisterAccept()
        {
            SocketAsyncEventArgs acceptArgs = new SocketAsyncEventArgs();
            acceptArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnAcceptCompleted);

            bool pending = _listenSocket.AcceptAsync(acceptArgs);
            if (!pending)
                OnAcceptCompleted(null, acceptArgs);
        }

        private void OnAcceptCompleted(object sender, SocketAsyncEventArgs args)
        {
            if (args.SocketError == SocketError.Success)
            {
                // 팩토리로 Session 생성 후 시작
                // Start() 내부에서 RecvAsync가 즉시 완료되며 소켓이 닫힐 수 있으므로 RemoteEndPoint를 Start() 호출 전에 미리 저장
                System.Net.EndPoint remoteEndPoint = args.AcceptSocket.RemoteEndPoint;
                YSession session = _sessionFactory.Invoke();
                session.Start(args.AcceptSocket);
                session.OnConnected(remoteEndPoint);
            }
            else
            {
                Console.WriteLine($"[YListener] Accept 실패: {args.SocketError}");
            }

            // 다음 연결을 받기 위해 다시 등록
            // AcceptSocket을 null로 초기화해야 재사용 가능
            args.AcceptSocket = null;
            bool pending = _listenSocket.AcceptAsync(args);
            if (!pending)
                OnAcceptCompleted(null, args);
        }
    }
}
