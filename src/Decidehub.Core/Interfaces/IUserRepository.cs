using System.Collections.Generic;
using System.Threading.Tasks;
using Decidehub.Core.Entities;
using Decidehub.Core.Identity;
using Microsoft.AspNetCore.Identity;

namespace Decidehub.Core.Interfaces
{
    public interface IUserRepository
    {
        Task<IList<ApplicationUser>> GetUsers(string tenantId = null);
        Task<IList<ApplicationUser>> GetUsersWithImage(string tenantId = null);
        Task<IList<ApplicationRole>> GetUserRoles(string userId, string tenantId = null);
        Task AddUser(ApplicationUser user);
        Task AssignRoleToUser(IdentityUserRole<string> userRole);
        Task<ApplicationUser> GetUser(string userId, bool ignoreTenant = false);
        Task<ApplicationUser> GetUserByIdAndTenant(string userId, string tenant);
        Task<ApplicationUser> GetUserWithImage(string userId, bool ignoreTenant = false);
        Task EditUser(ApplicationUser user);
        Task DeleteUser(string userId);
        Task<int> GetVoterCount(string tenantId);
        Task<IList<ApplicationUser>> GetVoters(string tenantId = null);
        Task UpdateAuthorityPercents(Dictionary<string, decimal> scores, string tenantId);
        Task UpdateUserDetails(UserDetail userDetail, string tenantId = null);
        Task AssignRoleToUser(string roleName, string userId, string tenantId = null);
        Task<IEnumerable<string>> GetAvailableTenantIds();
        Task<ApplicationUser> GetUserByEmail(string email, string tenantId);
        Task<IEnumerable<ApplicationUser>> GetAllUsers(int? take);
        Task<bool> UserInRole(string userId, string role);
        Task SetGeneratedPassToken(string userId, string token, string tenant);
        Task SetUserLangPreference(string userId, string lang);
        Task<int> GetActiveUserCount();
        Task<int> GetAllUserCount();
        Task<List<ApplicationUser>> ListAdmins();

        Task RemoveRoleFromUser(string roleName, string userId, string tenantId = null);
    }
}