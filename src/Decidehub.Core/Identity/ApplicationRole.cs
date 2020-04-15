using Microsoft.AspNetCore.Identity;

namespace Decidehub.Core.Identity
{
    public class ApplicationRole : IdentityRole<string>
    {
        public string TenantId { get; set; }
    }
}