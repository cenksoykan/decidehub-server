using System.ComponentModel.DataAnnotations;

namespace Decidehub.Web.ViewModels.Api
{
    public class ResetPasswordViewModel : UserTokenViewModel
    {
        [Required(ErrorMessage = "PasswordRequired")]
        [StringLength(100, ErrorMessage = "PasswordLengthError",
            MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "ConfirmPassword")]
        [Compare("Password", ErrorMessage = "PasswordsDontMatch")]
        public string ConfirmPassword { get; set; }

        [Required(ErrorMessage = "EmailRequired")]
        public string Email { get; set; }

        /// <summary>
        /// Generate password (0 for password reset, 1 for email verification)
        /// </summary>
        public short Gen { get; set; }
    }

    public class UserTokenViewModel
    {
        [Required(ErrorMessage = "UserIdRequired")]
        public string UserId { get; set; }

        public string Code { get; set; }

        public string Subdomain { get; set; }
    }
}