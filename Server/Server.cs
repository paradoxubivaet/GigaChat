﻿using Server.Service;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
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

        // NOTE
        private ObservableCollection<SessionInformation> observableSessionInformationStore = new ObservableCollection<SessionInformation>();
        
        // commands list
        private List<string> commandList = new List<string> { "login", "/register", "/kick", "/ban", "/mute", 
                                                              "/unmute", "/unban", "/givenickname" };

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

                    //var checkAuthorize = CheckUserAuthorization(udpReceiveResult);
                    var ip = ((IPEndPoint)(udpReceiveResult.RemoteEndPoint)).Address;

                    if (observableSessionInformationStore.Any(x => x.Address.ToString() == ip.ToString()))
                    {
                        if(observableSessionInformationStore.First(x => x.Address.ToString() == ip.ToString()).Status == "Authorized")
                        {
                            var name = observableSessionInformationStore.
                                        First(x => x.Address.ToString() == Ip.ToString()).User.Name;
                            if (controllDataBase.GetUserStatus(name) == "Muted")
                                await SendUdp(ip, "У вас отсутствует язык (Мут)");
                            else
                            {
                                await SendUdpAllUsers(data, ip);
                                observableSessionInformationStore.First(x => x.Address.ToString() == ip.ToString()).MessageStorage.Add(data);
                            }
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

                    Thread.Sleep(1);
                }
            });
        }

        private async void DisplayMessageFromSessionInformation_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                var bytes = (byte[])(e.NewItems[0]);
                var s = Encoding.UTF8.GetString(bytes);
                var ip = observableSessionInformationStore.First(x => x.MessageStorage.Any(t => t == bytes)).Address;

                if (observableSessionInformationStore.First(x => x.Address.ToString() == ip.ToString()).User != null)
                {
                    var userName = observableSessionInformationStore.First(x => x.Address.ToString() == ip.ToString()).User.Name;
                    var mess = $"{DateTime.Now.ToShortTimeString()} | {userName} | {s}";
                    Console.WriteLine(mess);
                }
                else
                    Console.WriteLine(s);

                await DetermineMessageTypeObservableAsync(bytes, ip);
            }
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
                    AuthorizationUser(message, address);
                else if (message.Contains("/register"))
                    RegisterNewUser(message, address);
                else if (message.Contains("/kick"))
                    KickUser(message,address);
                else if (message.Contains("/ban"))
                    BanUser(message,address);
                else if (message.Contains("/unban"))
                    UnbanUser(message,address);
                else if(message.Contains("/mute"))
                    MuteUser(message,address);
                else if (message.Contains("/unmute"))
                    UnmuteUser(message,address);
            });
        }

        // Chat commands --->

        // /Login
        private void AuthorizationUser(string message, IPAddress address)
        {
            // /login blakds 23232ds
            Task.Run(async () =>
            {
                string[] commandParameters = message.Split();

                if (commandParameters.Length == 3)
                {
                    User user = new User(commandParameters[1], commandParameters[2], "Normal");

                    if (controllDataBase.CheckingUser(commandParameters[1], commandParameters[2]))
                    {
                        var status = controllDataBase.GetUserStatus(commandParameters[1]);
                        if (status == "Normal" || status == "Muted")
                        {
                            var session = observableSessionInformationStore.Select(x => x).First(x => x.Address == address);
                            session.Status = "Authorized";
                            session.User = user;

                            await SendUdp(address, "Вы успешно вошли в систему");
                        }
                        else if(status == "Banned")
                            await SendUdp(address, "Вы были забанены.");
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
            // /register blakds 23232ds
            Task.Run(async () =>
            {
                string[] commandParameters = message.Split(' ');

                if (commandParameters.Length == 3)
                {
                    User user = new User(commandParameters[1], commandParameters[2], "Normal");
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
            // /kick blackbroke 
            Task.Run(async () =>
            {
                if (observableSessionInformationStore.First(x => x.Address.ToString() == address.ToString()).Status == "Authorized")
                {
                    string[] commandParameters = message.Split(' ');
                    var name = commandParameters[1];
                    var senderName = observableSessionInformationStore.First(x => x.Address.ToString() == address.ToString()).User.Name;

                    if (commandParameters.Length == 2)
                    {
                        if (controllDataBase.GetAccessUser(senderName) == "Administrator")
                        {
                            if (observableSessionInformationStore.Any(x => x.User.Name == name))
                            {
                                var kickedUser = observableSessionInformationStore.First(x => x.User.Name == name);
                                await SendUdp(kickedUser.Address, "Вы были кикнуты с сервера.");
                                observableSessionInformationStore.Remove(kickedUser);
                            }
                            else
                                await SendUdp(address, "Данного пользователя нет в системе.");
                        }
                        else
                            await SendUdp(address, "У вас недостаточно прав для этой команды.");
                    }
                    else
                        await SendUdp(address, "Неверная команда.");
                }
                else
                    await SendUdp(address, "Вы не авторизованы.");
            });
        }

        // To do something with two the methods below
        private void BanUser(string message, IPAddress address)
        {
            Task.Run(async () =>
            {
                if (observableSessionInformationStore.First(x => x.Address.ToString() == address.ToString()).Status == "Authorized")
                {
                    string[] commandParameters = message.Split(' ');
                    var name = commandParameters[1];
                    var senderName = observableSessionInformationStore.First(x => x.Address.ToString() == address.ToString()).User.Name;

                    if (commandParameters.Length == 2)
                    {
                        // Я понял, в чём ошибка. Нужно передать ник отправителя
                        if (controllDataBase.GetAccessUser(senderName) == "Administrator")
                        {
                            if (controllDataBase.CheckingUser(name))
                            {
                                controllDataBase.SetUserStatus(name, "Banned");
                                var mess = Encoding.UTF8.GetBytes($"Пользователь '{name}' был забанен.");
                                await SendUdpAllUsers(mess);
                            }
                            else
                                await SendUdp(address, "Данного пользователя нет в системе.");
                        }
                        else
                        {
                            Console.WriteLine($"{controllDataBase.GetAccessUser(senderName)}");
                            await SendUdp(address, "У вас недостаточно прав для этой команды.");
                        }
                    }
                    else
                        await SendUdp(address, "Неверная команда");
                }
                else
                    await SendUdp(address, "Вы не авторизованы.");
            });
        }

        private void UnbanUser(string message, IPAddress address)
        {
            Task.Run(async () =>
            {
                if (observableSessionInformationStore.First(x => x.Address.ToString() == address.ToString()).Status == "Authorized")
                {
                    string[] commandParameters = message.Split(' ');
                    var name = commandParameters[1];
                    var senderName = observableSessionInformationStore.First(x => x.Address.ToString() == address.ToString()).User.Name;

                    if (commandParameters.Length == 2)
                    {
                        if (controllDataBase.GetAccessUser(senderName) == "Administrator")
                        {
                            if (controllDataBase.CheckingUser(name))
                            {
                                controllDataBase.SetUserStatus(name, "Normal");
                                var mess = Encoding.UTF8.GetBytes($"Пользователь '{name}' был разбанен.");
                                await SendUdpAllUsers(mess);
                            }
                            else
                                await SendUdp(address, "Данного пользователя нет в системе.");
                        }
                        else
                            await SendUdp(address, "У вас недостаточно прав для этой команды.");
                    }
                    else
                        await SendUdp(address, "Неверная команда");
                }
                else
                    await SendUdp(address, "Вы не авторизованы.");

            });
        }

        private void MuteUser(string message, IPAddress address)
        {
            Task.Run(async () =>
            {
                if (observableSessionInformationStore.First(x => x.Address.ToString() == address.ToString()).Status == "Authorized")
                {
                    string[] commandParameters = message.Split(' ');
                    var name = commandParameters[1];
                    var senderName = observableSessionInformationStore.First(x => x.Address.ToString() == address.ToString()).User.Name;

                    if (commandParameters.Length == 2)
                    {
                        if (controllDataBase.GetAccessUser(senderName) == "Administrator")
                        {
                            if (controllDataBase.CheckingUser(name))
                            {
                                controllDataBase.SetUserStatus(name, "Muted");
                                var mess = Encoding.UTF8.GetBytes($"Пользователь '{name}' был лишен языка.");
                                await SendUdpAllUsers(mess);
                            }
                            else
                                await SendUdp(address, "Данного пользователя нет в системе.");
                        }
                        else
                            await SendUdp(address, "У вас недостаточно прав для этой команды.");
                    }
                    else
                        await SendUdp(address, "Неверная команда");
                }
                else
                    await SendUdp(address, "Вы не авторизованы.");
            });
        }

        private void UnmuteUser(string message, IPAddress address)
        {
            Task.Run(async () =>
            {
                if (observableSessionInformationStore.First(x => x.Address.ToString() == address.ToString()).Status == "Authorized")
                {
                    string[] commandParameters = message.Split(' ');
                    var name = commandParameters[1];
                    var senderName = observableSessionInformationStore.First(x => x.Address.ToString() == address.ToString()).User.Name;

                    if (commandParameters.Length == 2)
                    {
                        if (controllDataBase.GetAccessUser(senderName) == "Administrator")
                        {
                            if (controllDataBase.CheckingUser(name))
                            {
                                controllDataBase.SetUserStatus(name, "Normal");
                                var mess = Encoding.UTF8.GetBytes($"Пользователь '{name}' снова отрастил язык.");
                                await SendUdpAllUsers(mess);
                            }
                            else
                                await SendUdp(address, "Данного пользователя нет в системе.");
                        }
                        else
                            await SendUdp(address, "У вас недостаточно прав для этой команды.");
                    }
                    else
                        await SendUdp(address, "Неверная команда");
                }
                else
                    await SendUdp(address, "Вы не авторизованы.");
            });
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

        public async Task SendUdpAllUsers(byte[] message)
        {
            await Task.Run(() =>
            {
                var s = Encoding.UTF8.GetString(message);
                var newMassage = $"{DateTime.Now.ToShortTimeString()} | SERVER | {s}";

                var collection = observableSessionInformationStore.Where(x => x.Status == "Authorized").Select(x => x.Address);
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
}
