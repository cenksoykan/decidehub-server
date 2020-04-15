using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Decidehub.Core.Identity;

namespace Decidehub.Core.Entities
{
    public class UserImage
    {
        [Key] [ForeignKey("User")] public string UserId { get; set; }
        public string UserImageStr { get; set; }
        public string UserImageSmallStr { get; set; }
        public virtual ApplicationUser User { get; set; }
    }
}