using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Decidehub.Core.Entities;
using Decidehub.Core.Enums;
using Decidehub.Core.Identity;
using Decidehub.Core.Interfaces;
using Decidehub.Web.Interfaces;
using Decidehub.Web.ViewModels.Api;
using Newtonsoft.Json;

namespace Decidehub.Web.Services
{
    public class PollApiViewModelService : IPollApiViewModelService
    {
        private readonly IMapper _mapper;
        private readonly IPollService _pollService;
        private readonly ISettingService _settingService;
        private readonly ITenantProvider _tenantProvider;
        private readonly IUserService _userService;
        private readonly IVoteService _voteService;

        public PollApiViewModelService(ITenantProvider tenantProvider, IPollService pollService, IMapper mapper,
            IVoteService voteService, IUserService userService, ISettingService settingService)
        {
            _tenantProvider = tenantProvider;
            _pollService = pollService;
            _mapper = mapper;
            _voteService = voteService;
            _userService = userService;
            _settingService = settingService;
        }

        public async Task<PolicyChangePoll> NewPolicyChangePoll(PolicyChangePollViewModel model)
        {
            var poll = new PolicyChangePoll
            {
                UserId = model.UserId,
                CreateTime = DateTime.UtcNow,
                Active = true,
                Name = model.Name,
                QuestionBody = model.Description,
                TenantId = _tenantProvider.GetTenantId(),
                PolicyId = model.PolicyId
            };
            await _pollService.AddPoll(poll);
            return poll;
        }

        public async Task<AuthorityPoll> NewAuthorityPoll(AuthorityPollViewModel model)
        {
            var poll = new AuthorityPoll
            {
                UserId = model.UserId,
                CreateTime = DateTime.UtcNow,
                Active = true,
                Name = model.Name,
                TenantId = _tenantProvider.GetTenantId()
            };
            await _pollService.AddPoll(poll);
            return poll;
        }

        public async Task<Poll> NewMultipleChoicePoll(MultipleChoicePollViewModel model)
        {
            var poll = new MultipleChoicePoll
            {
                UserId = model.UserId,
                CreateTime = DateTime.UtcNow,
                Active = true,
                Name = model.Name,
                QuestionBody = model.Description,
                OptionsJsonString = JsonConvert.SerializeObject(model.Options),
                TenantId = _tenantProvider.GetTenantId()
            };
            await _pollService.AddPoll(poll);
            return poll;
        }

        public MultipleChoicePollViewModel MultipleChoicePollToViewModel(MultipleChoicePoll poll)
        {
            MultipleChoicePollViewModel model = null;
            if (poll != null) model = _mapper.Map<MultipleChoicePoll, MultipleChoicePollViewModel>(poll);

            return model;
        }

        public SharePollViewModel SharePollToViewModel(SharePoll poll)
        {
            SharePollViewModel model = null;
            if (poll != null) model = _mapper.Map<SharePoll, SharePollViewModel>(poll);

            return model;
        }

        public async Task SaveAuthorityVote(AuthorityPollSaveViewModel model, string voterId)
        {
            var votes = model.Votes.Select(item => new Vote
            {
                PollId = model.PollId, VoterId = voterId, VotedUserId = item.UserId, Value = item.Value
            });

            await _voteService.AddVotes(votes);
        }

        public async Task<Poll> NewSharePoll(SharePollViewModel model)
        {
            var poll = new SharePoll
            {
                UserId = model.UserId,
                CreateTime = DateTime.UtcNow,
                Active = true,
                Name = model.Name,
                TenantId = _tenantProvider.GetTenantId(),
                OptionsJsonString = JsonConvert.SerializeObject(model.Options),
                QuestionBody = model.Description
            };
            await _pollService.AddPoll(poll);
            return poll;
        }

        public async Task SaveSharePollVotes(SharePollVoteModel model)
        {
            var votes = model.Options.Select(item => new Vote
            {
                PollId = model.PollId,
                VoterId = model.UserId,
                VotedUserId = item.Option,
                Value = item.SharePercent
            });
            await _voteService.AddVotes(votes);
        }

        public async Task<AuthorityPollListViewModel> GetUsersForAuthorityVoting(AuthorityPoll poll)
        {
            var model = _mapper.Map<AuthorityPoll, AuthorityPollListViewModel>(poll);
            var users = await _userService.GetUsersWithImage();

            model.Users = _mapper.Map<List<ApplicationUser>, List<AuthorityPollUsersViewModel>>(users.ToList());
            return model;
        }

        public async Task<PollStatusViewModel> GetPollStatus(long pollId)
        {
            var model = new PollStatusViewModel();
            var poll = await _pollService.GetPoll(pollId);
            if (poll != null)
            {
                if (poll.PollType == PollTypes.SharePoll) model = new SharePollStatusViewModel();

                var allUsers = poll.PollType == PollTypes.AuthorityPoll
                    ? (await _userService.GetUsersAsync()).ToList()
                    : (await _userService.GetVoters(_tenantProvider.GetTenantId())).ToList();

                var voters = (await _voteService.GetVotesByPoll(pollId)).Select(x => x.VoterId).Distinct();
                model.NotVotedUsers = allUsers.Where(x => !voters.Contains(x.Id))
                    .Select(x => new {UserId = x.Id, UserName = $"{x.FirstName} {x.LastName}"})
                    .ToList();
                model.VotedUsers = allUsers.Where(x => voters.Contains(x.Id))
                    .Select(x => new {UserId = x.Id, UserName = $"{x.FirstName} {x.LastName}"})
                    .ToList();
                model.PollName = poll.Name;
                model.PollId = pollId;
            }

            return model;
        }

        public async Task<bool> CheckFirstAuthorityPoll(string userId)
        {
            var latestAuthorityVote = await _pollService.GetLastPollOfType<AuthorityPoll>();
            var hasActiveAuthorityPoll = await _pollService.HasActivePollOfType<AuthorityPoll>();
            var processedByRole = await _userService.GetUserRoles(userId);

            return (latestAuthorityVote == null || !hasActiveAuthorityPoll)
                   && processedByRole.Any(x => x.Name == "Admin");
        }

        public async Task<DateTime?> GetNextAuthorityPollStartDate()
        {
            DateTime? result;
            var getLatestVote = await _pollService.GetLastPollOfType<AuthorityPoll>();
            var hasActivePoll = await _pollService.HasActivePollOfType<AuthorityPoll>();
            if (getLatestVote == null || hasActivePoll)
            {
                result = null;
            }

            else
            {
                var votingFreq =
                    await _settingService.GetSettingValueByType(Settings.VotingFrequency,
                        _tenantProvider.GetTenantId());

                var votingFreqVal = Convert.ToInt32(votingFreq.Value) * 24;

                var nextPollStart = getLatestVote.Deadline.AddHours(votingFreqVal);
                result = nextPollStart;
            }

            return result;
        }
    }
}