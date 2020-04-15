namespace Decidehub.Web.ViewModels.Api
{
    public class UserVotedPollListViewModel : PollListViewModel
    {
        public UserVotedPollListViewModel()
        {
            UserVoted = true;
            ListType = "userVoted";
        }
    }
}