using System.ComponentModel.DataAnnotations;

namespace Decidehub.Web.ViewModels.AccountViewModels
{
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "{0} Gereklidir")]
        [Display(Name = "Ad")]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "{0} Gereklidir")]
        [Display(Name = "Soyad")]
        public string LastName { get; set; }

        [Required(ErrorMessage = "{0} Gereklidir")]
        [EmailAddress(ErrorMessage = "Geçersiz Email Adresi")]
        [Display(Name = "Email")]
        public string Email { get; set; }


        [Required(ErrorMessage = "Şifre Gereklidir")]
        [StringLength(100, ErrorMessage = "{0} en az {2} en fazla {1} karakter uzunluğunda olmalıdır.",
            MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Şifre")]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Şifre Doğrula")]
        [Compare("Password", ErrorMessage = "Şifreler uyuşmamaktadır")]
        public string ConfirmPassword { get; set; }

        [Display(Name = "Sub Domain")]
        [Required(ErrorMessage = "Sub Domain Gereklidir")]
        [StringLength(100, ErrorMessage = "{0} en az {2} en fazla {1} karakter uzunluğunda olmalıdır.",
            MinimumLength = 5)]
        public string TenantId { get; set; }

        [Required(ErrorMessage = "Host Name Gereklidir")]
        public string HostName { get; set; }
        [Required(ErrorMessage = "Dil Gereklidir")]
        public string Lang { get; set; }
    }
}