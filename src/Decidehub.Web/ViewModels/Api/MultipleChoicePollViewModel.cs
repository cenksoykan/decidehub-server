using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Decidehub.Web.ViewModels.Api
{
    public class MultipleChoicePollViewModel : PollListViewModel
    {
        public bool IsPublic { get; set; }
        [Required] public List<string> Options { get; set; }
    }
}