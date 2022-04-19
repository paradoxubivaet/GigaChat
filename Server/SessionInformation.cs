using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    public class SessionInformation
    {
        public int Id { get; set; }
        public User? User { get; set; }
        public UdpClient UdpClient { get; set; }
        public TcpClient TcpClient { get; set; } 
        public NetworkStream NetworkStream { get; set; }
        public List<byte[]> MessageStorage { get; set; }
    }
}
