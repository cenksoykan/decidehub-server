using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Decidehub.Core.Entities;
using Decidehub.Core.Enums;
using Decidehub.Core.Interfaces;

namespace Decidehub.Core.Services
{
    public class TenantService : ITenantService
    {
        private readonly ITenantRepository _tenantRepository;
        private readonly ISettingService _settingService;
        private readonly IRoleService _roleService;

        public TenantService(ITenantRepository tenantRepository, ISettingService settingService,
            IRoleService roleService)
        {
            _tenantRepository = tenantRepository;
            _settingService = settingService;
            _roleService = roleService;
        }

        public async Task<Tenant> AddTenant(Tenant tenant)
        {
            await _tenantRepository.AddTenant(tenant);
            var settings = await _settingService.GetSettings(null);
            foreach (var item in settings)
            {
                if (await _settingService.GetSettingValueByType((Settings) Enum.Parse(typeof(Settings), item.Key),
                        tenant.Id) == null)
                {
                    try
                    {
                        var setting = new Setting
                        {
                            IsVisible = item.IsVisible,
                            TenantId = tenant.Id,
                            Key = item.Key,
                            Value = item.Value
                        };
                        await _settingService.AddSetting(setting);
                    }
                    catch (Exception)
                    {
                        // ignored
                    }
                }
            }

            var roleExists = await _roleService.GetRoleByName("Admin", tenant.Id) != null;
            if (!roleExists)
                await _roleService.AddRole("Admin", tenant.Id);
            return tenant;
        }

        public async Task DeleteTenant(string tenantId)
        {
            await _tenantRepository.DeleteTenant(tenantId);
        }

        public async Task<Tenant> GetTenant(string id)
        {
            return await _tenantRepository.GetTenant(id);
        }

        public async Task<int> GetTenantCount()
        {
            return await _tenantRepository.GetTenantCount();
        }

        public async Task<string> GetTenantLang(string id)
        {
            var tenant = await GetTenant(id);
            return tenant.Lang ?? "tr";
        }

        public async Task<IEnumerable<Tenant>> GetTenants()
        {
            return await _tenantRepository.GetTenants();
        }

        public async Task<Tenant> GetTenantWithIgnoredQueries(string id)
        {
            return await _tenantRepository.GetTenantWithIgnoredQueries(id);
        }
    }
}