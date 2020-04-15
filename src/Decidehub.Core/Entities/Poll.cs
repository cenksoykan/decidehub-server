using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Decidehub.Core.Enums;
using Decidehub.Core.Identity;

namespace Decidehub.Core.Entities
{
    public abstract class Poll
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        public DateTime CreateTime { get; set; }

        [Display(Name = "Oylama Bitişi")] public DateTime Deadline { get; set; }

        public abstract PollTypes PollType { get; }

        [Display(Name = "Oylama Sorusu")]
        [DataType(DataType.MultilineText)]
        public string QuestionBody { get; set; }

        [Display(Name = "Oylama Adı")] public string Name { get; set; }

        [Display(Name = "Sonuç")] public string Result { get; set; }

        public bool Active { get; set; }

        public string UserId { get; set; }
        public string OptionsJsonString { get; set; }

        public long? PolicyId { get; set; }

        public virtual Policy Policy { get; set; }

        [Required] public string TenantId { get; set; }

        public virtual ApplicationUser User { get; set; }
        public virtual PollSetting PollSetting { get; set; }
    }
}