using Microsoft.EntityFrameworkCore;
using System;

namespace Server
{
    internal class UsersContext : DbContext
    {
        public DbSet<User> Users { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite(@"Data Source=GigaChat.db");
        }
    }
}
