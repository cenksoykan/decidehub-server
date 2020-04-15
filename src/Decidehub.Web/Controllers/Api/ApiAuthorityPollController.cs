using System;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Decidehub.Core.Entities;
using Decidehub.Core.Enums;
using Decidehub.Core.Interfaces;
using Decidehub.Web.Extensions;
using Decidehub.Web.Helpers;
using Decidehub.Web.Interfaces;
using Decidehub.Web.ViewModels.Api;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace Decidehub.Web.Controllers.Api
{
    /// <inheritdoc />
    [Route("api/v1/poll/AuthorityPoll")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class ApiAuthorityPollController : Controller
    {
        private readonly IStringLocalizer<ApiAuthorityPollController> _localizer;
        private readonly IMapper _mapper;
        private readonly IPollService _pollService;
        private readonly IPollApiViewModelService _pollViewModelService;
        private readonly ISettingService _settingService;
        private readonly ITenantProvider _tenantProvider;
        private readonly IUserService _userService;
        private readonly IVoteService _voteService;

        public ApiAuthorityPollController(IUserService userService, IPollService pollService,
            IPollApiViewModelService pollViewModelService, IMapper mapper, IVoteService voteService,
            IStringLocalizer<ApiAuthorityPollController> localizer, ISettingService settingService,
            ITenantProvider tenantProvider)
        {
            _userService = userService;
            _pollService = pollService;
            _pollViewModelService = pollViewModelService;
            _mapper = mapper;
            _voteService = voteService;
            _localizer = localizer;
            _settingService = settingService;
            _tenantProvider = tenantProvider;
        }

        /// <summary>
        ///     Starts new AuthorityPoll
        /// </summary>
        /// <returns> started poll info</returns>
        /// <response code="200"> started poll info </response>
        /// <response code="400">Error model</response>
        [ProducesResponseType(200)]
        [ProducesResponseType(typeof(ErrorViewModel), 400)]
        [Route("startPoll")]
        [HttpGet]
        public async Task<IActionResult> NewAuthorityPoll()
        {
            var model = new AuthorityPollViewModel();
            if (ModelState.ContainsKey("Name"))
                ModelState["Name"].Errors.Clear();
            var pollCount = await _pollService.GetPollCountByType<AuthorityPoll>() + 1;
            model.Name = $"{pollCount}.{_localizer["AuthorityDistPoll"]}";

            model.UserId = User.ApiGetUserId();
            var adminRole = await _userService.UserInRole(User.ApiGetUserId(), "Admin");

            if (!adminRole)
            {
                ModelState.AddModelError("", _localizer["AuthorityPollUserAdminError"]);
                return BadRequest(Errors.GetErrorList(ModelState));
            }

            if (await _userService.GetMaxInitialAuthorityPercent() > 1 / 3M)
            {
                ModelState.AddModelError("", _localizer["AuthorityPollUserInitialAuthorityError"]);
                return BadRequest(Errors.GetErrorList(ModelState));
            }

            if (await _pollService.HasActivePollOfType<AuthorityPoll>())
            {
                ModelState.AddModelError("", _localizer["AuthorityPollActivePollError"]);
                return BadRequest(Errors.GetErrorList(ModelState));
            }

            var poll = await _pollViewModelService.NewAuthorityPoll(model);

            await _pollService.NotifyUsers(poll.PollType, PollNotificationTypes.Started, poll);

            return Ok(_mapper.Map<AuthorityPoll, PollListViewModel>(poll));
        }

        /// <summary>
        ///     Adds user votes for authorityPoll
        /// </summary>
        /// <param name="model">model containing PollId, VoterId, Votes list.</param>
        /// <returns> voted poll info</returns>
        /// <response code="200"> voted poll info </response>
        /// <response code="400">Error model</response>
        [ProducesResponseType(200)]
        [ProducesResponseType(typeof(ErrorViewModel), 400)]
        [Route("vote")]
        [HttpPost]
        public async Task<IActionResult> Vote([FromBody] AuthorityPollSaveViewModel model)
        {
            if (ModelState.IsValid)
            {
                var voterId = User.ApiGetUserId();
                var poll = await _pollService.GetPoll(model.PollId);
                if (poll == null)
                {
                    ModelState.AddModelError("", _localizer["PollNotFound"]);
                    return BadRequest(Errors.GetErrorList(ModelState));
                }

                if (poll.GetType() != typeof(AuthorityPoll))
                    return BadRequest(Errors.GetSingleErrorList("", _localizer["PollTypeNotAuthority"]));

                if (!poll.Active)
                {
                    ModelState.AddModelError("", _localizer["PollCompleted"]);
                    return BadRequest(Errors.GetErrorList(ModelState));
                }

                if (!await _pollService.UserCanVote(poll.Id, voterId))
                    return BadRequest(Errors.GetSingleErrorList("", _localizer["UserCannotVoteAfterAddedPollStart"]));

                var userVoted = await _voteService.UserVotedInPoll(voterId, model.PollId);
                if (userVoted)
                {
                    ModelState.AddModelError("", _localizer["PollRecordExist"]);
                    return BadRequest(Errors.GetErrorList(ModelState));
                }

                if (model.Votes.Sum(v => v.Value) != 1000)
                {
                    ModelState.AddModelError("", _localizer["AuthoriyPollSumError"]);
                    return BadRequest(Errors.GetErrorList(ModelState));
                }

                await _pollViewModelService.SaveAuthorityVote(model, voterId);
                var result = _mapper.Map<Poll, PollListViewModel>(poll);
                result.UserVoted = true;
                return Ok(result);
            }

            return BadRequest(Errors.GetErrorList(ModelState));
        }

        /// <summary>
        ///     Gets Next Authority Poll Start Date
        /// </summary>
        /// <returns>Date</returns>
        /// <response code="200">Date</response>
        /// <response code="400">Error model</response>
        [ProducesResponseType(typeof(ErrorViewModel), 400)]
        [ProducesResponseType(typeof(DateTime), 200)]
        [Route("nextDate")]
        [HttpGet]
        public async Task<IActionResult> GetNextAuthorityPollStartDate()
        {
            var getLatestVote = await _pollService.GetLastPollOfType<AuthorityPoll>();
            var hasActivePoll = await _pollService.HasActivePollOfType<AuthorityPoll>();
            if (getLatestVote == null || hasActivePoll) return Ok(null);

            var votingFreq =
                await _settingService.GetSettingValueByType(Settings.VotingFrequency, _tenantProvider.GetTenantId());

            var votingFreqVal = Convert.ToInt32(votingFreq.Value) * 24;

            var nextPollStart = getLatestVote.Deadline.AddHours(votingFreqVal);
            return Ok(nextPollStart);
        }
    }
}