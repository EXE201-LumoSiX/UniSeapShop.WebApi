using Microsoft.EntityFrameworkCore;
using UniSeapShop.Domain.Entities;

namespace UniSeapShop.Domain
{
    public class UniSeapShopDBContext : DbContext
    {
        public UniSeapShopDBContext()
        {

        }

        public UniSeapShopDBContext(DbContextOptions<UniSeapShopDBContext> options) : base(options)
        {
        }
        public DbSet<User> Users { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

        }
    }
}
