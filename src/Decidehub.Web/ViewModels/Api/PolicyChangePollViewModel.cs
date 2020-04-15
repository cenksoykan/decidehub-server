using Decidehub.Core.Enums;
using Decidehub.Web.Extensions;

namespace Decidehub.Web.ViewModels.Api
{
    public class PolicyChangePollViewModel : PollListViewModel
    {
        public string PollType => PollTypes.PolicyChangePoll.ToString().FirstCharacterToLower();
        public new long PolicyId { get; set; }
    }
}