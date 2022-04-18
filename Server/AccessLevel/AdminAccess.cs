using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.AccessLevel
{
    internal class AdminAccess : IAccessLevel
    {
        public void Login(string nickname, string password)
        {
            throw new NotImplementedException();
        }

        public void Register(string nickname, string password)
        {
            throw new NotImplementedException();
        }

        public void Send(string message)
        {
            throw new NotImplementedException();
        }
    }
}
