using System.Collections.Generic;
using System.Threading.Tasks;
using Decidehub.Core.Identity;

namespace Decidehub.Core.Interfaces
{
    public interface IRoleService
    {
        Task<IList<ApplicationRole>> GetRolesAsync();
        Task<ApplicationRole> AddRole(string name, string tenantId);
        Task<ApplicationRole> GetRoleByName(string name, string tenantId);
    }
}