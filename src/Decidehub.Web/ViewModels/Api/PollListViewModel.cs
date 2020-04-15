using System;

namespace Decidehub.Web.ViewModels.Api
{
    public class PollListViewModel
    {
        public long PollId { get; set; }
        public string Name { get; set; }
        public DateTime Deadline { get; set; }
        public string Type { get; set; }
        public string Description { get; set; }
        public string Result { get; set; }
        public string MultipleChoiceResult { get; set; }
        public bool UserVoted { get; set; }
        public string UserId { get; set; }
        public string ListType { get; set; }
        public string StartedBy { get; set; }

        public long? PolicyId { get; set; }
    }
}