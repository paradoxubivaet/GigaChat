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

        public Server(string ip, string port)
        {
            Ip= ip; 
            Port= port;

            localEndPoint = new IPEndPoint(IPAddress.Parse(ip), Int32.Parse(port));

            listeningFlag = false;
            temporaryId = 0;

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

        public async Task StartListenAsync()
        {
            tcpListener.Start();

            listeningFlag = true;
            while (listeningFlag)
            {
                var tcpClient = await tcpListener.AcceptTcpClientAsync();

                if(tcpClient != null) 
                {
                    var networkStream = tcpClient.GetStream();
                    var sessionInf = new SessionInformation();
                    udpClient = new UdpClient(8000);

                    sessionInf.Id = temporaryId;
                    sessionInf.TcpClient = tcpClient;
                    sessionInf.NetworkStream = networkStream;
                    sessionInf.MessageStorage = new List<byte[]>();

                    temporarySessionInformation.Add(sessionInf);

                    temporaryId++;
                }
            }
        }

        public void StopListen()
        {
            listeningFlag = false;
            tcpListener.Stop();
        }
        
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

        public void DetermineCommand(string message, int id)
        {
            if (message.Contains("/register"))
            {
                RegisterNewUser(message, id);
            }
            else if (message.Contains("/login"))
            {
                LoginUser(message);
            }
            
        }

        public void SendMessageUsers(byte[] data)
        {
            for(int i = 0; i< temporarySessionInformation.Count; i++)
            {
                temporarySessionInformation[i]
            }
        }

        public void RegisterNewUser(string message, int id)
        {
            string[] loginPassword = message.Split(' ');

            string login = loginPassword[1];
            string password = loginPassword[2];

            User user = new User(login, password, "normal");

            controllDataBase.Add();
        }

        public void LoginUser(string message)
        {

        }
    }

    public class UdpState
    {
        public UdpClient udpClient;
        public IPEndPoint ip;
        public int id;
    }
}
