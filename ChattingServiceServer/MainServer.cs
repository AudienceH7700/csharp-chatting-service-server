using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ChattingServiceServer
{
    // 서버 실행 클래스
    public class MainServer
    {
        ClientManager _clientManager = new ClientManager();

        public MainServer() 
        {
            Task serverStart = Task.Run(() =>
            {
                ServerRun();
            });  
        }

        private void ServerRun()
        {
            // TcpListener 클래스를 이용한 TCP/IP 및 포트 연결 허용 실행
            TcpListener listener = new TcpListener(new IPEndPoint(IPAddress.Any, 708));
            listener.Start();

            while (true)
            {
                // TCP 연결을 비동기 Task로 실행
                Task<TcpClient> acceptTask = listener.AcceptTcpClientAsync();
                // TCP 연결 시도가 있기까지 대기
                acceptTask.Wait();
                // 수신될 경우, 해당 TcpClient를 클라이언트 관리 객체에 등록
                _clientManager.AddClient(acceptTask.Result);
            }
        }
    }
}
