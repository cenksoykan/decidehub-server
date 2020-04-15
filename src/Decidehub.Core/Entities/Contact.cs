using System.ComponentModel.DataAnnotations;
using Decidehub.Core.Identity;

namespace Decidehub.Core.Entities
{
    public class Contact
    {
        [Key] public long Id { get; set; }

        [Required(ErrorMessage = "Mesaj Girmelisiniz")]
        [Display(Name = "Mesaj")]
        [DataType(DataType.MultilineText)]
        public string Message { get; set; }

        public string UserId { get; set; }
        public string TenantId { get; set; }
        public virtual ApplicationUser User { get; set; }
    }
}