using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Service
{
    internal class ControllDataBase : IControllDataBase
    {
        public void Add(User user)
        {
            using (UsersContext db = new UsersContext())
            {
                db.Users.Add(user);
            }
        }

        public void Delete(User user)
        {
            using (UsersContext db = new UsersContext())
            {
                db.Remove(user);
            }
        }

        public bool CheckingUser(string login, string password)
        {
            using (UsersContext db = new UsersContext())
            {
                bool loginExisting = db.Users.Any(n => n.Name == login);
                bool passwordExisting = db.Users.Any(s => s.Password == password);

                return (loginExisting && passwordExisting);
            }
        }

        public User GetUser(string login, string password)
        {
            using (UsersContext db = new UsersContext())
            {
                return db.Users.First(n => n.Name == login && n.Password == password);
            }
        }
    }
}
