using System;
using System.ComponentModel.DataAnnotations;

namespace Decidehub.Web.ViewModels.Api
{
    public class UserViewModel : BaseViewModel<string>
    {
        [Required(ErrorMessage = "FirstNameRequired")]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "LastNameRequired")]
        public string LastName { get; set; }

        [EmailAddress(ErrorMessage = "InvalidEmailAddress")]
        [Required(ErrorMessage = "EmailRequired")]
        public string Email { get; set; }

        public string TenantId { get; set; }

        public bool IsAdmin { get; set; }
        public DateTime? CreatedAt { get; set; }
        public string UserImage { get; set; }
        public bool IsActive { get; set; }
        public bool HasAuthority  { get; set; }
        public decimal InitialAuthorityPercent { get; set; }
    }
}