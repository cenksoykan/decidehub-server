using Decidehub.Core.Entities;
using Decidehub.Core.Enums;

namespace Decidehub.Core.Specifications
{
    public class HistoricalPolicySpecification : BaseSpecification<Policy>
    {
        public HistoricalPolicySpecification(bool ignoreQueryFilters = false) : base(
            r => r.PolicyStatus == PolicyStatus.Overridden, ignoreQueryFilters)
        {
            AddInclude(r => r.User);
        }
    }
}