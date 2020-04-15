using Decidehub.Core.Entities;

namespace Decidehub.Core.Specifications
{
    public class VoteFilterByUserPollSpecification : BaseSpecification<Vote>
    {
        public VoteFilterByUserPollSpecification(string userId, long pollId) : base(v =>
            v.PollId == pollId && v.VoterId == userId)
        {
        }
    }
}