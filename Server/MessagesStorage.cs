using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    public class MessagesStorage
    {
        public int Id { get; set; }
        public IPAddress Ip {get; set; }
        public List<byte[]> Messages { get; set; } = new List<byte[]>();
    }
}
