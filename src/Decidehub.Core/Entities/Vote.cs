using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Decidehub.Core.Identity;

namespace Decidehub.Core.Entities
{
    public class Vote
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        [ForeignKey("Poll")]
        [Display(Name = "Oylama")]
        public long PollId { get; set; }

        public long? Value { get; set; }

        [Display(Name = "Oylanan Kullanıcı")] public string VotedUserId { get; set; }

        [Display(Name = "Oylayan")] [Required] public string VoterId { get; set; }

        public DateTime VotedAt { get; set; }
        public string TenantId { get; set; }R
        public virtual ApplicationUser Voter { get; set; }
        public virtual Poll Poll { get; set; }
    }
}