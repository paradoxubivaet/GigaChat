using Server.Service;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    public class Server
    {
        private string ip;
        private string port;

        private TcpListener tcpListener;
        private UdpClient udpClient;
        private IPEndPoint localEndPoint;

        private bool listeningFlag;
        private int temporaryId;

        // temporary information
        private List<SessionInformation> temporarySessionInformation;
        private List<SessionInformation> temporaryLoginedUsersInformation;
        private List<MessagesStorage> temporaryMessagesStorage;

        // ОБРАТИТЬ ВНИМАНИЕ 
        private ObservableCollection<string> messagesObservable = new ObservableCollection<string>();

        // ОБРАТИТЬ ВНИМАНИЕ 2
        private List<ObservableMessageStore> observableListMessageStore = new List<ObservableMessageStore>();
        
        // commands list
        private List<string> commandList = new List<string> { "login", "/register", "/kick", "/ban", "/mute", 
                                                              "/unban", "/givenickname" };

        // services
        private IControllDataBase controllDataBase;

        private List<byte[]> temporaryStorage;

        public Server(string ip, string port)
        {
            Ip= ip; 
            Port= port;

            localEndPoint = new IPEndPoint(IPAddress.Parse(ip), Int32.Parse(port));

            listeningFlag = false;
            temporaryId = 0;

            udpClient = new UdpClient(8000);
            temporarySessionInformation = new List<SessionInformation>();

            controllDataBase = new ControllDataBase();
            temporaryStorage = new List<byte[]>();
            temporaryMessagesStorage = new List<MessagesStorage>();
            temporaryLoginedUsersInformation = new List<SessionInformation>();


            // Обратить внимание
            // messagesObservable.CollectionChanged += DisplayMessageFromObservable_CollectionChanged;
            // Обратить внимание 2 
            

        }

        public string Ip
        {
            get
            {
                return ip;
            }
            set
            {
                ip = value;
            }
        }

        public string Port
        {
            get
            {
                return ip;
            }
            set
            {
                port = value;
            }
        }

        public void CreateTclListener()
        {
            tcpListener = new TcpListener(localEndPoint);
        }

        // Этот метод должен выполняться до момента, пока не будет исполнен StopListen().
        // Метод должен выполняться асинхронно

        public void StartServer()
        {
            tcpListener.Start();
        }

        public async Task StartListenAsync()
        {
            await Task.Run( async () =>
            {
                listeningFlag = true;
                while (listeningFlag)
                {
                    var tcpClient = tcpListener.AcceptTcpClient();

                    await CreateSessionInformationAsync(tcpClient);
                }
            });
        }

        public async Task CreateSessionInformationAsync(TcpClient tcpClient)
        {
            await Task.Run(() =>
            {
                var sessingInfo = new SessionInformation();

                sessingInfo.Id = temporaryId;
                sessingInfo.TcpClient = tcpClient;
                sessingInfo.NetworkStream = tcpClient.GetStream();
                sessingInfo.MessageStorage = new List<byte[]>();

                temporarySessionInformation.Add(sessingInfo);

                temporaryId++;
            });
        }

        public async Task UdpReceiveAsync()
        {
            await Task.Run(async () =>
            {
                while (true)
                {
                    var udpReceiveResult = await udpClient.ReceiveAsync();
                    byte[] data = udpReceiveResult.Buffer;

                    var checkAuthorize = CheckUserAuthorization(udpReceiveResult);

                    var ip = ((IPEndPoint)(udpReceiveResult.RemoteEndPoint)).Address;

                    //MessagesStorage messagesStorage = new MessagesStorage();
                    //messagesStorage.Ip = ip;
                    //messagesStorage.Messages.Add(data);

                    ObservableMessageStore observableMessageStore = new ObservableMessageStore();
                    observableMessageStore.Ip = ip;
                    observableMessageStore.Messages.CollectionChanged += DisplayMessageFromObservable_CollectionChanged;
                    observableMessageStore.Messages.Add(data);

                    if (checkAuthorize.Authorize)
                    {
                        temporarySessionInformation[checkAuthorize.Id].MessageStorage.Add(data);
                    }
                    else
                    {
                        //temporaryMessagesStorage.Add(messagesStorage);
                        //messagesObservable.Add(Encoding.UTF8.GetString(data));
                        observableListMessageStore.Add(observableMessageStore);


                        string answer = "Вы не можете отправлять сообщения, пока не авторизируетесь.\r\n" +
                                        "Авторизация: /login [username] [password]\r\n" +
                                        "Регистрация: /register [username] [password]";

                        await SendUdp(ip, answer);
                    }
                }
            });
        }

        public void DisplayMessageFromObservable_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                var s = Encoding.UTF8.GetString((byte[])(e.NewItems[0]));
                Console.WriteLine(s);
                

                DetermineMessageTypeObservable((byte[])(e.NewItems[0]));
            }
        }

        //public void DisplayMessageFromObservable_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        //{
        //    if (e.Action == NotifyCollectionChangedAction.Add) 
        //    {
        //        Console.WriteLine(e.NewItems[0]);
        //    }
        //}

        public SessionInformation CheckWhoIs(UdpReceiveResult result)
        {
            var ip = ((IPEndPoint)(result.RemoteEndPoint)).Address;

            return temporarySessionInformation.Single(x => ((IPEndPoint)(x.TcpClient.Client.RemoteEndPoint)).Address == ip);
        }

        public AuthorizeState CheckUserAuthorization(UdpReceiveResult result)
        {
            var ip = ((IPEndPoint)(result.RemoteEndPoint)).Address;
            AuthorizeState state = new AuthorizeState();
            //state.Address = ip;

            if (temporaryLoginedUsersInformation.Count != 0)
            {
                foreach (var userLogined in temporaryLoginedUsersInformation)
                {
                    if (userLogined.Address == ip) 
                    {
                        state.Id = userLogined.Id;
                        state.Authorize = true;

                        return state;
                    }
                    else 
                        state.Authorize = false;
                }
            }
            state.Authorize = false;

            return state;
        }

        public void DetermineMessageTypeObservable(byte[] data)
        {
            var message = Encoding.UTF8.GetString(data);

            if (commandList.Any(x => message.Contains(x)))
                DetermineCommandType(message);
        }

        public async Task SendUdp(IPAddress address, string message)
        {
            await Task.Run(() =>
            {
                byte[] data = Encoding.UTF8.GetBytes(message);

                udpClient.Connect("127.0.0.1", 7000);
                udpClient.Send(data, data.Length);
            });
        }

        public void DetermineCommandType(string message)
        {
            string[] commandParameters = message.Split(' ');

            User user = new User(commandParameters[1], commandParameters[2], "normal");

            if (message.Contains("/login"))
            {
                controllDataBase.CheckingUser(commandParameters[1], commandParameters[2]);
            }
            else if(message.Contains("/register"))
            {
                if(!controllDataBase.CheckingUser(commandParameters[1], commandParameters[2]))
                    controllDataBase.Add(user);
                SendUdp();
            }
        }

        // Переписать метод так, чтобы он выводил сообщения в порядке их поступления (первый пришел - первый вывелся и разослался)
        public async void DisplayMessageAsync()
        {
            await Task.Run(() =>
            {
                while (true)
                {
                    if (temporaryMessagesStorage.Count != 0)
                    {
                        for (int i = 0; i < temporaryMessagesStorage.Count; i++)
                        {
                                if (temporaryMessagesStorage[i].Messages.Count != 0)
                            {
                                for (int j = 0; j < temporaryMessagesStorage[i].Messages.Count; j++)
                                {
                                    var message = Encoding.UTF8.GetString(temporaryMessagesStorage[i].Messages[j]);
                                    Console.WriteLine(message);

                                    temporaryMessagesStorage[i].Messages.RemoveAt(j);
                                    j--;
                                }
                            }
                        }
                    }
                }
            });
        }


        public void StopListen()
        {
            listeningFlag = false;
            tcpListener.Stop();
        }
        
        // Чтобы знать, от кого пришло сообщение по udp, нужно при получении клиента тут же заносить его в базу и присваивать ему ID 

        // Этот метод должен выполняться асинхронно и ... потокобезопасно? 
        //public void GetUdpClients()
        //{
        //    for(int i=0; i< temporarySessionInformation.Count; i++)
        //    {
        //        IPAddress ip = ((IPEndPoint)(temporarySessionInformation[i].TcpClient.Client.RemoteEndPoint)).Address;

        //        IPEndPoint remoteIPEndPoint = new IPEndPoint(ip ,8000);
        //        UdpClient udpClient = new UdpClient(remoteIPEndPoint);

        //        temporarySessionInformation[i].UdpClient = udpClient; 
        //    }
        //}

        //public async Task DisplayMessageAsync()
        //{
        //    await Task.Run(() =>
        //    {
        //        while (true)
        //        {
        //            if (temporaryStorage.Count != 0)
        //            {
        //                for (int i = 0; i < temporaryStorage.Count; i++)
        //                {
        //                    var message = Encoding.UTF8.GetString(temporaryStorage[i]);
        //                    Console.WriteLine(message);
        //                    temporaryStorage.RemoveAt(i);
        //                }
        //            }
        //        }
        //    });
        //}

        // 2 метода ниже оставлены для понимания дальшейших действий
        // Этот метод должен выполняться асинхронно
        // 
        //public void UdpReceive()
        //{
        //    for(int i=0; i < temporarySessionInformation.Count; i++)
        //    {
        //        IPEndPoint ip = (IPEndPoint)temporarySessionInformation[i].TcpClient.Client.RemoteEndPoint;
        //        IPEndPoint iPEndPoint = new IPEndPoint(ip.Address, 8000);

        //        UdpClient udpClient = new UdpClient(iPEndPoint);

        //        UdpState us = new UdpState();
        //        us.udpClient = udpClient;
        //        us.ip = iPEndPoint;
        //        us.id = temporarySessionInformation[i].Id;

        //        udpClient.BeginReceive(new AsyncCallback(ReceviceCallback), us);
        //    }
        //}
        
        //private void ReceviceCallback(IAsyncResult ar)
        //{
        //    UdpClient udpClient = ((UdpState)(ar.AsyncState)).udpClient;
        //    IPEndPoint iPEndPoint = ((UdpState)(ar.AsyncState)).ip;
        //    int id = ((UdpState)(ar.AsyncState)).id;

        //    byte[] data = udpClient.EndReceive(ar, ref iPEndPoint);

        //    temporarySessionInformation.Single(x => x.Id == id).MessageStorage.Add(data);
        //}

        // Этот метод должен выполняться сразу после появление сообщения в хранилище сообщений 
        //public void DetermineMessageType()
        //{
        //    for (int j = 0; j < temporarySessionInformation.Count; j++) 
        //    {
        //        for (int i = 0; i < temporarySessionInformation[j].MessageStorage.Count; i++)
        //        {
        //            byte[] data = temporarySessionInformation[j].MessageStorage[i];

        //            var message = Encoding.UTF8.GetString(data);

        //            if (commandList.Any(s => message.Contains(s)))
        //                DetermineCommand(message, temporarySessionInformation[i].Id);
        //            else
        //                SendMessageUsers(data);
        //        }
        //    }
        //}

        // Этот метод определяет тип команды и должен выполняться ... асинхронно? 
        //public void DetermineCommand(string message, int id)
        //{
        //    if (message.Contains("/register"))
        //    {
        //        RegisterNewUser(message, id);
        //    }
        //    else if (message.Contains("/login"))
        //    {
        //        LoginUser(message, id);
        //    }
            
        //}

        // Этот метод должен выполняться асихнронно 
        //public void SendMessageUsers(byte[] data)
        //{
        //    for(int i = 0; i< temporarySessionInformation.Count; i++)
        //    {
        //        temporarySessionInformation[i].UdpClient.Send(data, data.Length);
        //    }
        //}

        // И этот метод должен выполняться асинхронно 
        //public void RegisterNewUser(string message, int id)
        //{
        //    string[] loginPassword = message.Split(' ');

        //    string login = loginPassword[1];
        //    string password = loginPassword[2];

        //    if (!controllDataBase.CheckingUser(login, password))
        //    {
        //        // Изменить статус с normal на limited
        //        // При регистрации новому User присваивается статус limited, позволяющий пользоваться только командами /register, /login.
        //        // После удачной команды /login пользователю присвается статус normal, при котором он может отправлять сообщения.
        //        User user = new User(login, password, "normal");

        //        temporarySessionInformation[id].User = user;
        //        controllDataBase.Add(user);
        //    }
        //    else
        //        return;
        //}

        // И этот метод должен выполняться асинхронно
        //public void LoginUser(string message, int id)
        //{
        //    // Уровень доступа меняется. Объекту User присваивается другой статус normal
        //}
    }

    

    public class UdpState
    {
        public UdpClient udpClient;
        public IPEndPoint ip;
        public int id;
    }

    public struct AuthorizeState
    {
        public int Id;
        public bool Authorize;
        public IPAddress Address;
    }

    public struct StatesObj
    {
        public IPAddress Address;
        public int Id;
        public bool Authorize;
    }
}
