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

namespace Client
{
    public class Client
    {
        private TcpClient tcpClient;
        private UdpClient udpClient;

        private NetworkStream stream;

        private bool waitingInput;

        private List<string> messagesStorage;
        private List<string> messagesReceivedStorage;

        private ObservableCollection<string> messagesObservable = new ObservableCollection<string>();

        public Client(string serverIp, string port)
        {   
            udpClient = new UdpClient(7000);
            tcpClient = new TcpClient(serverIp, Int32.Parse(port)); 

            stream = tcpClient.GetStream();

            waitingInput = true;

            messagesStorage=new List<string>();
            messagesReceivedStorage=new List<string>();

            messagesObservable.CollectionChanged += messagesObservable_CollectionChanged;
        }

        public async Task WaitingInput()
        {
            await Task.Run(() =>
            {
                while (waitingInput)
                {
                    string message = Console.ReadLine();

                    messagesStorage.Add(message);

                    Thread.Sleep(1);
                }
            });
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
            IPEndPoint endpoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 8000);
            udpClient.Send(data, data.Length, endpoint);
        }

        public async Task ReceiveUdpMessage()
        {
            await Task.Run(async () =>
            {
                while (true)
                {
                    var receivedData = await udpClient.ReceiveAsync();
                    byte[] data = receivedData.Buffer;
                    if (data.Length != 0)
                    {
                        messagesObservable.Add(Encoding.UTF8.GetString(data));
                    }
                }
            });
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

        public void messagesObservable_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if(e.Action == NotifyCollectionChangedAction.Add)
            {
                var s = e.NewItems[0];
                Console.WriteLine(s);
            }
                
        }

        public async Task DisplayMessage()
        {
            await Task.Run(() => 
            {
                while (true)
                {
                    if (messagesReceivedStorage.Count != 0)
                    {
                        for (int i = 0; i < messagesReceivedStorage.Count; i++)
                        {
                            Console.WriteLine(messagesReceivedStorage[i]);

                            messagesReceivedStorage.RemoveAt(i);
                        }
                    }
                }
            });
        }
    }
}
