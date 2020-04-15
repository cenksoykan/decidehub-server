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
using Decidehub.Web.Controllers.Api;
using Decidehub.Web.ViewModels.AccountViewModels;
using Decidehub.Web.ViewModels.Api;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;
using Moq;
using Xunit;

namespace UnitTests.Controllers
{
    public class ApiAccountControllerTests
    {
        public ApiAccountControllerTests()
        {
            _context = Helpers.GetContext("test");
            var tenantsDbContext = Helpers.GetTenantContext();
            _userManager = new Mock<FakeUserManager>();
            ITenantRepository tenantRepository = new EntityTenantRepository(tenantsDbContext);
            IAsyncRepository<Setting> settingRepository = new EfRepository<Setting>(_context);
            ISettingService settingService = new SettingService(settingRepository);
            IAsyncRepository<ApplicationRole> roleRepository = new EfRepository<ApplicationRole>(_context);
            var roleService = new RoleService(roleRepository);
            ITenantService tenantService = new TenantService(tenantRepository, settingService, roleService);

            var userList = new List<ApplicationUser>();
            _userManager.Setup(x => x.Users).Returns(userList.AsQueryable());
            _userManager.Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
                .Callback(
                    (ApplicationUser usr, string pass) =>
                    {
                        usr.Id = "2";
                        userList.Add(usr);
                    })
                .ReturnsAsync(IdentityResult.Success);
            var userRepository = new EntityUserRepository(_context, _userManager.Object);
            var userService = new UserService(userRepository, null);
            var emailSender = new Mock<IEmailSender>();
            var configMock = new Mock<IConfiguration>();
            _accountLocalizerMock = new Mock<IStringLocalizer<ApiAccountController>>();
            var tenantProviderMock = new Mock<ITenantProvider>();
            tenantProviderMock.Setup(serv => serv.GetTenantId()).Returns("test");

            _controller = new ApiAccountController(tenantService, userService, _userManager.Object, emailSender.Object,
                configMock.Object, _accountLocalizerMock.Object, tenantProviderMock.Object);
            var currentUser = new ApplicationUser
            {
                Email = "test@workhow.com",
                FirstName = "test",
                LastName = "Test",
                CreatedAt = DateTime.UtcNow,
                SecurityStamp = new Guid().ToString(),
                EmailConfirmed = true,
                Id = "1",
                IsDeleted = false,
                UserDetail = new UserDetail
                    {InitialAuthorityPercent = 0, AuthorityPercent = 30, LanguagePreference = "tr"}
            };
            _context.Users.Add(currentUser);
            _context.SaveChanges();
            tenantsDbContext.Tenants.Add(new Tenant
            {
                Id = "test",
                HostName = "test.decidehub.com",
                Lang = "tr"
            });
            tenantsDbContext.SaveChanges();
        }

        private readonly ApplicationDbContext _context;
        private readonly Mock<FakeUserManager> _userManager;
        private readonly Mock<IStringLocalizer<ApiAccountController>> _accountLocalizerMock;
        private readonly ApiAccountController _controller;

        [Fact]
        public async Task Should_AddTenant_AddUser_AssignAdminRole()
        {
            var model = new RegisterViewModel
            {
                TenantId = "test2",
                Email = "test2@workhow.com",
                FirstName = "test",
                LastName = "test",
                Password = "123456",
                ConfirmPassword = "123456",
                HostName = "test2.decidehub.com",
                Lang = "tr"
            };
            var result = await _controller.Register(model);

            var user = _userManager.Object.Users.FirstOrDefault(x => x.Email == model.Email && x.TenantId == "test2");
            var role = _context.Roles.IgnoreQueryFilters()
                .FirstOrDefault(r => r.Name == "Admin" && r.TenantId == model.TenantId);
            Assert.IsType<OkResult>(result);
            Assert.NotNull(user);
            Assert.Equal(model.Email, user.Email);
            Assert.Equal(model.FirstName, user.FirstName);
            Assert.False(user.EmailConfirmed);
            Assert.Equal(model.LastName, user.LastName);
            Assert.Equal(model.Lang, user.UserDetail.LanguagePreference);
            var roleCheck = _context.UserRoles.FirstOrDefault(x => x.RoleId == role.Id && x.UserId == user.Id);
            Assert.True(roleCheck != null);
        }

        [Fact]
        public async Task Should_Confirm_User()
        {
            _context.Users.Add(new ApplicationUser
            {
                Email = "test2@workhow.com",
                FirstName = "test",
                LastName = "Test",
                CreatedAt = DateTime.UtcNow,
                SecurityStamp = new Guid().ToString(),
                EmailConfirmed = false,
                Id = "2",
                IsDeleted = false,
                UserDetail = new UserDetail
                    {AuthorityPercent = 30, LanguagePreference = "tr"}
            });
            _context.SaveChanges();
            _userManager.Setup(x => x.ConfirmEmailAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
                .Callback(
                    (ApplicationUser usr, string token) =>
                    {
                        usr.EmailConfirmed = true;
                        _context.SaveChanges();
                    })
                .ReturnsAsync(IdentityResult.Success);


            var model = new UserTokenViewModel
            {
                UserId = "2",
                Code = new Guid().ToString()
            };
            var result = await _controller.ConfirmEmail(model);
            var user = _context.Users.FirstOrDefault(x => x.Id == model.UserId);
            Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(user);
            Assert.True(user.EmailConfirmed);
        }

        [Fact]
        public async Task Should_Return_Error_ForgotPassword_UserNotConfirmed()
        {
            _context.Users.Add(new ApplicationUser
            {
                Email = "test2@workhow.com",
                FirstName = "test",
                LastName = "Test",
                CreatedAt = DateTime.UtcNow,
                SecurityStamp = new Guid().ToString(),
                EmailConfirmed = false,
                Id = "2",
                IsDeleted = false,
                UserDetail = new UserDetail
                    {AuthorityPercent = 30, LanguagePreference = "tr"}
            });
            _context.SaveChanges();
            _userManager.Setup(x => x.FindByEmailAsync(It.IsAny<string>()))
                .ReturnsAsync((string x) => _context.Users.FirstOrDefault(u => u.Email == x));

            _userManager.Setup(x => x.IsEmailConfirmedAsync(It.IsAny<ApplicationUser>()))
                .ReturnsAsync(
                    (ApplicationUser usr) =>
                        _context.Users.FirstOrDefault(u => u.Id == usr.Id)?.EmailConfirmed ?? false);
            var key = "InvalidUser";
            var localizedString = new LocalizedString(key, key);
            _accountLocalizerMock.Setup(_ => _[key]).Returns(localizedString);
            var model = new ForgotPasswordViewModel
            {
                Email = "test2@workhow.com"
            };
            var result = await _controller.ForgotPassword(model);
            var actionResult = Assert.IsType<BadRequestObjectResult>(result);
            var actionResultList = actionResult.Value as List<ErrorViewModel>;
            Assert.Equal(key, actionResultList?[0].Description);
        }

        [Fact]
        public async Task Should_Return_Error_ForgotPassword_UserNotExists()
        {
            _userManager.Setup(x => x.FindByEmailAsync(It.IsAny<string>()))
                .ReturnsAsync((string x) => _context.Users.FirstOrDefault(u => u.Email == x));

            _userManager.Setup(x => x.IsEmailConfirmedAsync(It.IsAny<ApplicationUser>()))
                .ReturnsAsync(
                    (ApplicationUser usr) =>
                        _context.Users.FirstOrDefault(u => u.Id == usr.Id)?.EmailConfirmed ?? false);
            const string key = "InvalidUser";
            var localizedString = new LocalizedString(key, key);
            _accountLocalizerMock.Setup(_ => _[key]).Returns(localizedString);
            var model = new ForgotPasswordViewModel
            {
                Email = "test2@workhow.com"
            };
            var result = await _controller.ForgotPassword(model);
            var actionResult = Assert.IsType<BadRequestObjectResult>(result);
            var actionResultList = actionResult.Value as List<ErrorViewModel>;
            Assert.NotNull(actionResultList);
            Assert.NotEmpty(actionResultList);
            Assert.Equal(key, actionResultList[0].Description);
        }

        [Fact]
        public async Task Should_Return_Error_If_ConfirmEmail_UserNotFound()
        {
            var model = new UserTokenViewModel
            {
                UserId = "3"
            };
            var key = "UserNotFound";
            var localizedString = new LocalizedString(key, key);
            _accountLocalizerMock.Setup(_ => _[key]).Returns(localizedString);
            var result = await _controller.ConfirmEmail(model);
            var actionResult = Assert.IsType<BadRequestObjectResult>(result);
            var actionResultList = actionResult.Value as List<ErrorViewModel>;
            Assert.Equal(key, actionResultList?[0].Description);
        }

        [Fact]
        public async Task Should_Return_Error_IfSameTenantExists()
        {
            var model = new RegisterViewModel
            {
                TenantId = "test",
                Email = "test2@workhow.com"
            };
            var key = "TenantExists";
            var localizedString = new LocalizedString(key, key);
            _accountLocalizerMock.Setup(_ => _[key]).Returns(localizedString);
            var result = await _controller.Register(model);
            var actionResult = Assert.IsType<BadRequestObjectResult>(result);
            var actionResultList = actionResult.Value as List<ErrorViewModel>;
            Assert.NotNull(actionResultList);
            Assert.NotEmpty(actionResultList);
            Assert.Equal(key, actionResultList[0].Description);
        }
    }
}