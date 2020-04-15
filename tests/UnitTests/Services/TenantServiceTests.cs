using System.Linq;
using System.Threading.Tasks;
using Decidehub.Core.Entities;
using Decidehub.Core.Identity;
using Decidehub.Core.Interfaces;
using Decidehub.Core.Services;
using Decidehub.Infrastructure.Data;
using Decidehub.Infrastructure.Data.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace UnitTests.Services
{
    public class TenantServiceTests
    {
        private readonly ApplicationDbContext _context;
        private readonly TenantsDbContext _tenantsDbContext;
        private readonly ITenantService _tenantService;
        public TenantServiceTests()
        {
            _context = Helpers.GetContext("test");
            _tenantsDbContext = Helpers.GetTenantContext();
            ITenantRepository tenantRepository = new EntityTenantRepository(_tenantsDbContext);
            IAsyncRepository<Setting> settingRepository = new EfRepository<Setting>(_context);
            ISettingService settingService = new SettingService(settingRepository);
            IAsyncRepository<ApplicationRole> roleRepository = new EfRepository<ApplicationRole>(_context);
            IRoleService roleService = new RoleService(roleRepository);
            _tenantService = new TenantService(tenantRepository, settingService, roleService);

        }
        [Fact]
        public async Task Should_Add_Tenant_Setting_And_AdminRole()
        {
            var tenant = new Tenant
            {
                Id = "test2",
                InActive = false,
                HostName = "test2.decidehub.com"
            };
            await _tenantService.AddTenant(tenant);
            var checkTenant = _tenantsDbContext.Tenants.FirstOrDefault(x => x.Id == tenant.Id);
            Assert.NotNull(checkTenant);
            var settings = _context.Settings.Where(x => x.TenantId == tenant.Id);
            Assert.True(settings.Any());
            var roles = _context.Roles.IgnoreQueryFilters().Where(x => x.TenantId == tenant.Id);
            Assert.True(roles.Count() == 1);
        } 
        [Fact]
        public async Task Should_Delete_Tenant()
        {
            var tenant = new Tenant
            {
                Id = "test2",
                InActive = false,
                HostName = "test2.decidehub.com"
            };
            _tenantsDbContext.Add(tenant);
            _tenantsDbContext.SaveChanges();
            await _tenantService.DeleteTenant(tenant.Id);
            Assert.False(_tenantsDbContext.Tenants.Any(x => x.Id == tenant.Id));
        }
        [Fact]
        public async Task Should_Get_Tenant()
        {
            var tenant = new Tenant
            {
                Id = "test2",
                InActive = false,
                HostName = "test2.decidehub.com"
            };
            _tenantsDbContext.Add(tenant);
            _tenantsDbContext.SaveChanges();
            var getTenant= await _tenantService.GetTenant(tenant.Id);
            Assert.Equal(tenant.Id, getTenant.Id);
        }

        [Fact]
        public async Task Should_Get_Tenant_Count()
        {
            var tenant = new Tenant
            {
                Id = "test2",
                InActive = false,
                HostName = "test2.decidehub.com"
            };
            var tenant2 = new Tenant
            {
                Id = "test3",
                InActive = false,
                HostName = "test3.decidehub.com"
            };
            _tenantsDbContext.Add(tenant);
            _tenantsDbContext.Add(tenant2);
            _tenantsDbContext.SaveChanges();
            var count = await _tenantService.GetTenantCount();
            Assert.Equal(2, count);
            
        }
        [Fact]
        public async Task Should_Get_Tenant_Lang()
        {
            var tenant = new Tenant
            {
                Id = "test2",
                InActive = false,
                HostName = "test2.decidehub.com",
                Lang="en"
            };
            _tenantsDbContext.Add(tenant);
            _tenantsDbContext.SaveChanges();
            var lang = await _tenantService.GetTenantLang(tenant.Id);
            Assert.Equal(tenant.Lang, lang);
        }
        [Fact]
        public async Task Should_Get_Tenants()
        {
            var tenant = new Tenant
            {
                Id = "test2",
                InActive = false,
                HostName = "test2.decidehub.com"
            };
            var tenant2 = new Tenant
            {
                Id = "test3",
                InActive = false,
                HostName = "test3.decidehub.com"
            };
            _tenantsDbContext.Add(tenant);
            _tenantsDbContext.Add(tenant2);
            _tenantsDbContext.SaveChanges();
            var tenants = await _tenantService.GetTenants();
            Assert.Equal(2, tenants.Count());

        }
        [Fact]
        public async Task Should_Get_TenantWithIgnoredQueries()
        {
            var tenant = new Tenant
            {
                Id = "test2",
                InActive = true,
                HostName = "test2.decidehub.com",
                Lang = "en"
            };
            _tenantsDbContext.Add(tenant);
            _tenantsDbContext.SaveChanges();
            var getTenant = await _tenantService.GetTenantWithIgnoredQueries(tenant.Id);
            Assert.NotNull(getTenant);
        }
    }
}
