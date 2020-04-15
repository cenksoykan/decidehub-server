using Decidehub.Core.Entities;
using Decidehub.Core.Enums;

namespace Decidehub.Core.Specifications
{
    public class ActivePollPolicySpecification : BaseSpecification<Policy>
    {
        public ActivePollPolicySpecification(bool ignoreQueryFilters = false) : base(
            r => r.PolicyStatus == PolicyStatus.Voting, ignoreQueryFilters)
        {
            AddInclude(r => r.User);
        }
    }
}