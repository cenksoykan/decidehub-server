using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Decidehub.Core.Identity;

namespace Decidehub.Core.Entities
{
    public class UserDetail
    {
        [Key] [ForeignKey("User")] public string UserId { get; set; }

        [Display(Name = "Yetki Puanı")] public decimal AuthorityPercent { get; set; }
        
        public decimal InitialAuthorityPercent { get; set; }
        public string TenantId { get; set; }
        public string LanguagePreference { get; set; }
        
        public virtual ApplicationUser User { get; set; }
    }
}