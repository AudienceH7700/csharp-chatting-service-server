using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ChattingServiceServer
{
    // 클라이언트 관리 클래스로 해당 클래스를 통해 연결된 클라이언트(TcpClient)에 지속적인 메세지 수신 로직(BeginRead 내 AsyncCallback - 재귀적으로 동작)을 작성
    class ClientManager
    {
        // 클라이언트 관리 객체에서 구성하는 클라이언트 Dictionary(채번, 클라이언트데이터로 구성)
        public static ConcurrentDictionary<int, ClientData> clientDic = new ConcurrentDictionary<int, ClientData>();
        // 수신된 메세지를 바로 파싱시키는 이벤트 할당 맵
        public static event Action<string, string> messageParsingAction = null;
        // 수신된 메세지를 로그로 저장/관리하기 위한 이벤트 할당 맵
        public static event Action<string, int> ChangeListViewAction = null;

        // 클라이언트 추가 함수
        public void AddClient(TcpClient newClient)
        {
            ClientData currentClient = new ClientData(newClient);

            try
            {
                newClient.GetStream().BeginRead(currentClient.readBuffer, 0, currentClient.readBuffer.Length, new AsyncCallback(DataReceived), currentClient);
                clientDic.TryAdd(currentClient.clientNumber, currentClient);

            }
            catch (Exception e)
            {
            }
        }
        private void DataReceived(IAsyncResult ar)
        {
            ClientData client = ar.AsyncState as ClientData;
            try
            {
                int byteLength = client.tcpClient.GetStream().EndRead(ar);
                string strData = Encoding.Default.GetString(client.readBuffer, 0, byteLength);
                client.tcpClient.GetStream().BeginRead(client.readBuffer, 0, client.readBuffer.Length, new AsyncCallback(DataReceived), client);

                if (string.IsNullOrEmpty(client.clientName))
                {
                    if (ChangeListViewAction != null)
                    {
                        if (CheckID(strData))
                        {
                            string userName = strData.Substring(3);
                            client.clientName = userName;
                            ChangeListViewAction.Invoke(client.clientName, StaticDefine.ADD_USER_LIST);
                            string accessLog = string.Format("[{0}] {1} Access Server", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), client.clientName);
                            ChangeListViewAction.Invoke(accessLog, StaticDefine.ADD_ACCESS_LIST);
                            File.AppendAllText("AccessRecord.txt", accessLog + "\n");
                            return;
                        }
                    }
                }

                if (messageParsingAction != null)
                {
                    /*messageParsingAction.BeginInvoke(client.clientName, strData, null, null);*/
                    messageParsingAction.Invoke(client.clientName, strData);
                }
            }
            catch (Exception e)
            {
                // ChangeListViewAction.Invoke(string.Format("[{0}] [{1}] {2} ", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), e.Message, e.StackTrace), StaticDefine.ADD_ACCESS_LIST);
            }
        }

        private bool CheckID(string ID)
        {
            if (ID.Contains("%^&"))
            {
                return true;
            }
            return false;
        }

    }
}
