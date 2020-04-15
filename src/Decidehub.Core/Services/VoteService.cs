using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Decidehub.Core.Entities;
using Decidehub.Core.Interfaces;
using Decidehub.Core.Specifications;

namespace Decidehub.Core.Services
{
    public class VoteService : IVoteService
    {
        private readonly IAsyncRepository<Vote> _voteRepository;
        private readonly IUserService _userService;

        public VoteService(IAsyncRepository<Vote> voteRepository, IUserService userService)
        {
            _voteRepository = voteRepository;
            _userService = userService;
        }

        public async Task<Vote> AddVote(Vote vote)
        {
            vote.VotedAt = DateTime.UtcNow;
            await _voteRepository.AddAsync(vote);
            return vote;
        }


        public async Task<decimal> GetVotedAuthorityPercentageInPoll(Poll poll)
        {
            var voters = await _userService.GetVoters(poll.TenantId);
            var voterSum = voters.Sum(v=>v.UserDetail.AuthorityPercent);

            if (voterSum == 0)
                return 0;
            var votedUserIds =
                (await _voteRepository.ListAsync(new VoteFilterByPollSpecification(poll.Id, true)))
                .Select(v => v.VoterId);
            var votedUsersSum  = voters.Where(x => votedUserIds.Contains(x.Id)).Sum(v=>v.UserDetail.AuthorityPercent);

            var result = 100 * votedUsersSum / voterSum;
            return result;
        }

        public async Task<int> GetVotedUserCountInPoll(long pollId, bool ignoreTenantId)
        {
            var votedUserCount =
                (await _voteRepository.ListAsync(new VoteFilterByPollSpecification(pollId, ignoreTenantId)))
                .Select(v => v.VoterId).Distinct().Count();
            return votedUserCount;
        }

        public async Task<IList<Vote>> GetVotesByPoll(long pollId)
        {
            var votes = await _voteRepository.ListAsync(new VoteFilterByPollSpecification(pollId, true));
            return votes;
        }

        public async Task ResetVote(string userId, long pollId)
        {
            var getVotes = await _voteRepository.ListAsync(new VoteFilterByUserPollSpecification(userId, pollId));
            await _voteRepository.DeleteRangeAsync(getVotes);
        }

        public async Task<bool> UserVotedInPoll(string userId, long pollId)
        {
            var result = await _voteRepository.AnyAsync(new VoteFilterByUserPollSpecification(userId, pollId));
            return result;
        }

        public async Task DeleteVote(long pollId)
        {
            var getVotes = await _voteRepository.ListAsync(new VoteFilterByPollSpecification(pollId));
            await _voteRepository.DeleteRangeAsync(getVotes);
        }

        public async Task AddVotes(IEnumerable<Vote> votes)
        {
            await _voteRepository.AddRangeAsync(votes);
        }
    }
}