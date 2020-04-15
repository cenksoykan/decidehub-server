using System.ComponentModel.DataAnnotations;

namespace Decidehub.Core.Entities
{
    public class Tenant
    {
        [Key]
        public string Id { get; set; }
        public string HostName { get; set; }
        public string Lang  { get; set; }
        public bool InActive { get; set; }
    }
}