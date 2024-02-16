using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace ChattingServiceServer
{
    /// <summary>
    /// 채팅 서버 주요 로직 작성 클래스
    /// </summary>
    public partial class MainWindow : Window
    {
        // ?
        private object lockObj = new object();
        // 화면과 연동시키기 위한 콜렉션 클래스 -> 해당 데이터가 업데이트될 경우, 연동된 화면 상의 출력도 실시간으로 업데이트
        private ObservableCollection<string> chattingLogList = new ObservableCollection<string>();
        private ObservableCollection<string> userList = new ObservableCollection<string>();
        private ObservableCollection<string> AccessLogList = new ObservableCollection<string>();
        // 연결된 클라이언트 heartbeat를 확인하기 위한 Task 객체
        Task connectionCheckThread = null;

        // 기본 프로그램 시작점이며, 시작 화면은 App.xaml에서 수정 가능
        public MainWindow()
        {
            InitializeComponent();
            // 서버 실행
            MainServerStart();
            
            // 클라이언트 관리 클래스에 연동 이벤트 할당 (특이하게 Action 맵에 += 연산식을 사용하여 추가하는 방식으로 전달하는 파라미터도 동일하게 구성되어야 한다)
            ClientManager.messageParsingAction += MessageParsing;
            ClientManager.ChangeListViewAction += ChangeListView;
            
            // 화면상에 연동시킬 ObservableCollection 매핑
            ChattingLogListView.ItemsSource = chattingLogList;
            UserListView.ItemsSource = userList;
            AccessLogListView.ItemsSource = AccessLogList;

            // 연결된 클라이언트 heartbeat를 확인하는 task 실행
            connectionCheckThread = new Task(ConnectCheckLoop);
            connectionCheckThread.Start();

        }

        // 클라이언트 heartbeat 확인 함수 - 1초마다 실행
        private void ConnectCheckLoop()
        {
            while (true)
            {
                // 클라이언트 관리 클래스에 저장되어 있는 클라이언트 맵 목록 기준으로 각 클라이언트에 특정 메세지를 전달하고, 예외가 발생 시 해당 클라이언트 제거
                foreach (var item in ClientManager.clientDic)
                {
                    try
                    {
                        string sendStringData = "관리자<TEST>";
                        byte[] sendByteData = new byte[sendStringData.Length];
                        sendByteData = Encoding.Default.GetBytes(sendStringData);

                        item.Value.tcpClient.GetStream().Write(sendByteData, 0, sendByteData.Length);

                    }
                    catch (Exception e)
                    {
                        RemoveClient(item.Value);
                    }
                }
                Thread.Sleep(1000);
            }
        }

        // 클라이언트 heartbeat 확인 함수에서 응답이 없는 클라이언트를 지우는 함수
        private void RemoveClient(ClientData targetClient)
        {
            ClientData result = null;
            ClientManager.clientDic.TryRemove(targetClient.clientNumber, out result);
            if(result != null)
            {
                // 응답이 없는 클라이언트는 로그를 남긴다
                string leaveLog = string.Format("[{0}] {1} Leave Server", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), result.clientName);
                ChangeListView(leaveLog, StaticDefine.ADD_ACCESS_LIST);
                ChangeListView(result.clientName, StaticDefine.REMOVE_USER_LIST);
            }
        }

        // 상황별 메세지를 각 ObserableCollection에 저장하는 함수 (화면상 목록 변경 함수)
        private void ChangeListView(string Message, int key)
        {
            switch (key)
            {
                case StaticDefine.ADD_ACCESS_LIST:
                    {
                        Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() =>
                        {
                            AccessLogList.Add(Message);
                        }));
                        break;
                    }
                case StaticDefine.ADD_CHATTING_LIST:
                    {
                        Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() =>
                        {
                            chattingLogList.Add(Message);
                        }));
                        break;
                    }
                case StaticDefine.ADD_USER_LIST:
                    {
                        Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() =>
                        {
                            userList.Add(Message);
                        }));
                        break;
                    }
                case StaticDefine.REMOVE_USER_LIST:
                    {
                        Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() =>
                        {
                            userList.Remove(Message);
                        }));
                        break;
                    }
                default:
                    break;
            }
        }

        // 수신된 메세지 parsing 함수
        private void MessageParsing(string sender, string message)
        {
            lock (lockObj)
            {
                List<string> msgList = new List<string>();

                string[] msgArray = message.Split('>');
                foreach(var item in msgArray)
                {
                    if(string.IsNullOrEmpty(item)) continue;
                    msgList.Add(item);
                }
                SendMsgToClient(msgList, sender);
            }
        }

        // 수신된 메세지를 연결된 클라이언트에게 전달하는 함수
        // 수신된 메세지의 특정 문구나 특수기호에 따라 어떤 문자인지 판별
        // <GroupChattingStart> : 그룹 채팅 시작 시점에 수신되는 메세지로, '#참여자1#참여자2,...<GroupChattingStart>'형태로 구성
        // #가 포함된 메세지 : 그룹 채팅 참여자 목록 수신 문자 ex) #참여자1#참여자2#닉네임1#닉네임2
        // <GiveMeUserList> : 현재 접속 명단 요청 시 수신되는 문구 (클라이언트 -> 서버로 요청하는 메세지)
        // <ChattingStart> : 1:1 채팅 시작 시점에 수신되는 메세지로, '채팅상대명<ChattingStart>' 형태로 구성 -> 수신된 메세지를 기준으로 전달자 및 수신자에게 상대방의 닉네임을 전달
        private void SendMsgToClient(List<string> msgList, string sender)
        {
            // 로그 기록을 위한 메세지 파싱 변수
            string parsedMessage = "";
            // 수신된 메세지 변수
            string _message = "";
            // 전달 대상자 명
            string receiver = "";

            // Dictionary에 저장된 전달자 번호
            int senderNumber = -1;
            // Dictionary에 저장된 수신자 번호
            int receiverNumber = -1;

            // 수신된 메세지 목록 for문 실행
            // 수신된 메세지 목록은 '>' 기준으로 split된 메세지 목록이므로 항목 내에는 '>'가 포함되지 않음
            foreach(var item in msgList)
            {
                string[] splitedMsg = item.Split("<");

                _message = splitedMsg[1];

                receiver = splitedMsg[0];
                parsedMessage = string.Format("{0}<{1}>", sender, _message);

                // 1. 그룹 채팅 시작 문자인지 확인
                if (parsedMessage.Contains("<GroupChattingStart>"))
                {
                    // 그룹 채팅 대상 목록 split
                    string[] groupSplit = receiver.Split("#");

                    foreach(var el in groupSplit)
                    {
                        if(string.IsNullOrEmpty(el)) continue;

                        // 서버에서 수집하는 로그 목록에 저장
                        string groupLogMessage = string.Format(@"[{0}] [{1}] -> [{2}], {3}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), groupSplit[0], el, _message);
                        ChangeListView(groupLogMessage, StaticDefine.ADD_CHATTING_LIST);

                        // 수신자 채번 번호 확인
                        receiverNumber = GetClientNumber(el);

                        // 그룹 채팅 대상자에게 채팅 시작 문구 전달 (클라이언트에서는 해당 문구가 수신되면 채팅창을 실행)
                        parsedMessage = string.Format("{0}<GroupChattingStart>", receiver);
                        byte[] sendGroupByteData = Encoding.Default.GetBytes(parsedMessage);
                        ClientManager.clientDic[receiverNumber].tcpClient.GetStream().Write(sendGroupByteData, 0, sendGroupByteData.Length);
                    }
                    return;
                }

                // 2. 그룹 채팅 전달 메세지인지 확인
                if (receiver.Contains("#"))
                {
                    // 그룹 채팅 대상 목록 split
                    string[] groupSplit = receiver.Split("#");
                    string groupName = null;

                    foreach(var el in groupSplit)
                    {
                        groupName = groupSplit[0];

                        if (string.IsNullOrEmpty(el)) continue;
                        if(el == groupName) continue;

                        // 서버에서 수집하는 로그 목록에 저장
                        string groupLogMessage = string.Format(@"[{0}] [{1}] -> [{2}] , {3}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), groupName, el, _message);
                        ChangeListView(groupLogMessage, StaticDefine.ADD_CHATTING_LIST);

                        // 수신자 채번 번호 확인
                        receiverNumber = GetClientNumber(el);

                        // 그룹 채팅 대상자에게 수신 메세지 전달
                        parsedMessage = string.Format("{0}<{1}>", receiver, _message);
                        byte[] sendGroupByteData = Encoding.Default.GetBytes(parsedMessage);
                        ClientManager.clientDic[receiverNumber].tcpClient.GetStream().Write(sendGroupByteData, 0, sendGroupByteData.Length);
                    }
                    return;
                }

                // 수신자 및 전달자 채번 번호 확인
                senderNumber = GetClientNumber(sender);
                receiverNumber = GetClientNumber(receiver);

                // 관리되지 않는 채팅자인지 확인
                if(senderNumber == -1 || receiverNumber == -1) return;

                byte[] sendByteData = new byte[parsedMessage.Length];
                sendByteData = Encoding.Default.GetBytes(parsedMessage);

                // 3. 채팅 시작 전 현재 접속 중인 유저 목록 조회 요청 문자인지 확인
                if (parsedMessage.Contains("<GiveMeUserList>"))
                {
                    // 현재 접속 중인 유저 파싱(prefix '관리자'는 고정) ex) 관리자<$유저1$유저2$유저3...>
                    string userListStringData = "관리자<";
                    foreach(var el in userList)
                    {
                        userListStringData += string.Format("${0}", el);
                    }
                    userListStringData += ">";
                    byte[] userListByteData = new byte[userListStringData.Length];
                    userListByteData = Encoding.Default.GetBytes(userListStringData);
                    ClientManager.clientDic[receiverNumber].tcpClient.GetStream().Write(userListByteData, 0, userListByteData.Length);
                    return;
                }

                // 채팅 기록 로그에 해당 문구 저장
                string logMessage = string.Format(@"[{0}] [{1}] -> [{2}] , {3}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), sender, receiver, _message);
                ChangeListView(logMessage, StaticDefine.ADD_CHATTING_LIST);

                // 4. 일반 채팅 시작 문자인지 확인
                // 수신 대상자 및 전달자에게 채팅 시작 문구 전달 (클라이언트에서는 해당 문구가 수신되면 채팅창을 실행)
                if (parsedMessage.Contains("<ChattingStart>"))
                {
                    // 수신 대상자에게 시작 문구 전달
                    parsedMessage = string.Format("{0}<ChattingStart>", receiver);
                    sendByteData = Encoding.Default.GetBytes(parsedMessage);
                    ClientManager.clientDic[senderNumber].tcpClient.GetStream().Write(sendByteData, 0, sendByteData.Length);
                    
                    // 전달자게에도 시작 문구 전달
                    parsedMessage = string.Format("{0}<ChattingStart>", sender);
                    sendByteData = Encoding.Default.GetBytes(parsedMessage);
                    ClientManager.clientDic[receiverNumber].tcpClient.GetStream().Write(sendByteData, 0, sendByteData.Length);
                    return;
                }

                // 5. 일반 채팅 메세지 전달인 경우 바로 수신자에게 메세지 전달
                ClientManager.clientDic[receiverNumber].tcpClient.GetStream().Write(sendByteData, 0, sendByteData.Length);
            }
        }

        // 서버에서 관리하는 클라이언트 관리 클래스에 저장되어 있는 Dictionary에서 해당 사용자의 번호를 return하는 함수
        private int GetClientNumber(string targetClientName)
        {
            foreach(var item in ClientManager.clientDic)
            {
                if(item.Value.clientName == targetClientName) return item.Value.clientNumber;
            }
            return -1;
        }

        // 서버 시작 함수
        private void MainServerStart()
        {
            MainServer a = new MainServer();
        }
    }
}
