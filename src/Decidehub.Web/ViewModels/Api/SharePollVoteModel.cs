using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Decidehub.Web.ViewModels.Api
{
    public class SharePollVoteModel : PollListViewModel
    {
        [Required] public List<SharePollVoteValuesModel> Options { get; set; }
    }
}