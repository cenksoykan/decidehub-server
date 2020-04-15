using Decidehub.Core.Entities;
using Decidehub.Core.Enums;

namespace Decidehub.Core.Specifications
{
    public class CurrentPolicyForTenantSpecification : BaseSpecification<Policy>
    {
        public CurrentPolicyForTenantSpecification(string tenantId) : base(
            r => r.PolicyStatus == PolicyStatus.Active && r.TenantId == tenantId, true)
        {
        }
    }
}