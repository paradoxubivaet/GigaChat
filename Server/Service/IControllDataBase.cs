﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    internal interface IControllDataBase
    {
        public void Add(User user);
        public void Delete(User user);
        public bool CheckingUser(string login, string password);
        public User GetUser(string login, string password);
    }
}