using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Decidehub.Core.Entities;
using Decidehub.Core.Identity;

namespace Decidehub.Core.Interfaces
{
    public interface IPollRepository
    {
        Task<Poll> AddPoll(Poll poll);
        Task<IList<Poll>> GetUserNotVotedPolls(string userId);
        Task<Poll> GetPoll(long pollId, bool ignoreTenant = false);
        Task<IList<Poll>> GetPollsSince(DateTime date, string tenantId = null);
        Task<IList<Poll>> GetActivePolls(bool ignoreTenantId = false, string tenantId = null);
        Task EndPoll(long pollId);
        Task<IList<Poll>> GetUserVotedPolls(string userId);
        Task SetPollResult(long pollId, string result);
        Task<IEnumerable<Poll>> GetCompletedPolls(int? count);
        Task<int> GetCompletedPollCount(bool ignoreTenant = false, string tenantId = null);
        Task<int> GetActivePollCount(bool ignoreTenant = false, string tenantId = null);
        Task<IList<ApplicationUser>> GetNotVotedUsers(long pollId, string tenantId = null);
        Task DeletePoll(long pollId);
        Task<IList<Poll>> GetAllPolls();
        Task<IList<Poll>> GetPublicPolls();
        Task<int> GetPollCountByType<T>(string tenantId = null) where T : Poll;
        Task<T> GetLastPollOfType<T>(string tenantId) where T : Poll;
        Task<bool> HasActivePollOfType<T>() where T : Poll;
    }
}