using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    public class User
    {
        public int UserId { get; set; }
        public string AccessLevel { get; set;}
        public string? Nickname { get; set; }
        public string Name { get; set; }
        public string Password { get; set; }

        // Banned, normal, muted.
        public string Status { get; set; }

        public User(string name, string password, string status)
        {
            Name = name;
            Password = password;
            Status = status;
        }
    }
}
