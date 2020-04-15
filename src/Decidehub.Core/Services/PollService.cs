using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Decidehub.Core.Entities;
using Decidehub.Core.Enums;
using Decidehub.Core.Extensions;
using Decidehub.Core.Interfaces;
using Decidehub.Core.Models;
using Hangfire;
using Newtonsoft.Json;

namespace Decidehub.Core.Services
{
    public class PollService : IPollService
    {
        private readonly IEmailSender _emailSender;
        private readonly IGenericService _genericService;
        private readonly IPollRepository _pollRepository;
        private readonly ISettingService _settingService;
        private readonly ITenantService _tenantService;
        private readonly IUserService _userService;
        private readonly IVoteService _voteService;

        public PollService(IPollRepository pollRepository, ISettingService settingService, IUserService userService,
            ITenantService tenantService, IVoteService voteService, IEmailSender emailSender,
            IGenericService genericService)
        {
            _pollRepository = pollRepository;
            _settingService = settingService;
            _userService = userService;
            _tenantService = tenantService;
            _voteService = voteService;
            _emailSender = emailSender;
            _genericService = genericService;
        }

        public async Task<Poll> AddPoll(Poll poll)
        {
            var settings = (await _settingService.GetSettings(poll.TenantId)).ToList();
            var pollSettings = settings.Select(x => x.Clone()).ToList();

            var votingDurationVal = await _settingService.GetVotingDurationByPollType(poll.PollType, poll.TenantId);
            poll.Deadline = DateTime.UtcNow.AddHours(votingDurationVal);

            if (poll.PollType != PollTypes.PolicyChangePoll && poll.PollType != PollTypes.MultipleChoicePoll)
                if (poll.PollType == PollTypes.AuthorityPoll)
                {
                    var getVotingSetting =
                        pollSettings.FirstOrDefault(x => x.Key == Settings.VotingDuration.ToString());
                    if (getVotingSetting != null)
                        getVotingSetting.Value = votingDurationVal.ToString(CultureInfo.InvariantCulture);
                }

            poll.PollSetting = new PollSetting
            {
                SettingJsonString = JsonConvert.SerializeObject(pollSettings),
                TenantId = poll.TenantId
            };

            await _pollRepository.AddPoll(poll);

            return poll;
        }

        public async Task<bool> HasActivePollOfType<T>() where T : Poll
        {
            return await _pollRepository.HasActivePollOfType<T>();
        }

        public async Task<IList<Poll>> GetUserNotVotedPolls(string userId)
        {
            return await _pollRepository.GetUserNotVotedPolls(userId);
        }

        public async Task<Poll> GetPoll(long pollId, bool ignoreTenant)
        {
            return await _pollRepository.GetPoll(pollId, ignoreTenant);
        }

        public async Task<IList<Poll>> GetActivePolls(bool ignoreTenantId)
        {
            return await _pollRepository.GetActivePolls(ignoreTenantId);
        }

        public async Task EndPoll(long pollId)
        {
            await _pollRepository.EndPoll(pollId);
        }

        public async Task<IList<Poll>> GetUserVotedPolls(string userId)
        {
            return await _pollRepository.GetUserVotedPolls(userId);
        }

        public async Task SetPollResult(long pollId, string result)
        {
            await _pollRepository.SetPollResult(pollId, result);
        }

        public async Task NotifyUsers(PollTypes pollType, PollNotificationTypes pollNotificationType, Poll poll)
        {
            var tenant = await _tenantService.GetTenant(poll.TenantId);
            if (tenant == null) return;
            var msg = "";
            var msgEn = "";
            var baseUrl = await _genericService.GetBaseUrl(poll.TenantId);
            var langDic = new Dictionary<string, EmailDetailModel>();
            var pollTypeStr = pollType.DescriptionLang("tr");
            var pollTypeStrEn = pollType.DescriptionLang("en");

            switch (pollNotificationType)
            {
                case PollNotificationTypes.Started:
                    msg = $"<b>{poll.Name}</b> adlı oylama başlamıştır. "
                          + $"Oylamaya <a target =\"_blank\" href=\"{baseUrl}/polls\" style=\"font-weight: bold; color: #2F2F2F; cursor: pointer;\">"
                          + "buraya</a> tıklayarak ulaşabilirsiniz. ";


                    msgEn = $"<b>{poll.Name}</b> has started. "
                            + $"In order to see poll details <a target =\"_blank\" href=\"{baseUrl}/polls\" style=\"font-weight: bold; color: #2F2F2F; cursor: pointer;\">"
                            + "click here</a> ";
                    break;
                case PollNotificationTypes.Ended when pollType == PollTypes.AuthorityPoll:
                    msg =
                        $" <b>{poll.Name}</b> adlı oylama sonuçlanmıştır.<br/> Sonuçlara <a target=\"_blank\" href=\""
                        + $"{baseUrl}/polls\" style=\"font-weight: bold; color: #2F2F2F; cursor: pointer;\">buraya</a> tıklayarak ulaşabilirsiniz.";

                    msgEn =
                        $"Poll titled <b>{poll.Name}</b> has ended.<br/> In order to see the result <a target=\"_blank\" href=\""
                        + $"{baseUrl}/polls\" style=\"font-weight: bold; color: #2F2F2F; cursor: pointer;\"> click here</a>";
                    break;
                case PollNotificationTypes.Ended:
                {
                    string pollResEn;
                    var pollRes = pollResEn = poll.Result;


                    var res = Enum.TryParse(poll.Result, out PollResults result);
                    if (res)
                    {
                        pollRes = result.DescriptionLang("tr");
                        pollResEn = result.DescriptionLang("en");
                    }

                    msg = $"<strong>{poll.Name}</strong> adlı oylama, aşağıdaki şekilde sonuçlanmıştır. <br/>"
                          + $"<b>{pollRes}</b>";

                    msgEn = $"<strong>{poll.Name}</strong> named poll is resulted as " + $"<b>{pollResEn}</b>";
                    break;
                }
                case PollNotificationTypes.AboutToEnd:
                    msg = $"<strong>{poll.Name}</strong> adlı oylama yakında sonlanacaktır.<br/>"
                          + $"Oyunuzu kullanmak için <a target =\"_blank\" href=\"{baseUrl}/polls\" style=\"font-weight: bold; color: #2F2F2F; cursor: pointer;\">"
                          + "buraya</a> tıklayınız.";

                    msgEn = $"<strong>{poll.Name}</strong> named poll soon to be completed. <br/>"
                            + $"In order to vote <a target =\"_blank\" href=\"{baseUrl}/polls\" style=\"font-weight: bold; color: #2F2F2F; cursor: pointer;\">"
                            + "click here</a> .";
                    break;
            }

            langDic.Add("en", new EmailDetailModel {Subject = pollTypeStrEn, Message = msgEn});
            langDic.Add("tr", new EmailDetailModel {Subject = pollTypeStr, Message = msg});

            if (pollNotificationType != PollNotificationTypes.AboutToEnd)
            {
                if (pollType == PollTypes.AuthorityPoll)
                    BackgroundJob.Enqueue(() => _userService.SendEmailToAllUsers(poll.TenantId, langDic));
                else
                    BackgroundJob.Enqueue(() => _userService.SendEmailToVoters(poll.TenantId, langDic));
            }
            else
            {
                BackgroundJob.Enqueue(() => SendEmailToNotVotedUsers(poll.TenantId, langDic, poll.Id));
            }
        }

        public async Task<IList<Poll>> GetCompletedPolls(int? count)
        {
            return (await _pollRepository.GetCompletedPolls(count)).ToList();
        }

        public async Task<bool> UserCanVote(long pollId, string userId)
        {
            var canVote = false;
            var user = await _userService.GetUserById(userId);
            var poll = await GetPoll(pollId, false);
            if (poll != null && user != null)
                if (user.CreatedAt <= poll.CreateTime)
                    canVote = true;

            return canVote;
        }

        public async Task<int> GetCompletedPollCount(bool ignoreTenant = false, string tenantId = null)
        {
            return await _pollRepository.GetCompletedPollCount(ignoreTenant, tenantId);
        }

        public async Task<int> GetActivePollCount(bool ignoreTenant = false, string tenantId = null)
        {
            return await _pollRepository.GetActivePollCount(ignoreTenant, tenantId);
        }

        public async Task DeletePoll(long id)
        {
            await _voteService.DeleteVote(id);
            await _pollRepository.DeletePoll(id);
        }

        public async Task<bool> IsNotVotedPoll(string userId, Poll poll)
        {
            var voted = await _voteService.UserVotedInPoll(userId, poll.Id);
            return !voted && poll.Active;
        }

        public async Task<bool> IsVotedPoll(string userId, Poll poll)
        {
            return await _voteService.UserVotedInPoll(userId, poll.Id);
        }

        public bool IsCompleted(Poll poll)
        {
            var completed = false;
            if (poll.PollType == PollTypes.AuthorityPoll && !poll.Active)
                completed = true;
            else if (!poll.Active && poll.Deadline < DateTime.UtcNow) completed = true;

            return completed;
        }

        public async Task<IList<Poll>> GetPublicPolls()
        {
            return await _pollRepository.GetPublicPolls();
        }

        public async Task SendDirectEmailToNotVotedUsers(string tenantId, string subject, string message, long pollId)
        {
            var users = await _pollRepository.GetNotVotedUsers(pollId, tenantId);
            foreach (var user in users) await _emailSender.SendEmailAsync(user.Email, subject, message, tenantId);
        }

        public async Task SendDirectEmailToNotVotedUser(string tenantId, string subject, string message, string userId)
        {
            var user = await _userService.GetUserByIdAndTenant(userId, tenantId);
            if (user != null) await _emailSender.SendEmailAsync(user.Email, subject, message, tenantId);
        }

        public async Task<int> GetPollCountByType<T>(string tenantId) where T : Poll
        {
            return await _pollRepository.GetPollCountByType<T>(tenantId);
        }

        public async Task<double> GetPollVotingDuration(long pollId, bool ignoreTenant = false)
        {
            var getPoll = await GetPoll(pollId, ignoreTenant);
            if (getPoll.PollType == PollTypes.AuthorityPoll) return 24 * 3;

            var val = await GetPollSettingValueByType(getPoll, Settings.VotingDuration);
            return Convert.ToDouble(val);
        }


        public async Task<int> GetPollRequiredUserPercentage(long pollId, bool ignoreTenant = false)
        {
            var getPoll = await GetPoll(pollId, ignoreTenant);
            var val = await GetPollSettingValueByType(getPoll, Settings.AuthorityVotingRequiredUserPercentage);
            return Convert.ToInt32(val);
        }

        public async Task<T> GetLastPollOfType<T>(string tenantId) where T : Poll
        {
            return await _pollRepository.GetLastPollOfType<T>(tenantId);
        }

        private async Task SendEmailToNotVotedUsers(string tenantId,
            IReadOnlyDictionary<string, EmailDetailModel> langDic, long pollId)
        {
            var users = await _pollRepository.GetNotVotedUsers(pollId, tenantId);
            foreach (var user in users)
            {
                var lang = user.UserDetail.LanguagePreference ?? "tr";
                var langInfo = langDic[lang];

                await _emailSender.SendEmailAsync(user.Email, langInfo.Subject, langInfo.Message, tenantId);
            }
        }

        public async Task<IList<Poll>> GetAllPolls()
        {
            return await _pollRepository.GetAllPolls();
        }

        private async Task<string> GetPollSettingValueByType(Poll poll, Settings settingType)
        {
            var settings = poll?.PollSetting != null
                ? JsonConvert.DeserializeObject<List<Setting>>(poll.PollSetting.SettingJsonString)
                : (await _settingService.GetSettings(null)).ToList();

            var setting = settings.FirstOrDefault(x => x.Key == settingType.ToString());
            return setting?.Value;
        }
    }
}