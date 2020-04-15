using Decidehub.Core.Entities;

namespace Decidehub.Core.Specifications
{
    public class VoteFilterByPollSpecification : BaseSpecification<Vote>
    {
        public VoteFilterByPollSpecification(long pollId, bool ignoreQueryFilters = false) : base(
            v => v.PollId == pollId, ignoreQueryFilters)
        {
            Includes.Add(v => v.Voter);
            Includes.Add(v => v.Voter.UserDetail);
        }
    }
}