using System.Linq;
using Decidehub.Core.Helpers;
using Decidehub.Core.Interfaces;
using Decidehub.Infrastructure.Data;
using Microsoft.AspNetCore.Http;
using StackExchange.Profiling;

namespace Decidehub.Infrastructure.Services
{
    public class TenantProvider : ITenantProvider
    {
        private readonly TenantsDbContext _db;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public TenantProvider(IHttpContextAccessor httpContextAccessor, TenantsDbContext db)
        {
            _httpContextAccessor = httpContextAccessor;
            _db = db;
        }

        public string GetTenantId()
        {
            using (MiniProfiler.Current.Step("GetTenantId"))
            {
                var domain = GetSubdomain();
                if (domain == null) return null;

                using (MiniProfiler.Current.Step("TenantDbQuery"))
                {
                    var tenant = _db.Tenants.FirstOrDefault(x => x.Id == domain);
                    return tenant?.Id;
                }
            }
        }

        public string GetSubdomain()
        {
            if (_httpContextAccessor.HttpContext == null) return null;

            var host = _httpContextAccessor.HttpContext.Request.Host.Value;
            return UrlParser.GetSubDomain(host) ?? "";
        }
    }
}