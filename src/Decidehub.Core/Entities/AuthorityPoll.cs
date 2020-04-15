using Decidehub.Core.Enums;

namespace Decidehub.Core.Entities
{
    public class AuthorityPoll : Poll
    {
        public override PollTypes PollType => PollTypes.AuthorityPoll;
    }
}