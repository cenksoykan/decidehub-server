using System.ComponentModel.DataAnnotations;

namespace Decidehub.Web.ViewModels.Api
{
    public class MultipleChoicePollVoteViewModel
    {
        [Required(ErrorMessage = "PollIdRequired")]
        public long PollId { get; set; }

        [Required(ErrorMessage = "PollValueRequired")]
        public int Value { get; set; }
    }
}