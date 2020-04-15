using Decidehub.Core.Entities;

namespace Decidehub.Core.Specifications.SettingSpecifications
{
    public class VisibleSettingsSpecification : BaseSpecification<Setting>
    {
        public VisibleSettingsSpecification(string tenantId) : base(s => s.IsVisible && s.TenantId == tenantId, true)
        {
        }
    }
}