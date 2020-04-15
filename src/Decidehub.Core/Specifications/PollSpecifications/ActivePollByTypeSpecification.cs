using Decidehub.Core.Entities;
using Decidehub.Core.Enums;

namespace Decidehub.Core.Specifications.PollSpecifications
{
    public class ActivePollByTypeSpecification : BaseSpecification<Poll>
    {
        public ActivePollByTypeSpecification(PollTypes pollType) : base(p => p.Active && p.PollType == pollType)
        {
        }
    }
}