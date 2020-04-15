using System.Collections.Generic;
using System.Threading.Tasks;
using Decidehub.Core.Entities;
using Decidehub.Core.Enums;

namespace Decidehub.Core.Interfaces
{
    public interface IPollService
    {
        Task<Poll> AddPoll(Poll poll);
        Task<bool> HasActivePollOfType<T>() where T : Poll;
        Task<IList<Poll>> GetUserNotVotedPolls(string userId);
        Task<Poll> GetPoll(long pollId, bool ignoreTenantId = false);
        Task<IList<Poll>> GetActivePolls(bool ignoreTenantId = false);
        Task EndPoll(long pollId);
        Task<IList<Poll>> GetUserVotedPolls(string userId);
        Task SetPollResult(long pollId, string result);
        Task NotifyUsers(PollTypes pollType, PollNotificationTypes pollNotificationType, Poll poll);
        Task<IList<Poll>> GetCompletedPolls(int? count = null);
        Task<bool> UserCanVote(long pollId, string userId);
        Task SendDirectEmailToNotVotedUsers(string tenantId, string subject, string message, long pollId);
        Task SendDirectEmailToNotVotedUser(string tenantId, string subject, string message, string userId);
        Task DeletePoll(long id);
        Task<bool> IsNotVotedPoll(string userId, Poll poll);
        Task<bool> IsVotedPoll(string userId, Poll poll);
        bool IsCompleted(Poll poll);
        Task<int> GetPollCountByType<T>(string tenantId = null) where T : Poll;
        Task<double> GetPollVotingDuration(long pollId, bool ignoreTenant = false);
        Task<int> GetPollRequiredUserPercentage(long pollId, bool ignoreTenant = false);
        Task<T> GetLastPollOfType<T>(string tenantId = null) where T : Poll;
    }
}