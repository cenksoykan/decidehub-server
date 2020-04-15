using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Decidehub.Core.Entities;
using Decidehub.Core.Identity;
using Decidehub.Core.Interfaces;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Decidehub.Infrastructure.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, string>
    {
        private readonly string _tenantId;

        public ApplicationDbContext(DbContextOptions options, ITenantProvider tenantProvider) : base(options)
        {
            _tenantId = tenantProvider.GetTenantId();
        }

        public DbSet<Setting> Settings { get; set; }
        public DbSet<Vote> Votes { get; set; }
        public DbSet<Poll> Polls { get; set; }
        public DbSet<AuthorityPoll> AuthorityPolls { get; set; }
        public DbSet<MultipleChoicePoll> MultipleChoicePolls { get; set; }
        public DbSet<PolicyChangePoll> PolicyChangePolls { get; set; }

        public DbSet<Policy> Policies { get; set; }
        public DbSet<UserDetail> UserDetails { get; set; }
        public DbSet<PollSetting> PollSetting { get; set; }
        public DbSet<SharePoll> SharePolls { get; set; }
        public DbSet<Contact> Contact { get; set; }
        public DbSet<UserImage> UserImages { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            builder.Entity<Policy>().HasQueryFilter(b => EF.Property<string>(b, "TenantId") == _tenantId);
            builder.Entity<Vote>().HasQueryFilter(b => EF.Property<string>(b, "TenantId") == _tenantId);
            builder.Entity<Poll>().HasQueryFilter(b => EF.Property<string>(b, "TenantId") == _tenantId);
            builder.Entity<UserDetail>().HasQueryFilter(b => EF.Property<string>(b, "TenantId") == _tenantId);
            builder.Entity<PollSetting>().HasQueryFilter(b => EF.Property<string>(b, "TenantId") == _tenantId);
            builder.Entity<Contact>().HasQueryFilter(b => EF.Property<string>(b, "TenantId") == _tenantId);
            builder.Entity<ApplicationRole>().HasQueryFilter(b => EF.Property<string>(b, "TenantId") == _tenantId);
            builder.Entity<ApplicationUser>()
                .HasQueryFilter(b => EF.Property<string>(b, "TenantId") == _tenantId && !b.IsDeleted);

            builder.Entity<ApplicationUser>().HasIndex(p => p.NormalizedUserName).IsUnique(false);
            builder.Entity<ApplicationUser>().Property(b => b.CreatedAt).HasDefaultValueSql("now() at time zone 'utc'");
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            ChangeTracker.DetectChanges();

            foreach (var item in ChangeTracker.Entries()
                .Where(e => e.State == EntityState.Added && e.Metadata.GetProperties().Any(p => p.Name == "TenantId")))
                if (item.CurrentValues["TenantId"] == null)
                    item.CurrentValues["TenantId"] = _tenantId;

            return await base.SaveChangesAsync(cancellationToken);
        }

        public override int SaveChanges()
        {
            ChangeTracker.DetectChanges();

            foreach (var item in ChangeTracker.Entries()
                .Where(e => e.State == EntityState.Added && e.Metadata.GetProperties().Any(p => p.Name == "TenantId")))
                if (item.CurrentValues["TenantId"] == null)
                    item.CurrentValues["TenantId"] = _tenantId;

            return base.SaveChanges();
        }
    }
}