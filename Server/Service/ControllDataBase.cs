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
                db.SaveChanges();
            }
        }

        public void Delete(User user)
        {
            using (UsersContext db = new UsersContext())
            {
                db.Users.Remove(user);
                db.SaveChanges();
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

        public bool CheckingUser(string login)
        {
            using (UsersContext db = new UsersContext())
            {
                bool result = db.Users.Any(n => n.Name == login);
                return result;
            }
        }

        public User GetUser(string login, string password)
        {
            using (UsersContext db = new UsersContext())
            {
                return db.Users.First(n => n.Name == login && n.Password == password);
            }
        }

        public void SetUserStatus(string login, string status)
        {
            using (UsersContext db = new UsersContext())
            {
                db.Users.First(x => x.Name == login).Status = status;
                db.SaveChanges();
            }
        }

        public string GetUserStatus(string login)
        {
            using(UsersContext db = new UsersContext())
            {
                return db.Users.First(x => x.Name == login).Status;
            }
        }
    }
}
