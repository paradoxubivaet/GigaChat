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

        // NOTE
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

            temporaryLoginedUsersInformation = new List<SessionInformation>();

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
                            await SendUdpAllUsers(data, ip);
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
            await Task.Run(() =>
            {
                if (message.Contains("/login"))
                {
                    AuthorizationUser(message, address);
                }
                else if (message.Contains("/register"))
                {
                    RegisterNewUser(message, address);
                }
            });
        }

        // Chat commands --->

        // /Login
        private void AuthorizationUser(string message, IPAddress address)
        {
            Task.Run(async () =>
            {
                string[] commandParameters = message.Split();

                if (commandParameters.Length == 3)
                {
                    User user = new User(commandParameters[1], commandParameters[2], "normal");
                    

                    if (controllDataBase.CheckingUser(commandParameters[1], commandParameters[2]))
                    {
                        var session = observableSessionInformationStore.Select(x => x).First(x => x.Address == address);
                        session.Status = "Authorized";
                        session.User = user;

                        await SendUdp(address, "Вы успешно вошли в систему");
                    }
                    else
                    {
                        await SendUdp(address, "Такого пользователя не существует. Проверьте логин и/или пароль.");
                    }
                }
                else
                    await SendUdp(address, "Неверная команда.");
            });
        }

        private void RegisterNewUser(string message, IPAddress address)
        {
            Task.Run(async () =>
            {
                string[] commandParameters = message.Split(' ');

                if (commandParameters.Length == 3)
                {
                    User user = new User(commandParameters[1], commandParameters[2], "normal");
                    if (!controllDataBase.CheckingUser(commandParameters[1], commandParameters[2]))
                    {
                        controllDataBase.Add(user);
                        return;
                    }
                    else
                        await SendUdp(address, "Пользователь уже зарегистрирован");
                }
                else
                    await SendUdp(address, "Неверная команда.");
            });
        }

        private void KickUser(string message, IPAddress address)
        {

        }

        // Chat commands <---

        public async Task SendUdpAllUsers(byte[] message, IPAddress address)
        {
            await Task.Run(() =>
            {
                var s = Encoding.UTF8.GetString(message);
                var userName = observableSessionInformationStore.First(x => x.Address.ToString() == address.ToString()).User.Name;
                var newMassage = $"{DateTime.Now.ToShortTimeString()} | {userName} | {s}";

                var collection = observableSessionInformationStore.Where(x => x.Status == "Authorized").Where(x => x.Address.ToString() != address.ToString()).Select(x => x.Address);
                Parallel.ForEach(collection, async address => 
                {
                    await SendUdp(address, newMassage);
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
