using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Decidehub.Core.Identity;
using Decidehub.Core.Interfaces;
using Decidehub.Core.Services;
using Decidehub.Infrastructure.Data;
using Xunit;

namespace UnitTests.Services
{
    public class RoleServiceTests
    {
        private readonly ApplicationDbContext _context;
        private readonly IRoleService _roleService;
        public RoleServiceTests()
        {
            _context = Helpers.GetContext("test");
            IAsyncRepository<ApplicationRole> roleRepository = new EfRepository<ApplicationRole>(_context);
            _roleService = new RoleService(roleRepository);
        }
        [Fact]
        public async Task Should_Get_Roles()
        {
            var roles = new List<ApplicationRole>
            {
                new ApplicationRole
                {
                    Id = Guid.NewGuid().ToString(),
                    Name="test"
                },
                 new ApplicationRole
                 {
                     Id = Guid.NewGuid().ToString(),
                    Name="test2"
                }
            };
            _context.Roles.AddRange(roles);
            _context.SaveChanges();
            var roleList = await _roleService.GetRolesAsync();
            Assert.Equal(2, roleList.Count);
        }
        [Fact]
        public async Task Should_Add_Role()
        {
            await _roleService.AddRole("role1", "test");
            Assert.True(_context.Roles.Any(x => x.Name == "role1"));
        }
        [Fact]
        public async Task Should_Get_Role_ByName()
        {
            var roles = new List<ApplicationRole>
            {
                new ApplicationRole
                {
                    Id = Guid.NewGuid().ToString(),
                    Name="test"
                },
                 new ApplicationRole
                 {
                     Id = Guid.NewGuid().ToString(),
                    Name="test2"
                }
            };
            _context.Roles.AddRange(roles);
            _context.SaveChanges();
            var role = await _roleService.GetRoleByName("test", "test");
            Assert.NotNull(role);
        }
    }
}
