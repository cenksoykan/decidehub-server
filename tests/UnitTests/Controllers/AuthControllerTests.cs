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
using Decidehub.Web.Models;
using Decidehub.Web.Services;
using Decidehub.Web.ViewModels.AccountViewModels;
using Decidehub.Web.ViewModels.Api;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace UnitTests.Controllers
{
    public class AuthControllerTests
    {
        public AuthControllerTests()
        {
            _context = Helpers.GetContext("test");
            var option = new JwtIssuerOptions
            {
                Issuer = "webApi",
                Audience = "http://localhost:5000/"
            };

            var optionsMock = new Mock<IOptions<JwtIssuerOptions>>();
            optionsMock.Setup(ap => ap.Value).Returns(option);
            _userManager = new Mock<FakeUserManager>();
            var userRepository = new EntityUserRepository(_context, _userManager.Object);
            var userService = new UserService(userRepository, null);
            var tenantProviderMock = new Mock<ITenantProvider>();
            tenantProviderMock.Setup(serv => serv.GetTenantId()).Returns("test");
            var mapper = Helpers.GetMapper();
            var userViewModelService = new UserApiViewModelService(userService, mapper, tenantProviderMock.Object);
            _authLocalizerMock = new Mock<IStringLocalizer<AuthController>>();
            _controller = new AuthController(optionsMock.Object, _userManager.Object, userService, userViewModelService,
                _authLocalizerMock.Object);
        }

        private readonly ApplicationDbContext _context;
        private readonly Mock<FakeUserManager> _userManager;
        private readonly Mock<IStringLocalizer<AuthController>> _authLocalizerMock;
        private readonly AuthController _controller;

        [Fact]
        public async Task Should_Authenticate_User()
        {
            var user = new ApplicationUser
            {
                Email = "test@workhow.com",
                FirstName = "test",
                LastName = "Test",
                CreatedAt = DateTime.UtcNow,
                SecurityStamp = new Guid().ToString(),
                EmailConfirmed = true,
                Id = 1.ToString(),
                IsDeleted = false,
                UserDetail = new UserDetail
                    {AuthorityPercent = 30, LanguagePreference = "tr"}
            };
            _context.Users.Add(user);
            _context.SaveChanges();

            _userManager.Setup(x => x.IsEmailConfirmedAsync(It.IsAny<ApplicationUser>()))
                .ReturnsAsync(
                    (ApplicationUser usr) =>
                        _context.Users.FirstOrDefault(a => a.Id == usr.Id)?.EmailConfirmed ?? false);
            _userManager.Setup(x => x.CheckPasswordAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
                .ReturnsAsync(true);

            var model = new LoginViewModel
            {
                Email = "test@workhow.com",
                Password = "12345"
            };
            var result = await _controller.Post(model);
            var actionResult = Assert.IsType<OkObjectResult>(result);
            dynamic values = actionResult.Value;
            Assert.Equal(user.Email, values.GetType().GetProperty("Email").GetValue(values, null));
            Assert.Equal(user.FirstName, values.GetType().GetProperty("FirstName").GetValue(values, null));
            Assert.Equal(user.LastName, values.GetType().GetProperty("LastName").GetValue(values, null));
            Assert.Equal(user.TenantId, values.GetType().GetProperty("TenantId").GetValue(values, null));
            Assert.Equal(user.UserDetail.LanguagePreference,
                values.GetType().GetProperty("Lang").GetValue(values, null));
            Assert.False(values.GetType().GetProperty("IsAdmin").GetValue(values, null));
            Assert.Equal(user.Id, values.GetType().GetProperty("Id").GetValue(values, null));
        }

        [Fact]
        public async Task Should_Return_Error_IfEmailIsNotConfirmed()
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
                UserDetail = new UserDetail
                    {AuthorityPercent = 30, LanguagePreference = "tr"}
            });
            _context.SaveChanges();

            _userManager.Setup(x => x.IsEmailConfirmedAsync(It.IsAny<ApplicationUser>()))
                .ReturnsAsync(
                    (ApplicationUser usr) =>
                        _context.Users.FirstOrDefault(a => a.Id == usr.Id)?.EmailConfirmed ?? false);

            var model = new LoginViewModel
            {
                Email = "test@workhow.com",
                Password = "12345"
            };
            var key = "EmailNotConfirmed";
            var localizedString = new LocalizedString(key, key);
            _authLocalizerMock.Setup(_ => _[key]).Returns(localizedString);
            var result = await _controller.Post(model);
            var actionResult = Assert.IsType<BadRequestObjectResult>(result);
            var actionResultList = actionResult.Value as List<ErrorViewModel>;
            Assert.NotNull(actionResultList);
            Assert.NotEmpty(actionResultList);
            Assert.Equal(key, actionResultList[0].Description);
        }

        [Fact]
        public async Task Should_Return_Error_IfModelStateInvalid()
        {
            _controller.ModelState.AddModelError("Email", "Required");
            _controller.ModelState.AddModelError("Password", "Required");
            var model = new LoginViewModel();
            var result = await _controller.Post(model);
            var actionResult = Assert.IsType<BadRequestObjectResult>(result);
            var actionResultList = actionResult.Value as List<ErrorViewModel>;

            Assert.NotNull(actionResultList);
            Assert.NotEmpty(actionResultList);
            Assert.Equal(typeof(List<ErrorViewModel>), actionResult.Value.GetType());
            Assert.Equal(2, actionResultList.Count);
        }

        [Fact]
        public async Task Should_Return_Error_IfUserEmailNotFound()
        {
            var model = new LoginViewModel
            {
                Email = "test@workhow.com",
                Password = "12345"
            };
            var key = "InvalidLoginAttempt";
            var localizedString = new LocalizedString(key, key);
            _authLocalizerMock.Setup(_ => _[key]).Returns(localizedString);

            var key2 = "InvalidUserOrPass";
            var localizedString2 = new LocalizedString(key2, key2);
            _authLocalizerMock.Setup(_ => _[key2]).Returns(localizedString2);

            var result = await _controller.Post(model);
            var actionResult = Assert.IsType<BadRequestObjectResult>(result);
            var actionResultList = actionResult.Value as List<ErrorViewModel>;
            Assert.NotNull(actionResultList);
            Assert.NotEmpty(actionResultList);
            Assert.Equal(key2, actionResultList[0].Description);
            Assert.Equal(key, actionResultList[0].Title);
        }
    }
}