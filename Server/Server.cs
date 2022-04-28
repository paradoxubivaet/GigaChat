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

        // NOTE
        private ObservableCollection<ObservableMessageStore> observableListMessageStore = new ObservableCollection<ObservableMessageStore>();
        private ObservableCollection<SessionInformation> observableSessionInformationStore = new ObservableCollection<SessionInformation>();
        
        // commands list
        private List<string> commandList = new List<string> { "login", "/register", "/kick", "/ban", "/mute", 
                                                              "/unban", "/givenickname" };

        // services
        private IControllDataBase controllDataBase;

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
            //temporaryStorage = new List<byte[]>();
            temporaryMessagesStorage = new List<MessagesStorage>();
            temporaryLoginedUsersInformation = new List<SessionInformation>();

            
            //observableListMessageStore.CollectionChanged += HadnlerMessageObservable_CollectionChanged;

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

                sessingInfo.Status = "Nonauthorize";
                sessingInfo.Id = temporaryId;
                sessingInfo.TcpClient = tcpClient;
                sessingInfo.NetworkStream = tcpClient.GetStream();
                sessingInfo.MessageStorage = new ObservableCollection<byte[]>();

                temporarySessionInformation.Add(sessingInfo);

                var ip = ((IPEndPoint)(tcpClient.Client.RemoteEndPoint)).Address;
                sessingInfo.Address = ip;

                temporaryId++;
            });
        }

        // Принимающий метод должен лишь принимать
        public async Task UdpReceiveAsyncSecond()
        {
            await Task.Run(async () =>
            {
                while (true)
                {
                    var udpReceiveResult = await udpClient.ReceiveAsync();
                    byte[] data = udpReceiveResult.Buffer;

                    var checkAuthorize = CheckUserAuthorization(udpReceiveResult);
                    var ip = ((IPEndPoint)(udpReceiveResult.RemoteEndPoint)).Address;

                    if (observableSessionInformationStore.Any(x => x.Address.ToString() == ip.ToString()))
                    {
                        if(observableSessionInformationStore.First(x => x.Address.ToString() == ip.ToString()).Status == "Authorized")
                        {
                            await SendUdpAllUsers(data);
                            observableSessionInformationStore.First(x => x.Address.ToString() == ip.ToString()).MessageStorage.Add(data);
                        }
                        else
                        {
                            observableSessionInformationStore.First(x => x.Address.ToString() == ip.ToString()).MessageStorage.Add(data);
                        }
                    }
                    else
                    {
                        SessionInformation sessionInformation = new SessionInformation();
                        sessionInformation.MessageStorage = new ObservableCollection<byte[]>();
                        sessionInformation.MessageStorage.CollectionChanged += 
                                            DisplayMessageFromSessionInformation_CollectionChanged;
                        sessionInformation.Address = ip;
                        sessionInformation.Status = "Nonauthorize";
                        observableSessionInformationStore.Add(sessionInformation);
                        sessionInformation.MessageStorage.Add(data);

                        string answer = "Вы не можете отправлять сообщения, пока не авторизируетесь.\r\n" +
                                        "Авторизация: /login [username] [password]\r\n" +
                                        "Регистрация: /register [username] [password]";

                        await SendUdp(ip, answer);
                    }
                }
            });
        }

        private async void DisplayMessageFromSessionInformation_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                var bytes = (byte[])(e.NewItems[0]);
                var s = Encoding.UTF8.GetString(bytes);
                Console.WriteLine(s);

                var ip = observableSessionInformationStore.First(x => x.MessageStorage.Any(t => t == bytes)).Address;

                await DetermineMessageTypeObservableAsync(bytes, ip);
            }
        }

        //public async void DisplayMessageFromObservable_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        //{
        //    if (e.Action == NotifyCollectionChangedAction.Add)
        //    {
        //        var bytes = (byte[])(e.NewItems[0]);
        //        var s = Encoding.UTF8.GetString(bytes);
        //        Console.WriteLine(s);

        //        // Needs to be redone. Do you agree? 
        //        var ip = observableListMessageStore.First(x => x.Messages.Any(t => t == bytes)).Ip;

        //        //DetermineMessageTypeObservable((byte[])(e.NewItems[0]));
        //        DetermineMessageTypeObservableAsync(bytes, ip);
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

        public async Task DetermineMessageTypeObservableAsync(byte[] data, IPAddress address)
        {
            await Task.Run(async () =>
            {
                var message = Encoding.UTF8.GetString(data);

                if (commandList.Any(x => message.Contains(x)))
                    await DetermineCommandTypeAsync(message, address);
            });
        }

        public async Task DetermineCommandTypeAsync(string message, IPAddress address)
        {
            await Task.Run(async () =>
            {
                string[] commandParameters = message.Split(' ');
                User user = new User(commandParameters[1], commandParameters[2], "normal");

                if (message.Contains("/login"))
                {
                    if(controllDataBase.CheckingUser(commandParameters[1], commandParameters[2]))
                    {
                        //var sessionInfo = new SessionInformation();
                        //sessionInfo.User = user;
                        //sessionInfo.Address = address;

                        var session = observableSessionInformationStore.Select(x => x).First(x => x.Address == address);
                        session.Status = "Authorized";
                        session.User = user;


                        await SendUdp(address, "Вы успешно вошли в систему");
                    }

                }
                else if (message.Contains("/register"))
                {
                    if (!controllDataBase.CheckingUser(commandParameters[1], commandParameters[2]))
                    {
                        controllDataBase.Add(user);
                        return;
                    }
                    await SendUdp(address, "Пользователь уже зарегистрирован");
                }
            });
        }

        public async Task SendUdpAllUsers(byte[] message)
        {
            await Task.Run(async () =>
            {
                var collection = observableSessionInformationStore.Where(x => x.Status == "Authorized").Select(x => x.Address);
                Parallel.ForEach(collection, async address => 
                {
                    await SendUdp(address, message);
                });
            });
        }

        // do something with the two methods below
        public async Task SendUdp(IPAddress address, byte[] message)
        {
            await Task.Run(() =>
            {
                udpClient.Send(message, message.Length);
            });
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

        // Переписать метод так, чтобы он выводил сообщения в порядке их поступления (первый пришел - первый вывелся и разослался)
        //public async void DisplayMessageAsync()
        //{
        //    await Task.Run(() =>
        //    {
        //        while (true)
        //        {
        //            if (temporaryMessagesStorage.Count != 0)
        //            {
        //                for (int i = 0; i < temporaryMessagesStorage.Count; i++)
        //                {
        //                        if (temporaryMessagesStorage[i].Messages.Count != 0)
        //                    {
        //                        for (int j = 0; j < temporaryMessagesStorage[i].Messages.Count; j++)
        //                        {
        //                            var message = Encoding.UTF8.GetString(temporaryMessagesStorage[i].Messages[j]);
        //                            Console.WriteLine(message);

        //                            temporaryMessagesStorage[i].Messages.RemoveAt(j);
        //                            j--;
        //                        }
        //                    }
        //                }
        //            }
        //        }
        //    });
        //}

        public void StopListen()
        {
            listeningFlag = false;
            tcpListener.Stop();
        }
        
        // Чтобы знать, от кого пришло сообщение по udp, нужно при получении клиента тут же заносить его в базу и присваивать ему ID 
    }

    public struct AuthorizeState
    {
        public int Id;
        public bool Authorize;
        public IPAddress Address;
    }
}
