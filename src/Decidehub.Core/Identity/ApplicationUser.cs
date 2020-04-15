using System;
using Decidehub.Core.Entities;
using Microsoft.AspNetCore.Identity;

namespace Decidehub.Core.Identity
{
    // Add profile data for application users by adding properties to the ApplicationUser class
    public class ApplicationUser : IdentityUser<string>
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string UserImageGoogleUrl { get; set; }
        public string TenantId { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime? CreatedAt { get; set; }
        public UserDetail UserDetail { get; set; }
        public UserImage UserImage { get; set; }
        public string GeneratePassToken { get; set; }
    }
}