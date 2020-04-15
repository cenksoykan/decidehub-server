using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Decidehub.Core.Identity;
using Decidehub.Core.Interfaces;
using Decidehub.Core.Specifications.RoleSpecifications;

namespace Decidehub.Core.Services
{
    public class RoleService : IRoleService
    {
        private readonly IAsyncRepository<ApplicationRole> _roleRepository;

        public RoleService(IAsyncRepository<ApplicationRole> roleRepository)
        {
            _roleRepository = roleRepository;
        }

        public async Task<IList<ApplicationRole>> GetRolesAsync()
        {
            return await _roleRepository.ListAllAsync();
        }

        public async Task<ApplicationRole> AddRole(string name, string tenantId)
        {
            var entity = new ApplicationRole {Id = Guid.NewGuid().ToString(), Name = name, TenantId = tenantId};
            await _roleRepository.AddAsync(entity);
            return entity;
        }

        public async Task<ApplicationRole> GetRoleByName(string name, string tenantId)
        {
            var role = await _roleRepository.GetSingleBySpecAsync(new RoleByNameAndTenantSpecification(name, tenantId));
            return role;
        }
    }
}