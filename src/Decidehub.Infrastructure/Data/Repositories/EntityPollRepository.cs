using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Decidehub.Core.Entities;
using Decidehub.Core.Enums;
using Decidehub.Core.Identity;
using Decidehub.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Decidehub.Infrastructure.Data.Repositories
{
    public class EntityPollRepository : IPollRepository
    {
        private readonly ApplicationDbContext _db;
        private readonly TenantsDbContext _tenantsDb;

        public EntityPollRepository(ApplicationDbContext db, TenantsDbContext tenantsDb)
        {
            _db = db;
            _tenantsDb = tenantsDb;
        }

        public async Task<Poll> AddPoll(Poll poll)
        {
            _db.Polls.Add(poll);
            await _db.SaveChangesAsync();
            return poll;
        }

        public async Task<IList<Poll>> GetUserNotVotedPolls(string userId)
        {
            var votedPollIds = _db.Votes.Where(x => x.VoterId == userId).Select(v => v.PollId);
            return await _db.Polls.Where(p => !votedPollIds.Contains(p.Id) && p.Active).ToListAsync();
        }


        public async Task<Poll> GetPoll(long pollId, bool ignoreTenant = false)
        {
            if (ignoreTenant)
                return await _db.Polls.IgnoreQueryFilters()
                    .Include(p => p.PollSetting)
                    .Include(p => p.User)
                    .FirstOrDefaultAsync(x => x.Id == pollId);

            return await _db.Polls.Include(p => p.PollSetting)
                .Include(p => p.User)
                .FirstOrDefaultAsync(x => x.Id == pollId);
        }

        public async Task<IList<Poll>> GetPollsSince(DateTime date, string tenantId)
        {
            if (tenantId != null)
                return await _db.Polls.IgnoreQueryFilters()
                    .Where(x => x.Deadline >= date && x.TenantId == tenantId)
                    .ToListAsync();
            return await _db.Polls.Where(x => x.Deadline >= date).ToListAsync();
        }

        public async Task<IList<Poll>> GetActivePolls(bool ignoreTenantId, string tenantId)
        {
            return await GetActivePollsQuery(ignoreTenantId, tenantId).ToListAsync();
        }

        public async Task EndPoll(long pollId)
        {
            var poll = await _db.Polls.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == pollId);
            if (poll != null)
            {
                poll.Deadline = DateTime.UtcNow;
                poll.Active = false;
                await _db.SaveChangesAsync();
            }
        }

        public async Task<IList<Poll>> GetUserVotedPolls(string userId)
        {
            var pollIds = _db.Votes.Where(x => x.VoterId == userId).Select(v => v.PollId);

            return await _db.Polls.Where(p => pollIds.Contains(p.Id) && p.Active).ToListAsync();
        }

        public async Task SetPollResult(long pollId, string result)
        {
            var getPoll = await _db.Polls.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == pollId);
            if (getPoll != null)
            {
                getPoll.Result = result;
                await _db.SaveChangesAsync();
            }
        }

        public async Task<IEnumerable<Poll>> GetCompletedPolls(int? count)
        {
            var polls = GetCompletedPollQuery();
            if (count.HasValue) return await polls.Take(count.Value).ToListAsync();

            return await polls.ToListAsync();
        }

        public async Task<int> GetCompletedPollCount(bool ignoreTenant = false, string tenantId = null)
        {
            return await GetCompletedPollQuery(ignoreTenant, tenantId).CountAsync();
        }

        public async Task<int> GetActivePollCount(bool ignoreTenant = false, string tenantId = null)
        {
            return await GetActivePollsQuery(ignoreTenant, tenantId).CountAsync();
        }


        public async Task<IList<Poll>> GetAllPolls()
        {
            return await _db.Polls.ToListAsync();
        }

        public async Task<IList<Poll>> GetPublicPolls()
        {
            return await _db.Polls.IgnoreQueryFilters().Include(x => x.User).ToListAsync();
        }

        public IQueryable<dynamic> GetTenantSummaryQueryWithDetail()
        {
            var query = (from t in _tenantsDb.Tenants.IgnoreQueryFilters()
                join u in _db.Users.IgnoreQueryFilters() on t.Id equals u.TenantId into tu
                join p in _db.Polls.IgnoreQueryFilters() on t.Id equals p.TenantId into yp
                from tus in tu.DefaultIfEmpty()
                from ypo in yp.DefaultIfEmpty()
                group new {t, tus, ypo} by new {t.Id, tus.TenantId, PollTenant = ypo.TenantId}
                into grouped
                select new
                {
                    Tenant = grouped.Key.Id,
                    UserCount = grouped.Select(x => x.tus).Distinct().Count(),
                    CompletedCount = grouped.Select(x => x.ypo).Where(a => a != null && !a.Active).Distinct().Count(),
                    ActiveCount = grouped.Select(x => x.ypo).Where(a => a != null && a.Active).Distinct().Count(),
                    IsActive = !grouped.FirstOrDefault(x => x.t.Id == grouped.Key.Id).t.InActive
                }).AsQueryable();

            return query;
        }

        public async Task<T> GetLastPollOfType<T>(string tenantId) where T : Poll
        {
            if (tenantId != null)
                return await _db.Polls.IgnoreQueryFilters()
                    .OfType<T>()
                    .OrderByDescending(x => x.CreateTime)
                    .FirstOrDefaultAsync(x => !x.Active && x.TenantId == tenantId);
            return await _db.Polls.OfType<T>().OrderByDescending(x => x.CreateTime).FirstOrDefaultAsync(x => !x.Active);
        }

        public async Task<bool> HasActivePollOfType<T>() where T : Poll
        {
            return await _db.Polls.OfType<T>().AnyAsync(p => p.Active);
        }

        public IQueryable<dynamic> GetTenantSummaryQuery()
        {
            return _db.Users.IgnoreQueryFilters().GroupBy(x => x.TenantId);
        }

        public async Task<IList<ApplicationUser>> GetNotVotedUsers(long pollId, string tenantId = null)
        {
            var poll = await GetPoll(pollId, true);

            var voters = _db.Votes.Where(x => x.PollId == pollId);
            var users = _db.Users.Include(x => x.UserDetail).AsQueryable();
            if (tenantId != null)
            {
                voters = voters.IgnoreQueryFilters().Where(x => x.TenantId == tenantId);
                users = users.IgnoreQueryFilters().Where(x => x.TenantId == tenantId && !x.IsDeleted);
            }

            if (poll != null && poll.PollType != PollTypes.AuthorityPoll)
                users = users.Where(u => u.UserDetail.AuthorityPercent > 0);

            var voterIds = voters.Select(x => x.VoterId).Distinct();

            users = users.Where(x => !voterIds.Contains(x.Id));

            return await users.ToListAsync();
        }

        public async Task DeletePoll(long pollId)
        {
            var poll = await GetPoll(pollId);
            if (poll != null)
            {
                _db.Polls.Remove(poll);
                await _db.SaveChangesAsync();
            }
        }

        public async Task<int> GetPollCountByType<T>(string tenantId) where T : Poll
        {
            if (string.IsNullOrEmpty(tenantId)) return await _db.Polls.OfType<T>().CountAsync();

            return await _db.Polls.IgnoreQueryFilters()
                .OfType<T>()
                .Where(x => x.TenantId == tenantId)
                .CountAsync();
        }

        #region CommonQueries

        private IQueryable<Poll> GetCompletedPollQuery(bool ignoreFilters = false, string tenantId = null)
        {
            var query = _db.Polls.Include(p => p.User).Where(x => !x.Active && x.Deadline < DateTime.UtcNow);
            if (ignoreFilters) query = query.IgnoreQueryFilters();

            if (tenantId != null) query = query.IgnoreQueryFilters().Where(x => x.TenantId == tenantId);
            return query.OrderByDescending(x => x.Deadline);
        }

        private IQueryable<Poll> GetActivePollsQuery(bool ignoreTenantId, string tenantId = null)
        {
            var polls = _db.Polls.Where(x => x.Active);
            if (ignoreTenantId || tenantId != null) polls = polls.IgnoreQueryFilters();
            if (tenantId != null) polls = polls.Where(x => x.TenantId == tenantId);
            return polls;
        }

        #endregion
    }
}