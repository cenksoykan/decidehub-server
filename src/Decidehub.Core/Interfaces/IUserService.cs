using System.Collections.Generic;
using System.Threading.Tasks;
using Decidehub.Core.Entities;
using Decidehub.Core.Identity;
using Decidehub.Core.Models;
using Microsoft.AspNetCore.Identity;

namespace Decidehub.Core.Interfaces
{
    public interface IUserService
    {
        Task<IList<ApplicationUser>> GetUsersAsync(string tenantId = null);
        Task<IList<ApplicationUser>> GetUsersWithImage(string tenantId = null);
        Task<IEnumerable<ApplicationUser>> GetAllUsers(int? take);
        Task<IList<ApplicationRole>> GetUserRoles(string userId, string tenantId = null);
        Task<ApplicationUser> GetUserByIdAndTenant(string id, string tenant);

        Task<ApplicationUser> AddUser(ApplicationUser user, IEnumerable<IdentityUserRole<string>> roles);

        Task<ApplicationUser> GetUserById(string userId, bool ignoreTenant = false);
        Task<ApplicationUser> GetUserWithImageById(string userId, bool ignoreTenant = false);
        Task EditUser(ApplicationUser user);
        Task DeleteUser(string userId);
        Task SendEmailToAllUsers(string tenantId, Dictionary<string, EmailDetailModel> langDic);
        Task SendEmailToVoters(string tenantId, Dictionary<string, EmailDetailModel> langDic);
        Task<int> GetVoterCount(string tenantId = null);
        Task UpdateAuthorityPercents(Dictionary<string, decimal> scores, string tenantId);
        Task UpdateUserDetail(UserDetail userDetail, string tenantId);
        Task AssignRoleToUser(string userId, string roleName, string tenantId = null);
        Task<bool> IsVoter(string userId);
        Task<ApplicationUser> GetUserByEmail(string email, string tenantId = null);
        Task<bool> UserInRole(string userId, string role);
        Task<IList<ApplicationUser>> GetVoters(string tenant);
        Task SetGeneratedPassToken(string userId, string token, string tenant);
        Task SetUserLangPreference(string userId, string lang);
        Task<int> GetActiveUserCount();
        Task<int> GetAllUserCount();
        Task DeleteUsers();
        Task<List<ApplicationUser>> ListAdmins();
        Task<decimal> GetMaxInitialAuthorityPercent();
        Task RemoveUserFromRole(string userId, string roleName, string tenantId = null);
    }
}