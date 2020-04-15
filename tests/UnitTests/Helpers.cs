using System;
using AutoMapper;
using Decidehub.Core.Entities;
using Decidehub.Core.Identity;
using Decidehub.Core.Interfaces;
using Decidehub.Infrastructure.Data;
using Decidehub.Web.MappingProfiles;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace UnitTests
{
    public static class Helpers
    {
        public static ApplicationDbContext GetContext(string tenant)
        {
            var tenantProviderMock = new Mock<ITenantProvider>();

            tenantProviderMock.Setup(serv => serv.GetTenantId()).Returns(tenant);
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            var context = new ApplicationDbContext(options, tenantProviderMock.Object);


            return context;
        }

        public static TenantsDbContext GetTenantContext()
        {
            var options = new DbContextOptionsBuilder<TenantsDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            var context = new TenantsDbContext(options);
            return context;
        }

        public static ApplicationDbContext GetContextAndUserTestData(ApplicationDbContext context = null)
        {
             context ??= GetContext("test");
            context.Users.Add(new ApplicationUser
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

            context.Users.Add(new ApplicationUser
            {
                Email = "test2@workhow.com",
                FirstName = "test2",
                LastName = "test2",
                TenantId = "test",
                CreatedAt = DateTime.UtcNow,
                SecurityStamp = new Guid().ToString(),
                EmailConfirmed = true,
                Id = 2.ToString(),
                IsDeleted = false,
                UserDetail = new UserDetail {AuthorityPercent = 0, LanguagePreference = "tr"}
            });

            context.Users.Add(new ApplicationUser
            {
                Email = "test2@workhow.com",
                FirstName = "test2",
                LastName = "test2",
                TenantId = "test2",
                CreatedAt = DateTime.UtcNow,
                SecurityStamp = new Guid().ToString(),
                EmailConfirmed = true,
                Id = 3.ToString(),
                IsDeleted = false,
                UserDetail = new UserDetail {AuthorityPercent = 0, LanguagePreference = "tr"}
            });
            context.SaveChanges();
            return context;
        }

        public static ApplicationDbContext GetContextAndUserRoleTestData(ApplicationDbContext context = null)
        {
            context = context == null ? GetContextAndUserTestData() : GetContextAndUserTestData(context);
            context.Roles.Add(new ApplicationRole {Id = 1.ToString(), Name = "Admin", TenantId = "test"});
            context.Roles.Add(new ApplicationRole {Id = 2.ToString(), Name = "Admin", TenantId = "test2"});
            context.Roles.Add(new ApplicationRole {Id = 3.ToString(), Name = "Test", TenantId = "test"});
            context.UserRoles.Add(new IdentityUserRole<string> {RoleId = 1.ToString(), UserId = 1.ToString()});
            context.UserRoles.Add(new IdentityUserRole<string> {RoleId = 3.ToString(), UserId = 2.ToString()});
            context.UserRoles.Add(new IdentityUserRole<string> {RoleId = 2.ToString(), UserId = 3.ToString()});
            context.SaveChanges();
            return context;
        }

        public static IMapper GetMapper()
        {
            var mapper = new Mapper(
                new MapperConfiguration(
                    configure =>
                    {
                        configure.AddProfile<PollProfile>();
                        configure.AddProfile<SettingProfile>();
                        configure.AddProfile<UserProfile>();
                    }
                )
            );
            return mapper;
        }
    }
}