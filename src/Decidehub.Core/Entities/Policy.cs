using System;
using Decidehub.Core.Enums;
using Decidehub.Core.Identity;

namespace Decidehub.Core.Entities
{
    public class Policy
    {
        public long Id { get; set; }

        public string TenantId { get; set; }

        public string Title { get; set; }

        public string Body { get; set; }

        public string UserId { get; set; }


        public DateTime CreatedAt { get; set; }

        public ApplicationUser User { get; set; }

        public PolicyStatus PolicyStatus { get; set; }
    }
}