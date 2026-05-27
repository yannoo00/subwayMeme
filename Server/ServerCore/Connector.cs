using System;
using System.Net;
using System.Net.Sockets;

namespace ServerCore
{
    // Listener 의 반대 역할: 다른 서버에 능동적으로 TCP 연결.
    // 연결 성공 시 sessionFactory 로 만든 Session 에 소켓을 묶고 OnConnected 호출.
    // 한 번 호출에 한 번의 연결만 수행한다 (Listener 처럼 반복 Accept 하지 않음).
    public class Connector
    {
        Func<Session> _sessionFactory;

        public void Connect(IPEndPoint endPoint, Func<Session> sessionFactory)
        {
            _sessionFactory = sessionFactory;

            Socket socket = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            SocketAsyncEventArgs args = new SocketAsyncEventArgs();
            args.Completed     += new EventHandler<SocketAsyncEventArgs>(OnConnectCompleted);
            args.RemoteEndPoint = endPoint;
            args.UserToken      = socket;

            bool pending = socket.ConnectAsync(args);
            if (!pending)
                OnConnectCompleted(null, args);
        }

        void OnConnectCompleted(object sender, SocketAsyncEventArgs args)
        {
            if (args.SocketError == SocketError.Success)
            {
                Session session = _sessionFactory.Invoke();
                session.Start((Socket)args.UserToken);
                session.OnConnected(args.RemoteEndPoint);
            }
            else
            {
                Console.WriteLine($"[Connector] Connect 실패: {args.SocketError}");
            }
        }
    }
}
