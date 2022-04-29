using System;
using System.Linq;
using System.Threading.Tasks;

namespace Server
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Server server = new Server("127.0.0.1", "5000");
            server.CreateTclListener();

            server.StartServer();

            server.StartListenAsync();
            server.UdpReceiveAsyncSecond();

            Console.ReadLine();
        }
    }
}
