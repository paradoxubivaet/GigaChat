using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    internal interface IAccessLevel
    {
        public void Send(string message);
        public void Login(string nickname, string password);
        public void Register(string nickname, string password);
    }
}
