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
            server.UdpReceiveAsync();
            //server.DetermineMessageType();
            //server.DisplayMessageAsync();
            //using (UsersContext db = new UsersContext())
            //{
            //    Console.WriteLine(db.Users.Single(x => x.Name == "blackbroke"));
            //}

            Console.ReadLine();
        }
    }
}
