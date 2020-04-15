using System.Collections.Generic;

namespace Decidehub.Web.ViewModels.Api
{
    public class AuthorityPollListViewModel : PollListViewModel
    {
        public List<AuthorityPollUsersViewModel> Users { get; set; }
    }
}