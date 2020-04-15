using Decidehub.Core.Entities;
using Decidehub.Core.Enums;

namespace Decidehub.Core.Specifications
{
    public class CurrentPolicySpecification : BaseSpecification<Policy>
    {
        public CurrentPolicySpecification(bool ignoreQueryFilters = false) : base(
            r => r.PolicyStatus == PolicyStatus.Active, ignoreQueryFilters)
        {
            AddInclude(r => r.User);
        }
    }
}