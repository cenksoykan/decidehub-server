using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Decidehub.Core.Entities;
using Decidehub.Core.Identity;
using Decidehub.Core.Interfaces;
using Decidehub.Core.Services;
using Decidehub.Infrastructure.Data;
using Decidehub.Infrastructure.Data.Repositories;
using Decidehub.Web.Controllers.Api;
using Decidehub.Web.Services;
using Decidehub.Web.ViewModels.Api;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;
using Moq;
using Xunit;

namespace UnitTests.Controllers
{
    public class UserControllerTests
    {
        public UserControllerTests()
        {
            _context = Helpers.GetContext("test");
            _currentUser = new ApplicationUser
            {
                Email = "test@workhow.com",
                FirstName = "test",
                LastName = "Test",
                CreatedAt = DateTime.UtcNow,
                SecurityStamp = new Guid().ToString(),
                EmailConfirmed = true,
                Id = 1.ToString(),
                IsDeleted = false,
                UserDetail = new UserDetail {AuthorityPercent = 30, LanguagePreference = "tr"}
            };
            _context.Roles.Add(new ApplicationRole {Id = 1.ToString(), Name = "Admin"});
            _context.UserRoles.Add(new IdentityUserRole<string> {RoleId = 1.ToString(), UserId = 1.ToString()});
            _context.Users.Add(_currentUser);
            _context.SaveChanges();
            _tenantsDbContext = Helpers.GetTenantContext();
            _pollLocalizerMock = new Mock<IStringLocalizer<UserController>>();
            _userManager = new Mock<FakeUserManager>();
            var userList = new List<ApplicationUser> {_currentUser};
            _userManager.Setup(x => x.Users).Returns(userList.AsQueryable());
            _userManager.Setup(x => x.FindByEmailAsync(It.IsAny<string>()))
                .ReturnsAsync((string x) => userList.FirstOrDefault(a => a.Email == x));
            _userManager.Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>()))
                .Callback((ApplicationUser usr) => userList.Add(usr)).ReturnsAsync(IdentityResult.Success);
            _userManager.Setup(x => x.UpdateAsync(It.IsAny<ApplicationUser>()))
                .Callback((ApplicationUser usr) => userList[userList.FindIndex(x => x.Id == usr.Id)] = usr)
                .ReturnsAsync(IdentityResult.Success);
            _userManager.Setup(x => x.GeneratePasswordResetTokenAsync(It.IsAny<ApplicationUser>()))
                .ReturnsAsync(new Guid().ToString());
            var userRepository = new EntityUserRepository(_context, _userManager.Object);
            var userService = new UserService(userRepository, null);
            var tenantProviderMock = new Mock<ITenantProvider>();
            tenantProviderMock.Setup(serv => serv.GetTenantId()).Returns("test");
            var mapper = Helpers.GetMapper();
            var userApiViewModelService = new UserApiViewModelService(userService, mapper, tenantProviderMock.Object);
            ITenantRepository tenantRepository = new EntityTenantRepository(_tenantsDbContext);
            IAsyncRepository<Setting> settingRepository = new EfRepository<Setting>(_context);
            ISettingService settingService = new SettingService(settingRepository);
            ITenantService tenantService = new TenantService(tenantRepository, settingService, null);
            var emailSenderMock = new Mock<IEmailSender>();
            _configMock = new Mock<IConfiguration>();

            _controller = new UserController(userApiViewModelService, _userManager.Object, userService, tenantService,
                emailSenderMock.Object, _configMock.Object, _pollLocalizerMock.Object);
            var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.PrimarySid, _currentUser.Id)
            }));

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext {User = user}
            };
        }

        private readonly ApplicationDbContext _context;
        private readonly TenantsDbContext _tenantsDbContext;
        private readonly UserController _controller;
        private ApplicationUser _currentUser;
        private readonly Mock<IStringLocalizer<UserController>> _pollLocalizerMock;
        private readonly Mock<FakeUserManager> _userManager;
        private readonly Mock<IConfiguration> _configMock;

        [Fact]
        public async Task Should_Add_User()
        {
            var model = new CreateUserViewModel
            {
                Email = "test2@workhow.com",
                ProcessedById = 1.ToString(),
                FirstName = "test123",
                LastName = "test12345",
                TenantId = "test"
            };
            var key = "BaseUrlApi";
            _configMock.Setup(_ => _[key]).Returns(key);
            var key1 = "MembershipInvitation";
            var localizedString = new LocalizedString(key1, key1);
            _pollLocalizerMock.Setup(_ => _[key1]).Returns(localizedString);
            var key2 = "MembershipInvitationMsg";
            var localizedString2 = new LocalizedString(key2, key2);
            _pollLocalizerMock.Setup(_ => _[key2]).Returns(localizedString2);
            var key3 = "ToGeneratePassword";
            var localizedString3 = new LocalizedString(key3, key3);
            _pollLocalizerMock.Setup(_ => _[key3]).Returns(localizedString3);
            var key4 = "ClickHere";
            var localizedString4 = new LocalizedString(key4, key4);
            _pollLocalizerMock.Setup(_ => _[key4]).Returns(localizedString4);

            var result = await _controller.AddEdit(model);
            var actionResult = Assert.IsType<OkObjectResult>(result);
            var actionResultObj = actionResult.Value as UserViewModel;
            var user = _userManager.Object.Users.FirstOrDefault(x => x.Id == actionResultObj.Id);
            Assert.NotNull(user);
            Assert.Equal(model.Email, user.Email);
            Assert.Equal(model.FirstName, user.FirstName);
            Assert.Equal(model.LastName, user.LastName);
        }

        [Fact]
        public async Task Should_Check_IfUserIsVoter()
        {
            var result = await _controller.IsVoter();
            Assert.True(result);
        }

        [Fact]
        public async Task Should_Delete_Account()
        {
            _tenantsDbContext.Tenants.Add(new Tenant
            {
                Id = "test",
                InActive = false,
                HostName = "test.decidehub.com",
                Lang = "tr"
            });
            _context.SaveChanges();
            var result = await _controller.DeleteAccount();
            var users = _context.Users;
            var tenants = _tenantsDbContext.Tenants;
            Assert.IsType<OkResult>(result);
            Assert.Equal(0, users.Count());
            Assert.Equal(0, tenants.Count());
        }

        [Fact]
        public async Task Should_Delete_User()
        {
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
                UserDetail = new UserDetail {AuthorityPercent = 1, LanguagePreference = "tr"}
            });
            _context.SaveChanges();
            var result = await _controller.DeleteUser(2.ToString());
            var user = _context.Users.FirstOrDefault(x => x.Id == "2");
            Assert.IsType<OkObjectResult>(result);
            Assert.Null(user);
        }

        [Fact]
        public async Task Should_DeleteAccount_Return_Error_If_CurrentUserIsNotInAdminRole()
        {
            _currentUser = new ApplicationUser
            {
                Email = "test2@workhow.com",
                FirstName = "Test",
                LastName = "Test",
                CreatedAt = DateTime.UtcNow,
                SecurityStamp = new Guid().ToString(),
                EmailConfirmed = false,
                Id = 2.ToString(),
                IsDeleted = false,
                UserDetail = new UserDetail {AuthorityPercent = 1, LanguagePreference = "tr"}
            };
            _context.SaveChanges();
            var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.PrimarySid, _currentUser.Id)
            }));

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext {User = user}
            };
            var key = "AdminUserError";
            var localizedString = new LocalizedString(key, key);
            _pollLocalizerMock.Setup(_ => _[key]).Returns(localizedString);
            var result = await _controller.DeleteAccount();
            var actionResult = Assert.IsType<BadRequestObjectResult>(result);
            var actionResultList = actionResult.Value as List<ErrorViewModel>;
            Assert.Equal(key, actionResultList?[0].Description);
        }

        [Fact]
        public async Task Should_Edit_User()
        {
            var model = new CreateUserViewModel
            {
                Email = "test2@workhow.com",
                ProcessedById = 1.ToString(),
                FirstName = "test123",
                LastName = "test12345",
                Id = 1.ToString()
            };
            var result = await _controller.AddEdit(model);
            var actionResult = Assert.IsType<OkObjectResult>(result);
            var actionResultObj = actionResult.Value as UserViewModel;
            var user = _userManager.Object.Users.FirstOrDefault(x => x.Id == actionResultObj.Id);
            Assert.NotNull(user);
            Assert.Equal(model.Email, user.Email);
            Assert.Equal(model.FirstName, user.FirstName);
            Assert.Equal(model.LastName, user.LastName);
        }

        [Fact]
        public async Task Should_Get_User()
        {
            var result = await _controller.GetUser(1.ToString());
            var actionResult = Assert.IsType<OkObjectResult>(result);
            var actionResultObj = actionResult.Value as UserViewModel;
            Assert.Equal(_currentUser.Email, actionResultObj?.Email);
        }

        [Fact]
        public async Task Should_Get_Users()
        {
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
                UserDetail = new UserDetail {AuthorityPercent = 1, LanguagePreference = "tr"}
            });

            _context.SaveChanges();
            var list = await _controller.Get();
            Assert.Equal(2, list.Count());
        }

        [Fact]
        public async Task Should_Return_Error_If_CurrentUserIsNotInAdminRole()
        {
            _currentUser = new ApplicationUser
            {
                Email = "test2@workhow.com",
                FirstName = "Test",
                LastName = "Test",
                CreatedAt = DateTime.UtcNow,
                SecurityStamp = new Guid().ToString(),
                EmailConfirmed = false,
                Id = 2.ToString(),
                IsDeleted = false,
                UserDetail = new UserDetail {AuthorityPercent = 1, LanguagePreference = "tr"}
            };
            _context.SaveChanges();
            var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.PrimarySid, _currentUser.Id)
            }));

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext {User = user}
            };
            var key = "AdminUserError";
            var localizedString = new LocalizedString(key, key);
            _pollLocalizerMock.Setup(_ => _[key]).Returns(localizedString);
            var result = await _controller.DeleteUser(1.ToString());
            var actionResult = Assert.IsType<BadRequestObjectResult>(result);
            var actionResultList = actionResult.Value as List<ErrorViewModel>;
            Assert.Equal(key, actionResultList?[0].Description);
        }

        [Fact]
        public async Task Should_Return_Error_IfCurrentUserIsNotAdmin()
        {
            _currentUser = new ApplicationUser
            {
                Email = "test2@workhow.com",
                FirstName = "Test",
                LastName = "Test",
                CreatedAt = DateTime.UtcNow,
                SecurityStamp = new Guid().ToString(),
                EmailConfirmed = false,
                Id = 2.ToString(),
                IsDeleted = false,
                UserDetail = new UserDetail {AuthorityPercent = 1, LanguagePreference = "tr"}
            };
            _context.SaveChanges();

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                    {
                        new Claim(ClaimTypes.PrimarySid, _currentUser.Id)
                    }))
                }
            };

            var model = new CreateUserViewModel
            {
                Email = "test2@workhow.com",
                ProcessedById = 2.ToString()
            };
            var key = "AdminUserError";
            var localizedString = new LocalizedString(key, key);
            _pollLocalizerMock.Setup(_ => _[key]).Returns(localizedString);
            var result = await _controller.AddEdit(model);
            var actionResult = Assert.IsType<BadRequestObjectResult>(result);
            var actionResultList = actionResult.Value as List<ErrorViewModel>;
            Assert.Equal(key, actionResultList?[0].Description);
        }

        [Fact]
        public async Task Should_Return_Error_IfUserExistsWithSameEmail()
        {
            var model = new CreateUserViewModel
            {
                Email = "test@workhow.com"
            };
            var key = "UserExistWithSameEmail";
            var localizedString = new LocalizedString(key, key);
            _pollLocalizerMock.Setup(_ => _[key]).Returns(localizedString);
            var result = await _controller.AddEdit(model);
            var actionResult = Assert.IsType<BadRequestObjectResult>(result);
            var actionResultList = actionResult.Value as List<ErrorViewModel>;
            Assert.Equal(key, actionResultList?[0].Description);
        }

        [Fact]
        public async Task Should_Return_NotFound_IfUserDoesntExist()
        {
            var result = await _controller.GetUser(2.ToString());
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Should_Set_UserLanguagePreference()
        {
            var result = await _controller.SetUserLangPreference(1.ToString(), "en");
            Assert.IsType<OkObjectResult>(result);
            var user = _context.Users.FirstOrDefault(x => x.Id == "1");
            Assert.Equal("en", user?.UserDetail.LanguagePreference);
        }
    }
}