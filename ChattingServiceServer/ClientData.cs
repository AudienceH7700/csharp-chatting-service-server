using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ChattingServiceServer
{
    class ClientData
    {
        // 로컬 테스트 Y/N 속성
        public static bool isDebug = true;
        public TcpClient tcpClient { get; set; }
        public Byte[] readBuffer { get; set; }
        public StringBuilder currentMsg { get; set; }
        public string clientName { get; set; }
        public int clientNumber { get; set; }

        // 수신된 TcpClient를 통해 생성자 실행하며, 수신된 IP주소에 따라 채번 ex) 127.0.0.5이라면 끝의 자리 한 자리를 저장 -> 5번으로 채번
        public ClientData(TcpClient client)
        {
            currentMsg = new StringBuilder();
            readBuffer = new byte[1024];

            this.tcpClient = client;

            char[] splitDivision = new char[2];
            splitDivision[0] = '.';
            splitDivision[1] = ':';

            string[] temp = null;
            // 로컬에서 실행 및 테스트 시에는 LocalEndPoint로 IP주소 판별, 그 외에는 RemoteEndPoitn함수로 판별
            if(isDebug)
            {
                temp = tcpClient.Client.LocalEndPoint.ToString().Split(splitDivision);
            }
            else
            {
                temp = tcpClient.Client.RemoteEndPoint.ToString().Split(splitDivision);
            }

            this.clientNumber = int.Parse(temp[3]);
        }
    }
}
