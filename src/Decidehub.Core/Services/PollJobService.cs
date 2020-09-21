using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Decidehub.Core.Entities;
using Decidehub.Core.Enums;
using Decidehub.Core.Interfaces;
using MoreLinq;
using Newtonsoft.Json;

namespace Decidehub.Core.Services
{
    public class PollJobService : IPollJobService
    {
        private static readonly SemaphoreSlim PollFinishedSemaphore = new SemaphoreSlim(1, 1);
        private static readonly SemaphoreSlim AuthorityPollStartSemaphore = new SemaphoreSlim(1, 1);
        private readonly IPolicyService _policyService;
        private readonly IPollRepository _pollRepository;
        private readonly IPollService _pollService;
        private readonly ISettingService _settingService;
        private readonly ITenantService _tenantService;
        private readonly IUserService _userService;
        private readonly IVoteService _voteService;

        public PollJobService(IPollService pollService, IUserService userService, IVoteService voteService,
            ISettingService settingService, IPollRepository pollRepository, ITenantService tenantService,
            IPolicyService policyService)
        {
            _pollService = pollService;
            _userService = userService;
            _voteService = voteService;
            _settingService = settingService;
            _pollRepository = pollRepository;
            _tenantService = tenantService;
            _policyService = policyService;
        }


        #region Helpers

        private async Task AddNewAuthorityPoll(string tenant)
        {
            var lang = await _tenantService.GetTenantLang(tenant);
            var pollCount = await _pollService.GetPollCountByType<AuthorityPoll>(tenant) + 1;
            var poll = new AuthorityPoll
            {
                Active = true,
                CreateTime = DateTime.UtcNow,
                Name = lang == "tr"
                    ? $"{pollCount}.Yetki Dağılımı Oylaması"
                    : $"{pollCount}.Authority Distrubition Poll",
                TenantId = tenant
            };

            var addedPoll = await _pollService.AddPoll(poll);

            await _pollService.NotifyUsers(addedPoll.PollType, PollNotificationTypes.Started, addedPoll);
        }

        #endregion

        #region PollCompletionMethods

        public async Task CheckPollCompletion()
        {
            if (!await PollFinishedSemaphore.WaitAsync(1000)) return;
            try
            {
                var activePolls = await _pollService.GetActivePolls(true);
                var now = DateTime.UtcNow;
                foreach (var poll in activePolls)
                {
                    var voterCount = poll.PollType == PollTypes.AuthorityPoll
                        ? (await _userService.GetUsersAsync(poll.TenantId)).Count(u => u.EmailConfirmed)
                        : await _userService.GetVoterCount(poll.TenantId);

                    await CheckRemainingTime(poll, now);

                    if (!await VotingComplete(poll, voterCount)) continue;

                    var votes = (await _voteService.GetVotesByPoll(poll.Id)).ToList();

                    await _pollService.EndPoll(poll.Id);

                    if (poll.PollType == PollTypes.AuthorityPoll || await ExpectedAuthorityVoted(poll))
                    {
                        switch (poll.PollType)
                        {
                            case PollTypes.AuthorityPoll:
                                await CalculateAuthorityScore(poll, votes);
                                break;
                            case PollTypes.MultipleChoicePoll:
                                await CalculateChoicePercentage(poll, votes);
                                break;
                            case PollTypes.SharePoll:
                                await CalculateShares(poll, votes);
                                break;
                            case PollTypes.PolicyChangePoll:
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                    }
                    else
                    {
                        var result = PollResults.InsufficientAuthority.ToString();
                        await _pollService.SetPollResult(poll.Id, result);
                    }

                    if (poll.PolicyId != null)
                    {
                        await CalculatePolicyChangePercentage(poll.Id, poll.PolicyId.Value, votes);
                    }

                    await _pollService.NotifyUsers(poll.PollType, PollNotificationTypes.Ended, poll);
                }
            }
            catch (Exception)
            {
                // ignored
            }
            finally
            {
                PollFinishedSemaphore.Release();
            }
        }

        private async Task CheckRemainingTime(Poll poll, DateTime now)
        {
            var remainingMinutes = (int) (poll.Deadline - now).TotalMinutes;
            var thirtyPercentMin = (int) (await _pollService.GetPollVotingDuration(poll.Id, true) * 60 * 30 / 100);

            if (remainingMinutes == thirtyPercentMin)
                await _pollService.NotifyUsers(poll.PollType, PollNotificationTypes.AboutToEnd, poll);
        }

        private async Task CalculateShares(Poll poll, IList<Vote> votes)
        {
            var options = JsonConvert.DeserializeObject<List<string>>(poll.OptionsJsonString);

            var sharePercents = new Dictionary<string, decimal>();

            foreach (var option in options)
                sharePercents[option] = ShareMultiplier(votes.Where(v => v.VotedUserId == option));

            var totalShare = sharePercents.Sum(s => s.Value);
            if (totalShare <= 0) totalShare = 1;
            var shareMultiplier = 1.0M / totalShare;

            var results = options.Select(option =>
                    $"{option}: {(sharePercents[option] * shareMultiplier).ToString("P", new CultureInfo("tr"))}")
                .ToList();
            await _pollService.SetPollResult(poll.Id, string.Join("\n", results));
        }

        private async Task CalculatePolicyChangePercentage(long pollId, long policyId, IReadOnlyCollection<Vote> votes)
        {
            string result;
            var totalParticipation = votes.Where(v => v.Value != null).Sum(c => c.Voter.UserDetail.AuthorityPercent);

            var accepted = false;
            if (totalParticipation != 0)
            {
                var positiveParticipation = votes.Where(v => v.Value > 0).Sum(p => p.Voter.UserDetail.AuthorityPercent);
                var percentage = positiveParticipation / totalParticipation * 100;

                result = percentage >= 50 ? PollResults.Positive.ToString() : PollResults.Negative.ToString();

                if (percentage >= 50)
                    accepted = true;
            }
            else
            {
                result = PollResults.InsufficientAuthority.ToString();
            }

            if (accepted)
                await _policyService.AcceptPolicy(policyId);
            else
                await _policyService.RejectPolicy(policyId);

            await _pollService.SetPollResult(pollId, result);
        }

        private async Task CalculateChoicePercentage(Poll poll, IReadOnlyCollection<Vote> votes)
        {
            string result;
            var totalParticipation = votes.Where(v => v.Value != null && v.Value != -1)
                .Sum(c => c.Voter.UserDetail.AuthorityPercent);
            if (totalParticipation != 0)
            {
                var optionPercentages = votes.Where(v => v.Value != null && v.Value != -1)
                    .GroupBy(v => v.Value)
                    .Select(g => new
                    {
                        OptionIndex = g.Key,
                        Value = Math.Round(g.Sum(x => x.Voter.UserDetail.AuthorityPercent) / totalParticipation, 2)
                                * 100
                    })
                    .ToList();


                if (optionPercentages.Any())
                {
                    var maxValue = optionPercentages.Max(p => p.Value);
                    if (optionPercentages.Count(p => p.Value == maxValue) > 1)
                    {
                        result = PollResults.Undecided.ToString();
                    }
                    else
                    {
                        var selectedOptionIndex = (int) optionPercentages.OrderByDescending(r => r.Value)
                            .First()
                            .OptionIndex.GetValueOrDefault();
                        var optionIndex = selectedOptionIndex;
                        var choice = JsonConvert.DeserializeObject<List<string>>(poll.OptionsJsonString)[optionIndex];
                        result = choice;
                    }
                }
                else
                {
                    result = PollResults.Undecided.ToString();
                }
            }
            else
            {
                result = PollResults.Undecided.ToString();
            }

            await _pollService.SetPollResult(poll.Id, result);
        }

        private async Task CalculateAuthorityScore(Poll poll, IEnumerable<Vote> votes)
        {
            var pollVotes = votes.Where(x => !x.Voter.IsDeleted).ToList();

            var pollVoters = pollVotes.Select(p => p.Voter).Distinct().ToList();
            var pollVoterIds = pollVoters.Select(p => p.Id).ToList();
            if (pollVoters.Count >= 3)
            {
                var finalScores = pollVoters.ToDictionary(v => v.Id, v => 0.0M);

                var scores = pollVoters.ToDictionary(v => v.Id, v => v.UserDetail.InitialAuthorityPercent);

                if (scores.All(s => s.Value <= 0)) scores = pollVoters.ToDictionary(v => v.Id, v => 100M);

                var allUsers = await _userService.GetUsersAsync(poll.TenantId);

                for (var round = 1; round <= 3; round++)
                {
                    var roundScores =
                        allUsers.ToDictionary(v => v.Id, v => 0.0M);

                    foreach (var vote in pollVotes.Where(v => v.VoterId != v.VotedUserId))
                    {
                        if (vote.VotedUserId == null
                            || !pollVoterIds.Contains(vote.VotedUserId))
                            continue;
                        var score = scores[vote.VoterId];
                        roundScores[vote.VotedUserId] += score * vote.Value.GetValueOrDefault() / 1000.0M;
                        finalScores[vote.VotedUserId] += score * vote.Value.GetValueOrDefault() / 3000.0M;
                    }

                    scores = roundScores;
                }

                var topScores = finalScores.OrderByDescending(s => s.Value).ToList();

                var totalAuthority = Math.Max(1, topScores.Sum(s => s.Value));

                await _userService.UpdateAuthorityPercents(
                    topScores.ToDictionary(s => s.Key, s => s.Value * 100.0M / totalAuthority), poll.TenantId);

                await _pollService.SetPollResult(poll.Id, PollResults.Completed.ToString());
            }
            else
            {
                await _pollService.SetPollResult(poll.Id, PollResults.InsufficientParticipation.ToString());
            }
        }


        private async Task<bool> ExpectedAuthorityVoted(Poll poll)
        {
            var votedPercentage = await _voteService.GetVotedAuthorityPercentageInPoll(poll);

            var getRequiredUserPercentage = await _pollService.GetPollRequiredUserPercentage(poll.Id, true);
            var getRequiredUserPercentageVal = getRequiredUserPercentage;
            return votedPercentage >= getRequiredUserPercentageVal;
        }

        private async Task<bool> VotingComplete(Poll poll, int voterCount)
        {
            var voteCount = await _voteService.GetVotedUserCountInPoll(poll.Id, true);
            var everyoneVoted = voterCount == voteCount;

            var now = DateTime.UtcNow;

            var deadlinePassed = poll.Deadline < now;

            var complete = everyoneVoted || deadlinePassed;

            return complete;
        }

        private static decimal ShareMultiplier(IEnumerable<Vote> pollVotes)
        {
            var pollVotesByVoted = pollVotes.DistinctBy(v => v.Voter)
                .Select(v => new {v.Voter.UserDetail.AuthorityPercent, Value = v.Value / 10.0M})
                .ToList();

            var pointSum = pollVotesByVoted.Sum(v => v.Value * v.AuthorityPercent).GetValueOrDefault();
            var authoritySum = pollVotesByVoted.Sum(g => (decimal?) g.AuthorityPercent).GetValueOrDefault();

            if (authoritySum <= 0) return 0;

            return pointSum / authoritySum;
        }

        #endregion

        #region AuthorityPollStart

        public async Task AuthorityPollStart()
        {
            try
            {
                if (!await AuthorityPollStartSemaphore.WaitAsync(1000)) return;
                var now = DateTime.UtcNow;
                var tenants = (await _tenantService.GetTenants()).ToList();

                foreach (var tenant in tenants)
                {
                    var getLatestPoll = await _pollService.GetLastPollOfType<AuthorityPoll>(tenant.Id);
                    if (getLatestPoll != null)
                    {
                        var votingFreq =
                            await _settingService.GetSettingValueByType(Settings.VotingFrequency, tenant.Id);

                        var votingFreqVal = Convert.ToDouble(votingFreq.Value) * 24;
                        var nextPollPreliminaryStart = getLatestPoll.Deadline.AddHours(votingFreqVal);

                        await CheckAuthorityPreliminaryMeeting(tenant.Id, now, nextPollPreliminaryStart);
                    }
                }

                AuthorityPollStartSemaphore.Release();
            }
            catch (Exception)
            {
                AuthorityPollStartSemaphore.Release();
            }
        }

        private async Task CheckAuthorityPreliminaryMeeting(string tenant, DateTime now, DateTime nextDate)
        {
            if ((await _pollRepository.GetPollsSince(nextDate, tenant)).Any(p => p.PollType == PollTypes.AuthorityPoll))
                return;

            if (now >= nextDate) await AddNewAuthorityPoll(tenant);
        }

        #endregion
    }
}