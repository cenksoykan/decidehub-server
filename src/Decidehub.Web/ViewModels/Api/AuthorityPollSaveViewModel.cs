using System.Collections.Generic;

namespace Decidehub.Web.ViewModels.Api
{
    public class AuthorityPollSaveViewModel
    {
        public long PollId { get; set; }
        public List<AuthorityPollUserValues> Votes { get; set; }
    }

    public class AuthorityPollUserValues
    {
        public string UserId { get; set; }
        public int Value { get; set; }
    }
}