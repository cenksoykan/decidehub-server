using System.Linq;
using Decidehub.Core.Entities;
using Decidehub.Core.Enums;
using Decidehub.Core.Identity;
using Microsoft.EntityFrameworkCore;

namespace Decidehub.Infrastructure.Data
{
    public static class DbInitializer
    {
        public static void Initialize(ApplicationDbContext context, TenantsDbContext tenantsDbContext)
        {
            AddSettings(context, null);
            AddRoles(context, null);
            var tenant = tenantsDbContext.Tenants.FirstOrDefault(x => x.Id == "");
            if (tenant == null)
            {
                tenant = new Tenant {Id = "", HostName = "mngly.com"};
                tenantsDbContext.Add(tenant);
                tenantsDbContext.SaveChanges();
            }

            var adminTenant = tenantsDbContext.Tenants.FirstOrDefault(x => x.Id == "admin");
            if (adminTenant == null)
            {
                adminTenant = new Tenant {Id = "admin", HostName = "admin.decidehub.com"};
                tenantsDbContext.Add(adminTenant);
                tenantsDbContext.SaveChanges();
            }
        }

        private static void AddSettings(ApplicationDbContext context, string tenant)
        {
            var settings = new[]
            {
                new Setting
                {
                    Key = Settings.VotingFrequency.ToString(),
                    Value = "90",
                    IsVisible = true,
                    TenantId = tenant
                },
                new Setting
                {
                    Key = Settings.AuthorityVotingRequiredUserPercentage.ToString(),
                    Value = "50",
                    IsVisible = true,
                    TenantId = tenant
                },
                new Setting
                {
                    Key = Settings.VotingDuration.ToString(),
                    Value = "24",
                    IsVisible = true,
                    TenantId = tenant
                }
            };
            foreach (var setting in settings)
            {
                if (!context.Settings.IgnoreQueryFilters().Any(x => x.Key == setting.Key && x.TenantId == tenant))
                {
                    context.Settings.Add(setting);
                }
            }

            context.SaveChanges();
        }

        private static void AddRoles(ApplicationDbContext context, string tenant)
        {
            var predefinedRoles = new[]
            {
                new ApplicationRole {Name = "Admin", TenantId = tenant}
            };
            foreach (var role in predefinedRoles)
            {
                if (!context.Roles.IgnoreQueryFilters().Any(x => x.Name == role.Name && x.TenantId == tenant))
                {
                    context.Roles.Add(role);
                }
            }

            context.SaveChanges();
        }
    }
}