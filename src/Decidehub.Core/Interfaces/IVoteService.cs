using System.Collections.Generic;
using System.Threading.Tasks;
using Decidehub.Core.Entities;

namespace Decidehub.Core.Interfaces
{
    public interface IVoteService
    {
        Task<Vote> AddVote(Vote vote);
        Task<bool> UserVotedInPoll(string userId, long pollId);
        Task<decimal> GetVotedAuthorityPercentageInPoll(Poll poll);
        Task<int> GetVotedUserCountInPoll(long pollId, bool ignoreTenant);
        Task<IList<Vote>> GetVotesByPoll(long pollId);
        Task ResetVote(string userId, long pollId);
        Task DeleteVote(long pollId);
        Task AddVotes(IEnumerable<Vote> votes);
    }
}