using Decidehub.Core.Enums;

namespace Decidehub.Core.Entities
{
    public class PolicyChangePoll : Poll
    {
        public override PollTypes PollType => PollTypes.PolicyChangePoll;
    }
}