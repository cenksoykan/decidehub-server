using System.Collections.Generic;
using System.Threading.Tasks;
using Decidehub.Core.Entities;

namespace Decidehub.Core.Interfaces
{
    public interface ITenantService
    {
        Task<IEnumerable<Tenant>> GetTenants();
        Task<Tenant> AddTenant(Tenant tenant);
        Task<Tenant> GetTenant(string id);
        Task<string> GetTenantLang(string id);
        Task<int> GetTenantCount();
        Task DeleteTenant(string tenantId);
        Task<Tenant> GetTenantWithIgnoredQueries(string id);
    }
}