using System;
using System.Threading.Tasks;
using Decidehub.Core.Entities;
using Decidehub.Web.ViewModels.Api;

namespace Decidehub.Web.Interfaces
{
    public interface IPollApiViewModelService
    {
        Task<PolicyChangePoll> NewPolicyChangePoll(PolicyChangePollViewModel model);
        Task<AuthorityPoll> NewAuthorityPoll(AuthorityPollViewModel model);
        MultipleChoicePollViewModel MultipleChoicePollToViewModel(MultipleChoicePoll poll);
        PolicyChangePollViewModel PolicyChangePollToViewModel(PolicyChangePoll poll);
        Task<Poll> NewMultipleChoicePoll(MultipleChoicePollViewModel model);
        Task SaveAuthorityVote(AuthorityPollSaveViewModel model, string voterId);
        Task<Poll> NewSharePoll(SharePollViewModel model);
        Task SaveSharePoll(SharePollViewModel model);
        Task<AuthorityPollListViewModel> GetUsersForAuthorityVoting(AuthorityPoll poll);
        Task<PollStatusViewModel> GetPollStatus(long pollId);

        Task<bool> CheckFirstAuthorityPoll(string userId);
        Task<DateTime?> GetNextAuthorityPollStartDate();
    }
}