using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    public class ObservableMessageStore
    {
        public int Id { get; set; }
        public IPAddress Ip { get; set; }
        public ObservableCollection<byte[]> Messages { get; set; } = new ObservableCollection<byte[]>();
    }
}
