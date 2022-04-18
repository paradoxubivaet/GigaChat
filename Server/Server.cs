using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Server
{

    internal class Server
    {
        private string ip;
        private string port;

        private TcpListener tcpListener;

        private IPEndPoint localEndPoint;

        private bool listeningFlag;

        // id tcp clients and tcp clients
        private Dictionary<int, TcpClient> tcpClients;

        // id tcp clients and network stream
        private Dictionary<int, NetworkStream> clientSteams;

        // id tcp clients and udp client
        private Dictionary<int, UdpClient> udpClients;

        // messages queue 
        private List<byte[]> messageQueue;
        private List<string> commandList = new List<string> { "/kick", "/ban", "/mute", "/unban", "/givenickname" };
        public Server(string ip, string port)
        {
            Ip= ip; 
            Port= port;

            localEndPoint = new IPEndPoint(IPAddress.Parse(ip), Int32.Parse(port));

            listeningFlag = false;

            tcpClients = new Dictionary<int, TcpClient>();
            clientSteams = new Dictionary<int, NetworkStream>();
            udpClients = new Dictionary<int, UdpClient>();
            messageQueue = new List<byte[]>();

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

            var id = 0;
            listeningFlag = true;
            while (listeningFlag)
            {
                var tcpClient = await tcpListener.AcceptTcpClientAsync();

                if(tcpClient != null) 
                {
                    var networkStream = tcpClient.GetStream();
                    var udpClient = await Task.Run(() => GetUdpClient(id));

                    tcpClients.Add(id, tcpClient);
                    clientSteams.Add(id, networkStream);
                    udpClients.Add(id, udpClient);

                    id++;
                }
            }
        }

        public void StopListen()
        {
            listeningFlag = false;
            tcpListener.Stop();
        }

        public UdpClient GetUdpClient(int id)
        {
            var remoteEndPoint = (IPEndPoint)tcpClients[id].Client.RemoteEndPoint;

            var clientIp = remoteEndPoint.Address.ToString();
            var udpBytePort = TcpReceive(id);
            int udpPort = BitConverter.ToInt32(udpBytePort);

            UdpClient udpClient = new UdpClient(clientIp, udpPort);
            return udpClient;
        }

        public byte[] TcpReceive(int id)
        {
            byte[] receivedData = new byte[128];
            clientSteams[id].Read(receivedData);

            return receivedData;
        }

        public void UdpSend(byte[] message)
        {
            foreach(var client in udpClients)
            {
                client.Value.Send(message, message.Length);
            }
        }

        // Receive UDP ->

        public void UdpReceive()
        {
            for (int i = 0; i < tcpClients.Count; i++)
            {
                UdpState udpState = new UdpState();
                udpState.Client = udpClients[i];
                udpState.IP = (IPEndPoint)udpClients[i].Client.RemoteEndPoint;
                udpClients[i].BeginReceive(new AsyncCallback(ReceiveCallback), udpState);
            }
        }

        public void ReceiveCallback(IAsyncResult ar)
        {
            UdpClient client = ((UdpState)(ar.AsyncState)).Client;
            IPEndPoint ipEndPoint = ((UdpState)(ar.AsyncState)).IP;
            byte[] message = client.EndReceive(ar,ref ipEndPoint);

            messageQueue.Add(message);
        }

        public struct UdpState
        {
            public UdpClient Client;
            public IPEndPoint IP;
        }

        // Receive UDP <-

        public void DetermineTypeMessage()
        {
            if(messageQueue.Count != 0)
            {
                for (int i = 0; i < messageQueue.Count; i++)
                {
                    var message = BitConverter.ToString(messageQueue[i]);

                    if (commandList.Any(s => message.Contains(s)))
                    {

                    }
                    else
                    {

                    }
                }
            }
        }

        public void DetermineNickname()
        {

        }

        public void CloseConnection()
        {

        }

        public void DisplayMessageInConsole(byte[] message, string nickname)
        {
            Console.WriteLine($"{DateTime.Now.ToShortTimeString()} |- {nickname} -| {BitConverter.ToString(message)}");
        }
    }
}
