using System.ComponentModel.DataAnnotations;

namespace Decidehub.Web.ViewModels.AccountViewModels
{
    public class ForgotPasswordViewModel
    {
        [Required(ErrorMessage = "Email adresi giriniz")]
        [EmailAddress(ErrorMessage = "Geçersiz email adresi")]
        public string Email { get; set; }
        public string Subdomain { get; set; }
    }
}