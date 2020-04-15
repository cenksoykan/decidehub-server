using Decidehub.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace Decidehub.Infrastructure.Data
{
    public class TenantsDbContext : DbContext
    {
        public TenantsDbContext(DbContextOptions<TenantsDbContext> options) : base(options)
        {
        }

        public DbSet<Tenant> Tenants { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Tenant>().HasKey(c => new {c.Id, c.HostName});
            modelBuilder.Entity<Tenant>().HasQueryFilter(b => !b.InActive);
        }
    }
}