using System;
using Decidehub.Core.Enums;

namespace Decidehub.Web.ViewModels.Api
{
    public class PolicyViewModel
    {
        public long Id { get; set; }

        public string Title { get; set; }

        public string Body { get; set; }

        public DateTime CreatedAt { get; set; }

        public string UserName { get; set; }

        public PolicyStatus PolicyStatus { get; set; }
    }
}