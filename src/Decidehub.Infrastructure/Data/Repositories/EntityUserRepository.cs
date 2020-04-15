using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Decidehub.Core.Entities;
using Decidehub.Core.Identity;
using Decidehub.Core.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Decidehub.Infrastructure.Data.Repositories
{
    public class EntityUserRepository : IUserRepository
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;

        public EntityUserRepository(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        public async Task<IList<ApplicationUser>> GetUsers(string tenantId = null)
        {
            if (tenantId != null)
                return await _db.Users.Include(x => x.UserDetail)
                    .IgnoreQueryFilters()
                    .Where(x => x.TenantId == tenantId && !x.IsDeleted)
                    .ToListAsync();
            return await _db.Users.Include(x => x.UserDetail).ToListAsync();
        }

        public async Task<IList<ApplicationUser>> GetUsersWithImage(string tenantId = null)
        {
            if (tenantId != null)
                return await _db.Users.Include(x => x.UserDetail)
                    .Include(x => x.UserImage)
                    .IgnoreQueryFilters()
                    .Where(x => x.TenantId == tenantId)
                    .ToListAsync();
            return await _db.Users.Include(x => x.UserDetail).Include(x => x.UserImage).ToListAsync();
        }

        public async Task<IList<ApplicationRole>> GetUserRoles(string userId, string tenantId = null)
        {
            var userRolesQuery = _db.UserRoles.Where(x => x.UserId == userId);
            var rolesQuery = _db.Roles.AsQueryable();
            if (tenantId != null)
            {
                userRolesQuery = userRolesQuery.IgnoreQueryFilters();
                rolesQuery = rolesQuery.IgnoreQueryFilters();
            }

            var userRoles = await userRolesQuery.Select(x => x.RoleId).ToListAsync();

            return await rolesQuery.Where(r => userRoles.Contains(r.Id)).ToListAsync();
        }

        public async Task AddUser(ApplicationUser user)
        {
            await _userManager.CreateAsync(user);
        }

        public async Task AssignRoleToUser(IdentityUserRole<string> userRole)
        {
            var userHasRole =
                await _db.UserRoles.AnyAsync(u => u.RoleId == userRole.RoleId && u.UserId == userRole.UserId);
            if (!userHasRole)
            {
                await _db.UserRoles.AddAsync(userRole);
                await _db.SaveChangesAsync();
            }
        }

        public async Task<ApplicationUser> GetUser(string id, bool ignoreTenant = false)
        {
            if (ignoreTenant)
                return await _db.Users.Include(x => x.UserDetail)
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted);

            return await _db.Users.Include(x => x.UserDetail).FirstOrDefaultAsync(u => u.Id == id);
        }

        public async Task<ApplicationUser> GetUserWithImage(string id, bool ignoreTenant = false)
        {
            if (ignoreTenant)
                return await _db.Users.Include(x => x.UserDetail)
                    .Include(x => x.UserImage)
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted);

            return await _db.Users.Include(x => x.UserDetail)
                .Include(x => x.UserImage)
                .FirstOrDefaultAsync(u => u.Id == id);
        }

        public async Task EditUser(ApplicationUser user)
        {
            await _userManager.UpdateAsync(user);
        }

        public async Task DeleteUser(string id)
        {
            var user = await GetUser(id);
            if (user != null) user.IsDeleted = true;

            await _db.SaveChangesAsync();
        }

        public async Task<int> GetVoterCount(string tenantId)
        {
            return await GetVotersQuery(tenantId).CountAsync();
        }

        public async Task UpdateAuthorityPercents(Dictionary<string, decimal> scores, string tenantId)
        {
            var allUsers = await _db.UserDetails.IgnoreQueryFilters().Where(u => u.TenantId == tenantId).ToListAsync();
            var finalSum = Math.Max(1, scores.Sum(s => s.Value));

            foreach (var user in allUsers)
                user.AuthorityPercent = scores.ContainsKey(user.UserId) ? scores[user.UserId] * 100.0M / finalSum : 0;

            await _db.SaveChangesAsync();
        }

        public async Task UpdateUserDetails(UserDetail userDetail, string tenantId = null)
        {
            UserDetail getUserDetail;
            if (tenantId != null)
                getUserDetail = await _db.UserDetails.IgnoreQueryFilters()
                    .FirstOrDefaultAsync(x => x.UserId == userDetail.UserId && x.TenantId == tenantId);
            else
                getUserDetail = await _db.UserDetails.FirstOrDefaultAsync(x => x.UserId == userDetail.UserId);
            if (getUserDetail != null)
            {
                getUserDetail.AuthorityPercent = userDetail.AuthorityPercent;
                getUserDetail.InitialAuthorityPercent = userDetail.InitialAuthorityPercent;
            }

            await _db.SaveChangesAsync();
        }

        public async Task AssignRoleToUser(string roleName, string userId, string tenantId)
        {
            ApplicationRole role;
            if (tenantId == null)
                role = await _db.Roles.FirstOrDefaultAsync(x => x.Name == roleName);
            else
                role = await _db.Roles.IgnoreQueryFilters()
                    .FirstOrDefaultAsync(x => x.Name == roleName && x.TenantId == tenantId);
            if (role != null)
                await AssignRoleToUser(new IdentityUserRole<string> {UserId = userId, RoleId = role.Id});
        }

        public async Task RemoveRoleFromUser(string roleName, string userId, string tenantId = null)
        {
            ApplicationRole role;
            if (tenantId == null)
                role = await _db.Roles.FirstOrDefaultAsync(x => x.Name == roleName);
            else
                role = await _db.Roles.IgnoreQueryFilters()
                    .FirstOrDefaultAsync(x => x.Name == roleName && x.TenantId == tenantId);
            if (role != null)
            {
                var userRole = await _db.UserRoles.FirstOrDefaultAsync(r => r.RoleId == role.Id && r.UserId == userId);
                if (userRole != null)
                {
                    _db.UserRoles.Remove(userRole);
                    await _db.SaveChangesAsync();
                }
            }
        }

        public async Task<IEnumerable<string>> GetAvailableTenantIds()
        {
            var tenants = await _db.Users.IgnoreQueryFilters().Select(x => x.TenantId).Distinct().ToListAsync();
            return tenants;
        }

        public async Task<ApplicationUser> GetUserByEmail(string email, string tenantId)
        {
            if (tenantId != null)
                return await _db.Users.FirstOrDefaultAsync(x => x.Email == email && x.TenantId == tenantId);
            return await _db.Users.FirstOrDefaultAsync(x => x.Email == email);
        }

        public async Task<IEnumerable<ApplicationUser>> GetAllUsers(int? takeLast)
        {
            var query = _db.Users.Include(x => x.UserDetail)
                .Include(x => x.UserImage)
                .IgnoreQueryFilters()
                .OrderByDescending(x => x.CreatedAt);
            if (takeLast.HasValue) return await query.Take(takeLast.Value).ToListAsync();

            return await query.ToListAsync();
        }

        public Task<ApplicationUser> GetUserByIdAndTenant(string id, string tenant)
        {
            if (tenant != null)
                return _db.Users.Include(x => x.UserDetail)
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted && u.TenantId == tenant);

            return _db.Users.Include(x => x.UserDetail).FirstOrDefaultAsync(u => u.Id == id);
        }

        public async Task<IList<ApplicationUser>> GetVoters(string tenantId = null)
        {
            return await GetVotersQuery(tenantId).ToListAsync();
        }


        public async Task<bool> UserInRole(string userId, string role)
        {
            var result = false;
            var getRole = await _db.Roles.FirstOrDefaultAsync(x => x.Name == role);
            if (getRole != null)
                result = await _db.UserRoles.AnyAsync(x => x.UserId == userId && x.RoleId == getRole.Id);

            return result;
        }

        public async Task SetGeneratedPassToken(string userId, string token, string tenant)

        {
            var getUser = await _db.Users.IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.Id == userId && x.TenantId == tenant && !x.IsDeleted);
            if (getUser != null) getUser.GeneratePassToken = token;
            await _db.SaveChangesAsync();
        }

        public async Task SetUserLangPreference(string userId, string lang)
        {
            var getUser = await GetUser(userId);
            if (getUser?.UserDetail != null) getUser.UserDetail.LanguagePreference = lang;
            await EditUser(getUser);
        }

        public async Task<int> GetActiveUserCount()
        {
            return await _db.Users.Where(x => x.EmailConfirmed).CountAsync();
        }

        public async Task<int> GetAllUserCount()
        {
            return await _db.Users.IgnoreQueryFilters().Where(x => !x.IsDeleted).CountAsync();
        }

        public async Task<List<ApplicationUser>> ListAdmins()
        {
            var query = from user in _db.Users
                join userRole in _db.UserRoles on user.Id equals userRole.UserId
                join role in _db.Roles on userRole.RoleId equals role.Id
                where role.Name == "Admin"
                select user;
            return await query.ToListAsync();
        }

        private IQueryable<ApplicationUser> GetVotersQuery(string tenantId)
        {
            if (tenantId != null)
                return _db.Users.Include(x => x.UserDetail)
                    .IgnoreQueryFilters()
                    .Where(x => x.UserDetail.AuthorityPercent > 0 && x.TenantId == tenantId);
            return _db.Users.Include(x => x.UserDetail).Where(x => x.UserDetail.AuthorityPercent > 0);
        }
    }
}