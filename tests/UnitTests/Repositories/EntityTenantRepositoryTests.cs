using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Decidehub.Core.Entities;
using Decidehub.Infrastructure.Data.Repositories;
using Xunit;

namespace UnitTests.Repositories
{
    public class EntityTenantRepositoryTests
    {
        [Fact]
        public async Task Should_Add_Tenant()
        {
            var context = Helpers.GetTenantContext();
            var tenantRepository = new EntityTenantRepository(context);
            var tenant = new Tenant
            {
                Id = "test",
                HostName = "test.decidehub.com"
            };
            await tenantRepository.AddTenant(tenant);
            var tenants = context.Tenants;
            Assert.True(tenants.Count() > 0);
        }

        [Fact]
        public async Task Should_Delete_Tenant()
        {
            var context = Helpers.GetTenantContext();
            var tenantRepository = new EntityTenantRepository(context);
            var tenant = new Tenant
            {
                Id = "test",
                HostName = "test.decidehub.com"
            };
            context.Tenants.Add(tenant);
            context.SaveChanges();
            await tenantRepository.DeleteTenant(tenant.Id);
            var getTenant = context.Tenants.FirstOrDefault(x => x.Id == tenant.Id);
            Assert.Null(getTenant);
        }

        [Fact]
        public async Task Should_Get_Tenant_ById()
        {
            var context = Helpers.GetTenantContext();
            var tenantList = new List<Tenant>
            {
                new Tenant
                {
                    Id = "test",
                    HostName = "test.decidehub.com"
                },
                new Tenant
                {
                    Id = "test2",
                    HostName = "test2.decidehub.com"
                }
            };
            context.Tenants.AddRange(tenantList);
            context.SaveChanges();
            var tenantRepository = new EntityTenantRepository(context);
            var getTenant = await tenantRepository.GetTenant("test");
            Assert.NotNull(getTenant);
            Assert.Equal("test", getTenant.Id);
        }

        [Fact]
        public async Task Should_Get_Tenant_Count()
        {
            var context = Helpers.GetTenantContext();
            var tenantList = new List<Tenant>
            {
                new Tenant
                {
                    Id = "test",
                    HostName = "test.decidehub.com"
                },
                new Tenant
                {
                    Id = "test2",
                    HostName = "test2.decidehub.com"
                }
            };
            context.Tenants.AddRange(tenantList);
            context.SaveChanges();
            var tenantRepository = new EntityTenantRepository(context);
            var count = await tenantRepository.GetTenantCount();
            Assert.Equal(2, count);
        }

        [Fact]
        public async Task Should_Get_Tenants()
        {
            var context = Helpers.GetTenantContext();
            var tenantList = new List<Tenant>
            {
                new Tenant
                {
                    Id = "test",
                    HostName = "test.decidehub.com"
                },
                new Tenant
                {
                    Id = "test2",
                    HostName = "test2.decidehub.com"
                },
                new Tenant
                {
                    Id = "test3",
                    HostName = "test2.decidehub.com",
                    InActive = true
                }
            };
            context.Tenants.AddRange(tenantList);
            context.SaveChanges();
            var tenantRepository = new EntityTenantRepository(context);
            var tenants = await tenantRepository.GetTenants();
            Assert.Equal(2, tenants.Count());
        }

        [Fact]
        public async Task Should_Get_TenantWithIgnoredQueries()
        {
            var context = Helpers.GetTenantContext();
            var tenantList = new List<Tenant>
            {
                new Tenant
                {
                    Id = "test",
                    HostName = "test.decidehub.com"
                },
                new Tenant
                {
                    Id = "test2",
                    HostName = "test2.decidehub.com"
                },
                new Tenant
                {
                    Id = "test3",
                    HostName = "test2.decidehub.com",
                    InActive = true
                }
            };
            context.Tenants.AddRange(tenantList);
            context.SaveChanges();
            var tenantRepository = new EntityTenantRepository(context);
            var tenant = await tenantRepository.GetTenantWithIgnoredQueries("test3");
            Assert.True(tenant.InActive);
        }
    }
}