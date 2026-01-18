using Microsoft.EntityFrameworkCore;
using MyThuatShop.Models;
using System.Collections.Generic;

namespace MyThuatShop.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Category> Categories => Set<Category>();
    }
}
