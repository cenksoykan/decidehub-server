using Decidehub.Core.Entities;
using Decidehub.Core.Enums;

namespace Decidehub.Core.Specifications.SettingSpecifications
{
    public class SettingByKeyTenantSpecification : BaseSpecification<Setting>
    {
        public SettingByKeyTenantSpecification(Settings setting, string tenantId) : base(s =>
            s.Key == setting.ToString() && s.TenantId == tenantId)
        {
        }
    }
}