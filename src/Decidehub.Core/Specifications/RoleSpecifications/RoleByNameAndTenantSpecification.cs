using Decidehub.Core.Identity;

namespace Decidehub.Core.Specifications.RoleSpecifications
{
    public class RoleByNameAndTenantSpecification : BaseSpecification<ApplicationRole>
    {
        public RoleByNameAndTenantSpecification(string name, string tenantId) : base(
            r => r.Name == name && r.TenantId == tenantId, true)
        {
        }
    }
}