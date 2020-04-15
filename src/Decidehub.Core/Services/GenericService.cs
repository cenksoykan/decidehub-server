using System.Threading.Tasks;
using Decidehub.Core.Interfaces;
using Microsoft.Extensions.Configuration;

namespace Decidehub.Core.Services
{
    public class GenericService : IGenericService
    {
        private readonly ITenantProvider _tenantProvider;
        private readonly ITenantService _tenantService;
        private readonly IConfiguration _config;

        public GenericService(ITenantProvider tenantProvider, IConfiguration config, ITenantService tenantService)
        {
            _tenantProvider = tenantProvider;
            _config = config;
            _tenantService = tenantService;
        }

        public async Task<string> GetBaseUrl(string tenant)
        {
            var baseUrl = $"https://{_config["BaseUrlApi"]}";
            if (tenant == null)
            {
                tenant = _tenantProvider.GetTenantId();
            }

            var getTenant = await _tenantService.GetTenant(tenant);
            if (getTenant != null)
            {
                baseUrl = $"https://{getTenant.Id}.{_config["BaseUrlApi"]}";
            }

            return baseUrl;
        }
    }
}