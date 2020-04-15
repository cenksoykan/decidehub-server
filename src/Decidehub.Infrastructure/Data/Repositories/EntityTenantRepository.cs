using System.Collections.Generic;
using System.Threading.Tasks;
using Decidehub.Core.Entities;
using Decidehub.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Decidehub.Infrastructure.Data.Repositories
{
    public class EntityTenantRepository : ITenantRepository
    {
        private readonly TenantsDbContext _db;


        public EntityTenantRepository(TenantsDbContext db)
        {
            _db = db;            
        }

        public async Task<Tenant> AddTenant(Tenant tenant)
        {
            _db.Tenants.Add(tenant);
            await _db.SaveChangesAsync();
            return tenant;
        }

        public async Task DeleteTenant(string tenantId)
        {
            var getTenant = await _db.Tenants.FirstOrDefaultAsync(x => x.Id == tenantId);
            if (getTenant != null)
            {
                getTenant.InActive = true;
                await _db.SaveChangesAsync();
            }
        }

        public async Task<Tenant> GetTenant(string id)
        {
            return await _db.Tenants.FirstOrDefaultAsync(x => x.Id.ToLower() == id.ToLower());
        }
        public async Task<int> GetTenantCount()
        {
            return await _db.Tenants.CountAsync();
        }

        public async Task<IEnumerable<Tenant>> GetTenants()
        {
            return await _db.Tenants.ToListAsync();
        }

        public async Task<Tenant> GetTenantWithIgnoredQueries(string id)
        {
            return await _db.Tenants.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id.ToLower() == id.ToLower());
        }
    }
}