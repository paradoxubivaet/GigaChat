using System;
using System.Threading.Tasks;

namespace Client
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Start client!");

            Client client = new Client("127.0.0.1", "5000");

            client.SendMessagesFromStorageAsync();

            while (true)
            {
                client.WaitingInput();
            }

            Console.WriteLine("Передача окончена");
            Console.ReadLine();
        }
    }
}
