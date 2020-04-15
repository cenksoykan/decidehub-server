using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Decidehub.Core.Entities;
using Decidehub.Core.Identity;
using Decidehub.Core.Interfaces;
using Decidehub.Core.Models;
using Microsoft.AspNetCore.Identity;
using Serilog;

namespace Decidehub.Core.Services
{
    public class UserService : IUserService
    {
        private readonly IEmailSender _emailSender;
        private readonly IUserRepository _userRepository;

        public UserService(IUserRepository userRepository, IEmailSender emailSender)
        {
            _userRepository = userRepository;
            _emailSender = emailSender;
        }

        public async Task<ApplicationUser> AddUser(ApplicationUser user, IEnumerable<IdentityUserRole<string>> roles)
        {
            await _userRepository.AddUser(user);
            foreach (var role in roles)
                await _userRepository.AssignRoleToUser(new IdentityUserRole<string>
                    {RoleId = role.RoleId, UserId = user.Id});

            return user;
        }

        public async Task<ApplicationUser> GetUserById(string userId, bool ignoreTenant = false)
        {
            return await _userRepository.GetUser(userId, ignoreTenant);
        }

        public async Task<ApplicationUser> GetUserWithImageById(string userId, bool ignoreTenant = false)
        {
            return await _userRepository.GetUserWithImage(userId, ignoreTenant);
        }

        public async Task<IEnumerable<ApplicationUser>> GetAllUsers(int? takeLast)
        {
            return await _userRepository.GetAllUsers(takeLast);
        }

        public async Task<IList<ApplicationRole>> GetUserRoles(string userId, string tenantId = null)
        {
            return await _userRepository.GetUserRoles(userId, tenantId);
        }

        public async Task<IList<ApplicationUser>> GetUsersAsync(string tenantId = null)
        {
            return await _userRepository.GetUsers(tenantId);
        }

        public async Task<IList<ApplicationUser>> GetUsersWithImage(string tenantId = null)
        {
            return await _userRepository.GetUsersWithImage(tenantId);
        }

        public async Task EditUser(ApplicationUser user)
        {
            await _userRepository.EditUser(user);
        }

        public async Task DeleteUser(string userId)
        {
            await RecalculateAuthorityPercentOfUsers(userId);
            await _userRepository.DeleteUser(userId);
        }

        public async Task<int> GetVoterCount(string tenantId)
        {
            return await _userRepository.GetVoterCount(tenantId);
        }

        public async Task SendEmailToAllUsers(string tenantId, Dictionary<string, EmailDetailModel> langDic)
        {
            var users = await GetUsersAsync(tenantId);

            foreach (var user in users.Where(u => u.EmailConfirmed))
            {
                var lang = user.UserDetail.LanguagePreference ?? "tr";
                var langInfo = langDic[lang];
                Log.Logger.ForContext("UserEmail",
                        new {tenantId, subject = langInfo.Subject, message = langInfo.Message, email = user.Email})
                    .Error("UserEmail", user);

                await _emailSender.SendEmailAsync(user.Email, langInfo.Subject, langInfo.Message, tenantId);
            }
        }

        public async Task UpdateAuthorityPercents(Dictionary<string, decimal> scores, string tenantId)
        {
            await _userRepository.UpdateAuthorityPercents(scores, tenantId);
        }


        public async Task UpdateUserDetail(UserDetail userDetail, string tenantId)
        {
            await _userRepository.UpdateUserDetails(userDetail, tenantId);
        }

        public async Task AssignRoleToUser(string userId, string roleName, string tenantId)
        {
            await _userRepository.AssignRoleToUser(roleName, userId, tenantId);
        }

        public async Task RemoveUserFromRole(string userId, string roleName, string tenantId = null)
        {
            await _userRepository.RemoveRoleFromUser(roleName, userId, tenantId);
        }

        public async Task<bool> IsVoter(string userId)
        {
            var result = false;
            var user = await GetUserById(userId);
            if (user != null)
                result = user.UserDetail.AuthorityPercent > 0;
            return result;
        }

        public async Task<ApplicationUser> GetUserByEmail(string email, string tenantId)
        {
            return await _userRepository.GetUserByEmail(email, tenantId);
        }

        public async Task SendEmailToVoters(string tenantId, Dictionary<string, EmailDetailModel> langDic)
        {
            var users = await _userRepository.GetVoters(tenantId);

            foreach (var user in users)
            {
                var lang = user.UserDetail.LanguagePreference ?? "tr";
                var langInfo = langDic[lang];
                Log.Logger.ForContext("UserEmail",
                        new {tenantId, subject = langInfo.Subject, message = langInfo.Message, email = user.Email})
                    .Error("UserEmail", user);
                await _emailSender.SendEmailAsync(user.Email, langInfo.Subject, langInfo.Message, tenantId);
            }
        }

        public Task<ApplicationUser> GetUserByIdAndTenant(string id, string tenant)
        {
            return _userRepository.GetUserByIdAndTenant(id, tenant);
        }

        public async Task<bool> UserInRole(string userId, string role)
        {
            return await _userRepository.UserInRole(userId, role);
        }

        public async Task<IList<ApplicationUser>> GetVoters(string tenant)
        {
            return await _userRepository.GetVoters(tenant);
        }

        public async Task SetGeneratedPassToken(string userId, string token, string tenant)

        {
            await _userRepository.SetGeneratedPassToken(userId, token, tenant);
        }

        public async Task SetUserLangPreference(string userId, string lang)
        {
            await _userRepository.SetUserLangPreference(userId, lang);
        }

        public async Task<int> GetActiveUserCount()
        {
            return await _userRepository.GetActiveUserCount();
        }

        public async Task<int> GetAllUserCount()
        {
            return await _userRepository.GetAllUserCount();
        }

        public async Task DeleteUsers()
        {
            var users = await GetUsersAsync();
            foreach (var user in users)
                await DeleteUser(user.Id);
        }

        public async Task<List<ApplicationUser>> ListAdmins()
        {
            return await _userRepository.ListAdmins();
        }

        public async Task<decimal> GetMaxInitialAuthorityPercent()
        {
            var allUsers = await _userRepository.GetUsers();

            var initialAuthorityScores = allUsers.Select(u => u.UserDetail.InitialAuthorityPercent).ToList();

            if (initialAuthorityScores.All(s => s <= 0))
                return 1M / initialAuthorityScores.Count;

            return initialAuthorityScores.Max() / initialAuthorityScores.Sum();
        }

        private async Task RecalculateAuthorityPercentOfUsers(string userId)
        {
            var getUser = await _userRepository.GetUser(userId);
            if (getUser != null)
            {
                var allUsersExceptCurrent = (await _userRepository.GetUsers()).Where(u => u.Id != userId).ToList();
                var totalSum = allUsersExceptCurrent.Sum(s => s.UserDetail.AuthorityPercent);
                if (totalSum != 0)
                {
                    foreach (var user in allUsersExceptCurrent)
                    {
                        user.UserDetail.AuthorityPercent = 100 * user.UserDetail.AuthorityPercent / totalSum;
                        await _userRepository.UpdateUserDetails(user.UserDetail);
                    }

                    getUser.UserDetail.AuthorityPercent = 0;
                    await _userRepository.UpdateUserDetails(getUser.UserDetail);
                }
            }
        }
    }
}