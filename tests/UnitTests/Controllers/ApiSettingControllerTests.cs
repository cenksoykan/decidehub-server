using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Decidehub.Core.Entities;
using Decidehub.Core.Enums;
using Decidehub.Core.Identity;
using Decidehub.Core.Interfaces;
using Decidehub.Core.Services;
using Decidehub.Infrastructure.Data;
using Decidehub.Infrastructure.Data.Repositories;
using Decidehub.Web.Controllers.Api;
using Decidehub.Web.ViewModels.Api;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Moq;
using Xunit;

namespace UnitTests.Controllers
{
    public class ApiSettingControllerTests
    {
        public ApiSettingControllerTests()
        {
            var context = Helpers.GetContext("test");
            var currentUser = new ApplicationUser
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
            context.Users.Add(currentUser);          
            context.Roles.Add(new ApplicationRole {Id = 1.ToString(), Name = "Admin"});
            context.UserRoles.Add(new IdentityUserRole<string> {RoleId = 1.ToString(), UserId = 1.ToString()});

            context.SaveChanges();
            IAsyncRepository<Setting> settingRepository = new EfRepository<Setting>(context);
            ISettingService settingService = new SettingService(settingRepository);
            var mapper = Helpers.GetMapper();
            var tenantProviderMock = new Mock<ITenantProvider>();
            tenantProviderMock.Setup(serv => serv.GetTenantId()).Returns("test");

            var userManager = new Mock<FakeUserManager>();
            var userRepository = new EntityUserRepository(context, userManager.Object);
            var userService = new UserService(userRepository, null);

            var pollLocalizerMock = new Mock<IStringLocalizer<ApiSettingController>>();
            const string key = "AdminUserError";
            var localizedString = new LocalizedString(key, key);
            pollLocalizerMock.Setup(_ => _[key]).Returns(localizedString);

            _controller = new ApiSettingController(settingService, tenantProviderMock.Object, mapper, userService,
                pollLocalizerMock.Object);
            var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.PrimarySid, "1")
            }));

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext {User = user}
            };
        }

        private readonly ApiSettingController _controller;

        [Fact]
        public async Task Should_Get_Settings()
        {
            var list = await _controller.Get();
            var count = Enum.GetValues(typeof(Settings)).Length;
            Assert.Equal(count, list.Count);
        }

        [Fact]
        public async Task Should_Save_Settings()
        {
            var response = await _controller.Update(new SettingSaveViewModel
            {
                Settings = new List<SettingViewModel>
                {
                    new SettingViewModel
                    {
                        Key = Settings.VotingDuration.ToString(), Value = "77"
                    }
                }
            });

            Assert.IsType<OkObjectResult>(response);
            var count = Enum.GetValues(typeof(Settings)).Length;
            var settings = (IList<Setting>) ((OkObjectResult) response).Value;
            Assert.Equal(count, settings.Count);
            Assert.Equal("77", settings.FirstOrDefault(l => l.Key == Settings.VotingDuration.ToString())?.Value ?? "");
        }
    }
}