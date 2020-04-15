using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Decidehub.Core.Entities;
using Decidehub.Core.Identity;
using Decidehub.Core.Interfaces;
using Decidehub.Core.Services;
using Decidehub.Infrastructure.Data;
using Decidehub.Infrastructure.Data.Repositories;
using Microsoft.AspNetCore.Identity;
using Moq;
using Xunit;

namespace UnitTests.Services
{
    public class UserServiceTests
    {
        public UserServiceTests()
        {
            _context = Helpers.GetContext("test");
            var user = new ApplicationUser
            {
                Id = 1.ToString(),
                Email = "test3@workhow.com",
                FirstName = "test3",
                LastName = "test3",
                CreatedAt = DateTime.UtcNow,
                SecurityStamp = new Guid().ToString(),
                EmailConfirmed = true,
                IsDeleted = false,
                UserDetail = new UserDetail {AuthorityPercent = 0, LanguagePreference = "tr"}
            };

            var list = new List<ApplicationUser>
            {
                user
            };

            _userManager = new Mock<FakeUserManager>();

            _userManager.Setup(x => x.Users).Returns(list.AsQueryable());
            _userManager.Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>()))
                .Callback((ApplicationUser usr) => list.Add(usr))
                .ReturnsAsync(IdentityResult.Success);
            _userManager.Setup(x => x.UpdateAsync(It.IsAny<ApplicationUser>()))
                .Callback((ApplicationUser usr) => list[list.FindIndex(x => x.Id == usr.Id)] = usr)
                .ReturnsAsync(IdentityResult.Success);
            IUserRepository userRepository = new EntityUserRepository(_context, _userManager.Object);
            _userService = new UserService(userRepository, null);
        }

        private readonly IUserService _userService;
        private readonly ApplicationDbContext _context;
        private readonly Mock<FakeUserManager> _userManager;

        [Fact]
        public async Task Should_AddUser_And_Assign_Role()
        {
            var user = new ApplicationUser
            {
                Id = Guid.NewGuid().ToString(),
                Email = "test3@workhow.com",
                FirstName = "test3",
                LastName = "test3",
                CreatedAt = DateTime.UtcNow,
                SecurityStamp = new Guid().ToString(),
                EmailConfirmed = true,
                IsDeleted = false,
                UserDetail = new UserDetail {AuthorityPercent = 0, LanguagePreference = "tr"}
            };
            _context.Users.Add(user);
            var role = new ApplicationRole
            {
                Id = Guid.NewGuid().ToString(), Name = "Admin", TenantId = "test"
            };
            _context.Roles.Add(role);
            _context.SaveChanges();
            await _userService.AddUser(user,
                new List<IdentityUserRole<string>> {new IdentityUserRole<string> {UserId = user.Id, RoleId = role.Id}});
            var addedUser = _userManager.Object.Users.FirstOrDefault(x => x.Id == user.Id);
            Assert.NotNull(addedUser);
            Assert.True(_context.UserRoles.Any(x => x.RoleId == role.Id && x.UserId == user.Id));
        }

        [Fact]
        public async Task Should_Assign_RoleToUser()
        {
            Helpers.GetContextAndUserRoleTestData(_context);
            await _userService.AssignRoleToUser(1.ToString(), "Test");
            Assert.NotNull(_context.UserRoles.SingleOrDefault(x => x.RoleId == "3" && x.UserId == "1"));
        }

        [Fact]
        public async Task Should_Check_IfTheUserIsVoter()
        {
            Helpers.GetContextAndUserTestData(_context);
            var isVoter = await _userService.IsVoter(1.ToString());
            var isVoter2 = await _userService.IsVoter(2.ToString());
            Assert.True(isVoter);
            Assert.False(isVoter2);
        }

        [Fact]
        public async Task Should_Check_User_Role()
        {
            Helpers.GetContextAndUserRoleTestData(_context);
            var result = await _userService.UserInRole(1.ToString(), "Admin");
            Assert.True(result);
        }

        [Fact]
        public async Task Should_Delete_User_And_Redistribute_AuthorityPoints()
        {
            Helpers.GetContextAndUserTestData(_context);
            var newUser = new ApplicationUser
            {
                Id = 4.ToString(),
                Email = "test2@workhow.com",
                FirstName = "test",
                LastName = "test",
                CreatedAt = DateTime.UtcNow,
                SecurityStamp = new Guid().ToString(),
                EmailConfirmed = true,
                IsDeleted = false,
                UserDetail = new UserDetail {AuthorityPercent = 20, LanguagePreference = "tr"}
            };
            _context.Users.Add(newUser);
            _context.SaveChanges();
            var userDetail1 = _context.UserDetails.FirstOrDefault(x => x.UserId == "1");
            Assert.NotNull(userDetail1);
            userDetail1.AuthorityPercent = 30;
            var userDetail2 = _context.UserDetails.FirstOrDefault(x => x.UserId == "2");
            Assert.NotNull(userDetail2);
            userDetail2.AuthorityPercent = 50;
            _context.SaveChanges();
            await _userService.DeleteUser(2.ToString());
            var user = _context.Users.FirstOrDefault(x => x.Id == "2");
            Assert.Null(user);
            var user1 = _context.UserDetails.FirstOrDefault(x => x.UserId == newUser.Id);
            Assert.Equal(40, user1?.AuthorityPercent);
            var user2 = _context.UserDetails.FirstOrDefault(x => x.UserId == "1");
            Assert.Equal(60, user2?.AuthorityPercent);
        }

        [Fact]
        public async Task Should_Delete_Users()
        {
            Helpers.GetContextAndUserTestData(_context);
            await _userService.DeleteUsers();
            var users = _context.Users;
            Assert.Equal(0, users.Count());
        }

        [Fact]
        public async Task Should_Get_Active_User_Count()
        {
            Helpers.GetContextAndUserTestData(_context);
            var count = await _userService.GetActiveUserCount();
            Assert.Equal(1, count);
        }

        [Fact]
        public async Task Should_Get_All_Users()
        {
            Helpers.GetContextAndUserTestData(_context);
            var users = await _userService.GetAllUsers(null);
            Assert.Equal(3, users.Count());
        }

        [Fact]
        public async Task Should_Get_Specified_Count_Users()
        {
            Helpers.GetContextAndUserTestData(_context);
            var users = await _userService.GetAllUsers(2);
            Assert.Equal(2, users.Count());
        }

        [Fact]
        public async Task Should_Get_Specified_TenantUsers()
        {
            Helpers.GetContextAndUserTestData(_context);
            var users = await _userService.GetUsersAsync("test2");
            Assert.Equal(1, users.Count);
        }

        [Fact]
        public async Task Should_Get_Tenant_Admin()
        {
            Helpers.GetContextAndUserRoleTestData(_context);
            var admins = await _userService.ListAdmins();
            Assert.Equal(new[] {"1"}, admins.Select(r => r.Id).ToArray());
        }

        [Fact]
        public async Task Should_Get_TenantUsers()
        {
            Helpers.GetContextAndUserTestData(_context);
            var users = await _userService.GetUsersAsync();
            Assert.Equal(2, users.Count);
        }

        [Fact]
        public async Task Should_Get_User_By_Email()
        {
            Helpers.GetContextAndUserTestData(_context);
            var user = await _userService.GetUserByEmail("test@workhow.com");
            Assert.NotNull(user);
        }

        [Fact]
        public async Task Should_Get_User_By_Id()
        {
            var user = new ApplicationUser
            {
                Id = Guid.NewGuid().ToString(),
                Email = "test3@workhow.com",
                FirstName = "test3",
                LastName = "test3",
                CreatedAt = DateTime.UtcNow,
                SecurityStamp = new Guid().ToString(),
                EmailConfirmed = true,
                IsDeleted = false,
                UserDetail = new UserDetail {AuthorityPercent = 0, LanguagePreference = "tr"}
            };
            _context.Users.Add(user);
            _context.SaveChanges();
            var getUser = await _userService.GetUserById(user.Id);
            Assert.NotNull(getUser);
        }

        [Fact]
        public async Task Should_Get_UserByIdAndTenant()
        {
            Helpers.GetContextAndUserTestData(_context);
            var user = await _userService.GetUserByIdAndTenant(3.ToString(), "test2");
            Assert.NotNull(user);
        }

        [Fact]
        public async Task Should_Get_UserRoles()
        {
            _context.Users.Add(new ApplicationUser
            {
                Email = "test@workhow.com",
                FirstName = "test",
                LastName = "Test",
                TenantId = "test",
                CreatedAt = DateTime.UtcNow,
                SecurityStamp = new Guid().ToString(),
                EmailConfirmed = false,
                Id = 1.ToString(),
                IsDeleted = false,
                UserDetail = new UserDetail {AuthorityPercent = 1, LanguagePreference = "tr"}
            });

            _context.Roles.Add(new ApplicationRole {Id = 1.ToString(), Name = "Admin", TenantId = "test"});
            _context.UserRoles.Add(new IdentityUserRole<string> {RoleId = 1.ToString(), UserId = 1.ToString()});
            _context.SaveChanges();
            var userRoles = await _userService.GetUserRoles(1.ToString());
            Assert.Equal(1, userRoles.Count);
            Assert.Contains(userRoles, x => x.Id == 1.ToString() && x.Name == "Admin");
        }

        [Fact]
        public async Task Should_Get_Voter_Count()
        {
            Helpers.GetContextAndUserTestData(_context);
            var count = await _userService.GetVoterCount();
            Assert.Equal(1, count);
        }

        [Fact]
        public async Task Should_Return_All_Users_Count_TenantIndependent()
        {
            Helpers.GetContextAndUserTestData(_context);
            var count = await _userService.GetAllUserCount();
            Assert.Equal(3, count);
        }

        [Fact]
        public async Task Should_Set_GeneratedPassToken()
        {
            var token = Guid.NewGuid().ToString();
            Helpers.GetContextAndUserTestData(_context);
            await _userService.SetGeneratedPassToken(1.ToString(), token, "test");
            var user = _context.Users.FirstOrDefault(x => x.Id == 1.ToString());
            Assert.Equal(token, user?.GeneratePassToken);
        }

        [Fact]
        public async Task Should_Set_User_Language()
        {
            Helpers.GetContextAndUserTestData(_context);
            await _userService.SetUserLangPreference(1.ToString(), "en");
            var user = _context.UserDetails.FirstOrDefault(x => x.UserId == "1");
            Assert.Equal("en", user?.LanguagePreference);
        }

        [Fact]
        public async Task Should_Update_Authority_Percents()
        {
            Helpers.GetContextAndUserTestData(_context);
            var scores = new Dictionary<string, decimal>
            {
                {1.ToString(), 80},
                {2.ToString(), 120}
            };
            await _userService.UpdateAuthorityPercents(scores, "test");
            var user1 = _context.UserDetails.FirstOrDefault(x => x.UserId == "1");
            var user2 = _context.UserDetails.FirstOrDefault(x => x.UserId == "2");
            Assert.Equal(40, user1?.AuthorityPercent);
            Assert.Equal(60, user2?.AuthorityPercent);
        }

        [Fact]
        public async Task Should_Update_User()
        {
            var user = new ApplicationUser
            {
                Id = 1.ToString(),
                Email = "test2@workhow.com",
                FirstName = "test",
                LastName = "test",
                CreatedAt = DateTime.UtcNow,
                SecurityStamp = new Guid().ToString(),
                EmailConfirmed = true,
                IsDeleted = false,
                UserDetail = new UserDetail {AuthorityPercent = 0, LanguagePreference = "tr"}
            };
            await _userService.EditUser(user);
            var getUser = _userManager.Object.Users.First(x => x.Id == user.Id);
            Assert.Equal("test2@workhow.com", getUser.Email);
            Assert.Equal("test", getUser.FirstName);
            Assert.Equal("test", getUser.LastName);
        }

        [Fact]
        public async Task Should_Update_User_Details()
        {
            Helpers.GetContextAndUserTestData(_context);
            var userDetail = _context.UserDetails.FirstOrDefault(x => x.UserId == "1");
            Assert.NotNull(userDetail);
            userDetail.AuthorityPercent = 50;
            userDetail.InitialAuthorityPercent = 50;
            await _userService.UpdateUserDetail(userDetail, null);
            var user = _context.UserDetails.FirstOrDefault(x => x.UserId == "1");
            Assert.Equal(userDetail.AuthorityPercent, user?.AuthorityPercent);
            Assert.Equal(userDetail.InitialAuthorityPercent, user?.InitialAuthorityPercent);
        }
    }
}