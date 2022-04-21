using Server.Service;
using System;
using System.Collections.Generic;
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
        
        // commands list
        private List<string> commandList = new List<string> { "/kick", "/ban", "/mute", "/unban", "/givenickname" };

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
        // Метод должен выполняться асинхронно(или в другом потоке)

        public void StartServer()
        {
            tcpListener.Start();
        }

        public async Task StartListenAsync()
        {
            await Task.Run(() =>
            {
                listeningFlag = true;
                while (listeningFlag)
                {
                    var tcpClient = tcpListener.AcceptTcpClient();
                }
            });
        }

        public void ListenCallback(IAsyncResult ar)
        {
            var sessingInfo = new SessionInformation();

            TcpListener tcpListener = (TcpListener)ar.AsyncState;

            TcpClient tcpClient = tcpListener.EndAcceptTcpClient(ar);

            sessingInfo.Id = temporaryId;
            sessingInfo.TcpClient = tcpClient;
            sessingInfo.NetworkStream = tcpClient.GetStream();
            sessingInfo.MessageStorage = new List<byte[]>();

            temporarySessionInformation.Add(sessingInfo);

            temporaryId++;
        }

        public void StopListen()
        {
            listeningFlag = false;
            tcpListener.Stop();
        }
        
        // Этот метод должен выполняться асинхронно и ... потокобезопасно? 
        public void GetUdpClients()
        {
            for(int i=0; i< temporarySessionInformation.Count; i++)
            {
                IPAddress ip = ((IPEndPoint)(temporarySessionInformation[i].TcpClient.Client.RemoteEndPoint)).Address;

                IPEndPoint remoteIPEndPoint = new IPEndPoint(ip ,8000);
                UdpClient udpClient = new UdpClient(remoteIPEndPoint);

                temporarySessionInformation[i].UdpClient = udpClient; 
            }
        }

        // Асинхронный прием сообщений по udp 
        public async Task UdpReceiveAsync()
        {
            await Task.Run(async () => 
            {
                while (true)
                {
                    var udpReceiveResult = await udpClient.ReceiveAsync();
                    byte[] data = udpReceiveResult.Buffer;
                    Console.WriteLine(Encoding.UTF8.GetString(data));
                    temporaryStorage.Add(data);
                }
            });
        }

        // 2 метода ниже оставлены для понимания дальшейших действий
        // Этот метод должен выполняться асинхронно
        // 
        public void UdpReceive()
        {
            for(int i=0; i < temporarySessionInformation.Count; i++)
            {
                IPEndPoint ip = (IPEndPoint)temporarySessionInformation[i].TcpClient.Client.RemoteEndPoint;
                IPEndPoint iPEndPoint = new IPEndPoint(ip.Address, 8000);

                UdpClient udpClient = new UdpClient(iPEndPoint);

                UdpState us = new UdpState();
                us.udpClient = udpClient;
                us.ip = iPEndPoint;
                us.id = temporarySessionInformation[i].Id;

                udpClient.BeginReceive(new AsyncCallback(ReceviceCallback), us);
            }
        }
        
        private void ReceviceCallback(IAsyncResult ar)
        {
            UdpClient udpClient = ((UdpState)(ar.AsyncState)).udpClient;
            IPEndPoint iPEndPoint = ((UdpState)(ar.AsyncState)).ip;
            int id = ((UdpState)(ar.AsyncState)).id;

            byte[] data = udpClient.EndReceive(ar, ref iPEndPoint);

            temporarySessionInformation.Single(x => x.Id == id).MessageStorage.Add(data);
        }

        // Этот метод должен выполняться сразу после появление сообщения в хранилище сообщений 
        public void DetermineMessageType()
        {
            for (int j = 0; j < temporarySessionInformation.Count; j++) 
            {
                for (int i = 0; i < temporarySessionInformation[j].MessageStorage.Count; i++)
                {
                    byte[] data = temporarySessionInformation[j].MessageStorage[i];

                    var message = Encoding.UTF8.GetString(data);

                    if (commandList.Any(s => message.Contains(s)))
                        DetermineCommand(message, temporarySessionInformation[i].Id);
                    else
                        SendMessageUsers(data);
                }
            }
        }

        // Этот метод определяет тип команды и должен выполняться ... асинхронно? 
        public void DetermineCommand(string message, int id)
        {
            if (message.Contains("/register"))
            {
                RegisterNewUser(message, id);
            }
            else if (message.Contains("/login"))
            {
                LoginUser(message, id);
            }
            
        }

        // Этот метод должен выполняться асихнронно 
        public void SendMessageUsers(byte[] data)
        {
            for(int i = 0; i< temporarySessionInformation.Count; i++)
            {
                temporarySessionInformation[i].UdpClient.Send(data, data.Length);
            }
        }

        // И этот метод должен выполняться асинхронно 
        public void RegisterNewUser(string message, int id)
        {
            string[] loginPassword = message.Split(' ');

            string login = loginPassword[1];
            string password = loginPassword[2];

            if (!controllDataBase.CheckingUser(login, password))
            {
                // Изменить статус с normal на limited
                // При регистрации новому User присваивается статус limited, позволяющий пользоваться только командами /register, /login.
                // После удачной команды /login пользователю присвается статус normal, при котором он может отправлять сообщения.
                User user = new User(login, password, "normal");

                temporarySessionInformation[id].User = user;
                controllDataBase.Add(user);
            }
            else
                return;
        }

        // И этот метод должен выполняться асинхронно
        public void LoginUser(string message, int id)
        {
            // Уровень доступа меняется. Объекту User присваивается другой статус normal
        }
    }

    

    public class UdpState
    {
        public UdpClient udpClient;
        public IPEndPoint ip;
        public int id;
    }
}
