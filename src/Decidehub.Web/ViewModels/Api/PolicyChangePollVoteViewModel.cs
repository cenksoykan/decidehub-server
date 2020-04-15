using System.ComponentModel.DataAnnotations;

namespace Decidehub.Web.ViewModels.Api
{
    public class PolicyChangePollVoteViewModel
    {
        [Required(ErrorMessage = "PollIdRequired")]
        public long PollId { get; set; }

        [Required(ErrorMessage = "PollValueRequired")]
        public int PollValue { get; set; }
    }
}