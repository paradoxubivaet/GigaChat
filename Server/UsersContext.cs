using Microsoft.EntityFrameworkCore;
using System;

namespace Server
{
    internal class UsersContext : DbContext
    {
        public DbSet<User> Users { get; set; }
    }
}
