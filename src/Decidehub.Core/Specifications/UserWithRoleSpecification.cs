using Decidehub.Core.Identity;

namespace Decidehub.Core.Specifications
{
    public class UserWithRoleSpecification : BaseSpecification<ApplicationUser>
    {
        public UserWithRoleSpecification() : base(u => true)
        {
            // AddInclude(u => u.Roles);
        }
    }
}