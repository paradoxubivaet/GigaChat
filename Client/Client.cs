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

        private bool waitingInput;

        private List<string> messagesStorage;
        public Client(string serverIp, string port)
        {   
            udpClient = new UdpClient(serverIp, 8000);
            tcpClient = new TcpClient(serverIp, Int32.Parse(port)); 

            stream = tcpClient.GetStream();

            waitingInput = true;

            messagesStorage=new List<string>();
        }

        public void WaitingInput()
        {
            while (waitingInput)
            {
                string message = Console.ReadLine();

                messagesStorage.Add(message);
            }
        }

        public async Task SendMessagesFromStorageAsync()
        {
            await Task.Run(() =>
            {
                while (true)
                {
                    if (messagesStorage.Count != 0)
                    {
                        for (int i = 0; i < messagesStorage.Count; i++)
                        {
                            var message = ConvertToBit(messagesStorage[i]);
                            SendUdpMessage(message);
                            messagesStorage.RemoveAt(i);
                        }
                    }
                }
            });
        }

        // Этот метод должен выполняться асинхронно
        public void SendUdpMessage(byte[] data)
        {
            udpClient.Send(data, data.Length);
        }

        public async Task<byte[]> ReceiveUdpMessage()
        {
            var receivedData = await udpClient.ReceiveAsync();
            byte[] data = receivedData.Buffer;

            return data;
        }

        // Этот метод должен выполняться асинхронно
        public async Task<byte[]> ReceiveTcpMessage()
        {
            byte[] receivedData = null;
            await stream.ReadAsync(receivedData);

            return receivedData;
        }

        // Этот метод должен выполняться асинхронно 
        public byte[] ConvertToBit(string message)
        {
            byte[] data = Encoding.UTF8.GetBytes(message);

            return data;
        }
        public string ConvertToString(byte[] data)
        {
            return Encoding.UTF8.GetString(data);
        }

        public Task DisplayMessage(string message)
        {
            Task t = new Task(() =>
            {
                Console.WriteLine(message);
            });

            return t;
        }
    }
}
