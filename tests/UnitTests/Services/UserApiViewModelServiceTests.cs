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
using Decidehub.Web.Interfaces;
using Decidehub.Web.Services;
using Decidehub.Web.ViewModels.Api;
using Microsoft.AspNetCore.Identity;
using Moq;
using Xunit;

namespace UnitTests.Services
{
    public class UserApiViewModelServiceTests
    {
        private readonly ApplicationDbContext _context;
        private readonly IUserApiViewModelService _userApiViewModelService;
        private readonly Mock<FakeUserManager> _userManager;
        public UserApiViewModelServiceTests()
        {
            _context = Helpers.GetContext("test");

            var list = new List<ApplicationUser>
            {
               new ApplicationUser
               {
                Id = 1.ToString(),
                Email = "test3@workhow.com",
                FirstName = "test3",
                LastName = "test3",
                CreatedAt = DateTime.UtcNow,
                SecurityStamp = new Guid().ToString(),
                EmailConfirmed = true,
                IsDeleted = false,
                UserDetail = new UserDetail { AuthorityPercent = 0, LanguagePreference = "tr" }
            }

        };

        _userManager = new Mock<FakeUserManager>();

            _userManager.Setup(x => x.Users)
                .Returns(list.AsQueryable());
            _userManager.Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>())).
                Callback((ApplicationUser usr) => list.Add(usr))
            .ReturnsAsync(IdentityResult.Success);
            _userManager.Setup(x => x.UpdateAsync(It.IsAny<ApplicationUser>()))
            .Callback((ApplicationUser usr) => list[list.FindIndex(x => x.Id == usr.Id)] = usr)
            .ReturnsAsync(IdentityResult.Success); 
            var userRepository = new EntityUserRepository(_context, _userManager.Object);
            var userService = new UserService(userRepository, null);
            var tenantProviderMock = new Mock<ITenantProvider>();
            tenantProviderMock.Setup(serv => serv.GetTenantId()).Returns("test");
            var mapper = Helpers.GetMapper();
            _userApiViewModelService = new UserApiViewModelService(userService, mapper, tenantProviderMock.Object);

        }
        [Fact]
        public async Task Should_Get_Users()
        {
            _context.Users.Add(new ApplicationUser
            {
                Email = "test@workhow.com",
                FirstName = "test",
                LastName = "Test",
                CreatedAt = DateTime.UtcNow,
                SecurityStamp = new Guid().ToString(),
                EmailConfirmed = false,
                Id = 1.ToString(),
                IsDeleted = false,
                UserDetail = new UserDetail { AuthorityPercent = 1, LanguagePreference = "tr" }
            });

            _context.Users.Add(new ApplicationUser
            {
                Email = "test2@workhow.com",
                FirstName = "Test",
                LastName = "Test",
                CreatedAt = DateTime.UtcNow,
                SecurityStamp = new Guid().ToString(),
                EmailConfirmed = false,
                Id = 2.ToString(),
                IsDeleted = false,
                UserDetail = new UserDetail { AuthorityPercent = 1, LanguagePreference = "tr" }
            });
            _context.Roles.Add(new ApplicationRole { Id = 1.ToString(), Name = "Admin" });
            _context.UserRoles.Add(new IdentityUserRole<string> { RoleId = 1.ToString(), UserId = 1.ToString() });
            _context.SaveChanges();
            
            var users = await _userApiViewModelService.ListUsersWithImages();
            var admin = users.FirstOrDefault(x => x.Id == "1");
            Assert.Equal(2, users.Count);
            Assert.True(admin?.IsAdmin);
        }
        [Fact]
        public async Task Should_Create_User()
        {
            var model = new CreateUserViewModel
            {
                Email = "test@workhow.com",
                FirstName = "test",
                LastName = "test"
            };
            var user = await _userApiViewModelService.CreateUser(model);
            var getUser = _userManager.Object.Users.FirstOrDefault(u => u.Id == user.Id);
            Assert.NotNull(getUser);
        }
        [Fact]
        public async Task Should_Update_User()
        {
            var model = new CreateUserViewModel
            {
                Id = 1.ToString(),
                Email = "test2@workhow.com",
                FirstName = "test",
                LastName = "Test"

            };        
            _context.Users.Add(_userManager.Object.Users.ToList()[0]);
            _context.SaveChanges();
            await _userApiViewModelService.EditUser(model, true);
            var user= _userManager.Object.Users.FirstOrDefault(u => u.Id == "1");
            Assert.Equal(model.Email, user?.Email);
            Assert.Equal(model.FirstName, user?.FirstName);
            Assert.Equal(model.LastName, user?.LastName);
        }
        [Fact]
        public async Task Should_GetUser_ById()
        {
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
                UserDetail = new UserDetail { AuthorityPercent = 1, LanguagePreference = "tr" }
            };
            _context.Users.Add(user);
            _context.Roles.Add(new ApplicationRole { Id = 1.ToString(), Name = "Admin" });
            _context.UserRoles.Add(new IdentityUserRole<string> { RoleId = 1.ToString(), UserId = 1.ToString() });
            _context.SaveChanges();
            var getUser = await _userApiViewModelService.GetUserById(1.ToString());
            Assert.Equal(user.Email, getUser.Email);
            Assert.True(getUser.HasAuthority);
            Assert.Equal(user.FirstName, getUser.FirstName);
            Assert.Equal(user.LastName, getUser.LastName);
            Assert.Equal(user.CreatedAt, getUser.CreatedAt);
            Assert.True(getUser.IsActive);
            Assert.True(getUser.IsAdmin);

        }
    }
}
