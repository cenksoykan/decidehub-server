using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Decidehub.Web.ViewModels.Api
{
    public class SharePollViewModel : PollListViewModel
    {
        [Required] public List<string> Options { get; set; }
    }
}