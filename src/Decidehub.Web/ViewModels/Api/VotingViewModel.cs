using System;

namespace Decidehub.Web.ViewModels.Api
{
    public class VotingViewModel
    {
        public long PollId { get; set; }
        public string PollName { get; set; }
        public string PollType { get; set; }
        public bool IsActive { get; set; }
        public DateTime Deadline { get; set; }
        public string Result { get; set; }
    }
}