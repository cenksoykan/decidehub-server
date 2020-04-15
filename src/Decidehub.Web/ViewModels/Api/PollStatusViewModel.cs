using System.Collections.Generic;

namespace Decidehub.Web.ViewModels.Api
{
    public class PollStatusViewModel
    {
        public long PollId { get; set; }
        public string PollName  { get; set; }
        public IEnumerable<object> NotVotedUsers { get; set; }
        public IEnumerable<object> VotedUsers { get; set; }
    }
}
