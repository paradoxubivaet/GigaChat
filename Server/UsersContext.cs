using Microsoft.EntityFrameworkCore;
using System;

namespace Server
{
    public class UsersContext : DbContext
    {
        public DbSet<User> Users { get; set; }

        public UsersContext() 
        {

        }

        //I don't understand 
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            //optionsBuilder.UseSqlite(@"Data Source=C:\GigaUsers.db");
            optionsBuilder.UseSqlServer(@"Server=HOME-PC\SQLEXPRESS;Database=GigaUsers;Trusted_Connection=True;");
        }
    }
}
