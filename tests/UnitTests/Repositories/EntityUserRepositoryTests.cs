using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Decidehub.Core.Identity;
using Decidehub.Infrastructure.Data.Repositories;
using Microsoft.AspNetCore.Identity;
using Moq;
using Xunit;

namespace UnitTests.Repositories
{
    public class EntityUserRepositoryTests
    {
        [Fact]
        public async Task Should_Get_TenantUsers()
        {
            var context = Helpers.GetContextAndUserTestData();
            var userRepository = new EntityUserRepository(context, null);
            var result = await userRepository.GetUsers();
            Assert.Equal(2, result.Count);
        }

        [Fact]
        public async Task Should_Get_GivenTenantUsers()
        {
            var context = Helpers.GetContextAndUserTestData();
            var userRepository = new EntityUserRepository(context, null);
            var result = await userRepository.GetUsers("test");
            var result2 = await userRepository.GetUsers("test2");
            Assert.Equal(2, result.Count);
            Assert.Single(result2);
        }

        [Fact]
        public async Task Should_Get_UserRoles()
        {
            var context = Helpers.GetContextAndUserRoleTestData();
            var userRepository = new EntityUserRepository(context, null);
            var result = await userRepository.GetUserRoles(1.ToString());

            Assert.Equal(1, result.Count);
            Assert.Contains(result, x => x.Id == 1.ToString() && x.Name == "Admin");
        }

        [Fact]
        public async Task Should_Assign_Role_To_User()
        {
            var context = Helpers.GetContextAndUserRoleTestData();
            var userRepository = new EntityUserRepository(context, null);
            var userRole = new IdentityUserRole<string> {RoleId = 3.ToString(), UserId = 1.ToString()};
            await userRepository.AssignRoleToUser(userRole);
            Assert.NotNull(context.UserRoles.SingleOrDefault(x =>
                x.RoleId == userRole.RoleId && x.UserId == userRole.UserId));
        }

        [Fact]
        public async Task Should_GetUser_By_Id()
        {
            var context = Helpers.GetContextAndUserRoleTestData();
            var userRepository = new EntityUserRepository(context, null);
            var user = await userRepository.GetUser(1.ToString());
            Assert.NotNull(user);
        }

        [Fact]
        public async Task Should_GetDifferentTenantUser_ById()
        {
            var context = Helpers.GetContextAndUserRoleTestData();
            var userRepository = new EntityUserRepository(context, null);
            var user = await userRepository.GetUser(3.ToString(), true);
            Assert.NotNull(user);
            Assert.NotEqual("test", user.TenantId);
        }

        [Fact]
        public async Task Should_DeleteUser()
        {
            var context = Helpers.GetContextAndUserTestData();
            var userRepository = new EntityUserRepository(context, null);
            await userRepository.DeleteUser(2.ToString());
            var user = await userRepository.GetUser(2.ToString());
            Assert.Null(user);
        }

        [Fact]
        public async Task Should_Return_TenantVoters_Count()
        {
            var context = Helpers.GetContextAndUserTestData();
            var userRepository = new EntityUserRepository(context, null);
            var testCount = await userRepository.GetVoterCount("test");
            var test2Count = await userRepository.GetVoterCount("test2");
            Assert.Equal(1, testCount);
            Assert.Equal(0, test2Count);
        }

        [Fact]
        public async Task Should_Correctly_Update_Authority_Percents()
        {
            var context = Helpers.GetContextAndUserTestData();
            var userRepository = new EntityUserRepository(context, null);
            var scores = new Dictionary<string, decimal>
            {
                {1.ToString(), 80},
                {2.ToString(), 40}
            };
            await userRepository.UpdateAuthorityPercents(scores, "test");
            var user1 = context.UserDetails.FirstOrDefault(x => x.UserId == "1");
            var user2 = context.UserDetails.FirstOrDefault(x => x.UserId == "2");
            Assert.Equal(66.7M, Math.Round(user1?.AuthorityPercent ?? 0, 1));
            Assert.Equal(33.3M, Math.Round(user2?.AuthorityPercent ?? 0, 1));
        }

        [Fact]
        public async Task Should_Update_UserDetail()
        {
            var context = Helpers.GetContextAndUserTestData();
            var userRepository = new EntityUserRepository(context, null);
            var userDetail = context.UserDetails.FirstOrDefault(x => x.UserId == "1");
            Assert.NotNull(userDetail);
            userDetail.AuthorityPercent = 50;
            userDetail.InitialAuthorityPercent = 64;
            await userRepository.UpdateUserDetails(userDetail);
            var user = context.UserDetails.FirstOrDefault(x => x.UserId == "1");
            Assert.Equal(userDetail.AuthorityPercent, user?.AuthorityPercent ?? 0);
            Assert.Equal(userDetail.InitialAuthorityPercent, user?.InitialAuthorityPercent ?? 0);
        }

        [Fact]
        public async Task Should_Get_User_ByEmail()
        {
            var context = Helpers.GetContextAndUserTestData();
            var userRepository = new EntityUserRepository(context, null);
            const string email = "test@workhow.com";
            var user = await userRepository.GetUserByEmail(email, null);
            Assert.NotNull(user);
            Assert.Equal(email, user.Email);
        }

        [Fact]
        public async Task Should_Get_All_Users()
        {
            var context = Helpers.GetContextAndUserTestData();
            var userRepository = new EntityUserRepository(context, null);
            var users = await userRepository.GetAllUsers(null);
            Assert.Equal(3, users.Count());
        }

        [Fact]
        public async Task Should_Get_SpecifiedCount_Users()
        {
            var context = Helpers.GetContextAndUserTestData();
            var userRepository = new EntityUserRepository(context, null);
            var users = await userRepository.GetAllUsers(2);
            Assert.Equal(2, users.Count());
        }

        [Fact]
        public async Task Should_Get_User_By_IdAndTenant()
        {
            var context = Helpers.GetContextAndUserTestData();
            var userRepository = new EntityUserRepository(context, null);
            var user = await userRepository.GetUserByIdAndTenant(1.ToString(), "test");
            var user2 = await userRepository.GetUserByIdAndTenant(3.ToString(), "test");
            Assert.NotNull(user);
            Assert.Null(user2);
        }

        [Fact]
        public async Task Should_GetVoters()
        {
            var context = Helpers.GetContextAndUserTestData();
            var userRepository = new EntityUserRepository(context, null);
            var voters = await userRepository.GetVoters();
            Assert.Equal(1, voters.Count);
            Assert.True(voters.Count(x => x.UserDetail.AuthorityPercent > 0) == 1);
        }

        [Fact]
        public async Task Should_Check_User_Role()
        {
            var context = Helpers.GetContextAndUserRoleTestData();
            var userRepository = new EntityUserRepository(context, null);
            Assert.True(await userRepository.UserInRole(1.ToString(), "Admin"));
            Assert.False(await userRepository.UserInRole(1.ToString(), "Test"));
        }

        [Fact]
        public async Task Should_Set_GeneratedPassToken()
        {
            var token = Guid.NewGuid().ToString();
            var context = Helpers.GetContextAndUserTestData();
            var userRepository = new EntityUserRepository(context, null);
            await userRepository.SetGeneratedPassToken(1.ToString(), token, "test");
            var user = context.Users.FirstOrDefault(x => x.Id == "1");
            Assert.Equal(token, user?.GeneratePassToken);
        }

        [Fact]
        public async Task Should_Get_Active_User_Count()
        {
            var context = Helpers.GetContextAndUserTestData();
            var userRepository = new EntityUserRepository(context, null);
            var count = await userRepository.GetActiveUserCount();
            Assert.Equal(1, count);
        }

        [Fact]
        public async Task Should_Return_All_Users_Count_TenantIndependent()
        {
            var context = Helpers.GetContextAndUserTestData();
            var userRepository = new EntityUserRepository(context, null);
            var count = await userRepository.GetAllUserCount();
            Assert.Equal(3, count);
        }

        [Fact]
        public async Task Should_Get_Tenant_Admins()
        {
            var context = Helpers.GetContextAndUserRoleTestData();
            var userRepository = new EntityUserRepository(context, null);
            var admin = await userRepository.ListAdmins();
            Assert.Equal(new [] {"1"}, admin.Select(r => r.Id).ToArray());
        }

        [Fact]
        public async Task Should_Set_User_Language()
        {
            var context = Helpers.GetContextAndUserTestData();
            var userManagerMock = new Mock<FakeUserManager>();
            userManagerMock.Setup(x => x.UpdateAsync(It.IsAny<ApplicationUser>()))
                .ReturnsAsync(IdentityResult.Success);
            var userRepository = new EntityUserRepository(context, userManagerMock.Object);

            await userRepository.SetUserLangPreference(1.ToString(), "en");
            var user = context.UserDetails.FirstOrDefault(x => x.UserId == "1");
            Assert.Equal("en", user?.LanguagePreference);
        }
    }
}