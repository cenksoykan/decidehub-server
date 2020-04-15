using System.Collections.Generic;
using System.Threading.Tasks;
using Decidehub.Core.Entities;

namespace Decidehub.Core.Interfaces
{
    public interface ITenantRepository
    {
        Task<IEnumerable<Tenant>> GetTenants();
        Task<Tenant> AddTenant(Tenant tenant);
        Task<Tenant> GetTenant(string id);
        Task<Tenant> GetTenantWithIgnoredQueries(string id);
        Task<int> GetTenantCount();
        Task DeleteTenant(string tenantId);
    }
}