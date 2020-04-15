using Decidehub.Core.Enums;

namespace Decidehub.Core.Entities
{
    public class SharePoll : Poll
    {
        public override PollTypes PollType => PollTypes.SharePoll;
    }
}