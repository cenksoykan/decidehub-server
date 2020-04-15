using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Decidehub.Core.Entities
{
    public class Setting
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public string Key { get; set; }
        public string Value { get; set; }
        public bool IsVisible { get; set; }
        public string TenantId { get; set; }

        public dynamic Clone()
        {
            return MemberwiseClone();
        }
    }
}