using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Client
{
    public class Client
    {
        private TcpClient tcpClient;
        private UdpClient udpClient;

        private NetworkStream stream;
        public Client(string serverIp, string port)
        {   
            udpClient = new UdpClient(serverIp, 8000);
            tcpClient = new TcpClient(serverIp, Int32.Parse(port)); 

            stream = tcpClient.GetStream();
        }

        // Этот метод должен выполняться асинхронно
        public void SendUdpMessage(byte[] data)
        {
            udpClient.Send(data, data.Length);
        }

        // Этот метод должен выполняться асинхронно
        public byte[] ReceiveTcpMessage()
        {
            byte[] receivedData = null;
            stream.Read(receivedData);

            return receivedData;
        }

        // Этот метод должен выполняться асинхронно 
        public byte[] ConverterToBit(string message)
        {
            byte[] data = Encoding.UTF8.GetBytes(message);

            return data;
        }
    }
}
