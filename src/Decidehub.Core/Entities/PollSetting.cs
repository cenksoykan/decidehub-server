using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Decidehub.Core.Entities
{
    public class PollSetting
    {
        [Key] [ForeignKey("Poll")] public long PollId { get; set; }

        public string SettingJsonString { get; set; }
        public string TenantId { get; set; }
        public Poll Poll { get; set; }
    }
}