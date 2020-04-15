using Decidehub.Core.Enums;

namespace Decidehub.Core.Entities
{
    public class MultipleChoicePoll : Poll
    {
        public override PollTypes PollType => PollTypes.MultipleChoicePoll;
    }
}