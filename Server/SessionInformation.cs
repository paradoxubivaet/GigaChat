using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    public class SessionInformation
    {
        public int Id { get; set; }
        public User? User { get; set; }
        // Autorized/Nonauthorized 
        public string Status { get; set; }
        public UdpClient UdpClient { get; set; }
        public IPAddress Address { get; set; }
        public TcpClient TcpClient { get; set; } 
        public NetworkStream NetworkStream { get; set; }
        public ObservableCollection<byte[]> MessageStorage { get; set; }

        public void CloseConnect()
        {
            UdpClient.Dispose();
        }
    }
}
