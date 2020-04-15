using System.ComponentModel.DataAnnotations;

namespace Decidehub.Web.ViewModels.AccountViewModels
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Email adresi giriniz")]
        [EmailAddress(ErrorMessage = "Geçersiz email adresi")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Şifre Gereklidir")]
        [DataType(DataType.Password)]
        [Display(Name = "Şifre")]
        public string Password { get; set; }

        [Display(Name = "Beni Hatırla?")] public bool RememberMe { get; set; }
    }
}