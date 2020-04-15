using Decidehub.Core.Entities;

namespace Decidehub.Core.Specifications
{
    public class PolicyByIdSpecification : BaseSpecification<Policy>
    {
        public PolicyByIdSpecification(long id) : base(r => r.Id == id, true)
        {
        }
    }
}