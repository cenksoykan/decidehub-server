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
    [Route("api/v1/poll/sharePoll")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class ApiSharePollController : Controller
    {
        private readonly IStringLocalizer<ApiSharePollController> _localizer;
        private readonly IMapper _mapper;
        private readonly IPollService _pollService;
        private readonly IPollApiViewModelService _pollViewModelService;
        private readonly IUserService _userService;
        private readonly IVoteService _voteService;

        public ApiSharePollController(IPollService pollService, IPollApiViewModelService pollViewModelService,
            IMapper mapper, IVoteService voteService, IUserService userService,
            IStringLocalizer<ApiSharePollController> localizer)
        {
            _pollService = pollService;
            _pollViewModelService = pollViewModelService;
            _mapper = mapper;
            _voteService = voteService;
            _userService = userService;
            _localizer = localizer;
        }

        /// <summary>
        ///     Starts new SharePoll
        /// </summary>
        /// <returns> started poll info</returns>
        /// <response code="200"> started poll info </response>
        /// <response code="400">Error model</response>
        [ProducesResponseType(200)]
        [ProducesResponseType(typeof(ErrorViewModel), 400)]
        [HttpPost]
        [Route("startPoll")]
        public async Task<IActionResult> NewSharePoll([FromBody] SharePollViewModel model)
        {
            model.UserId = User.ApiGetUserId();

            var latestAuthorityPoll = await _pollService.GetLastPollOfType<AuthorityPoll>();
            if (latestAuthorityPoll == null)
            {
                ModelState.AddModelError("", _localizer["CantStartPollBeforeAuthorityComplete"]);
                return BadRequest(Errors.GetErrorList(ModelState));
            }

            if (latestAuthorityPoll.Result == "Kararsız" ||
                latestAuthorityPoll.Result == PollResults.Undecided.ToString())
            {
                ModelState.AddModelError("", _localizer["CantStartPollBeforeAuthorityComplete"]);
                return BadRequest(Errors.GetErrorList(ModelState));
            }

            if (await _pollService.HasActivePollOfType<AuthorityPoll>())
            {
                ModelState.AddModelError("", _localizer["AuthorityPollActivePollError"]);
                return BadRequest(Errors.GetErrorList(ModelState));
            }

            var poll = await _pollViewModelService.NewSharePoll(model);

            await _pollService.NotifyUsers(poll.PollType, PollNotificationTypes.Started, poll);

            return Ok(_mapper.Map<SharePoll, PollListViewModel>((SharePoll) poll));
        }

        /// <summary>
        ///     Adds user votes for sharePoll
        /// </summary>
        /// <param name="model">model containing PollId, UserId, Users list(containing votes for each users).</param>
        /// <returns> voted poll info</returns>
        /// <response code="200"> voted poll info </response>
        /// <response code="400">Error model</response>
        [ProducesResponseType(200)]
        [ProducesResponseType(typeof(ErrorViewModel), 400)]
        [Route("vote")]
        [HttpPost]
        public async Task<IActionResult> Vote([FromBody] SharePollVoteModel model)
        {
            if (ModelState.IsValid)
            {
                var poll = await _pollService.GetPoll(model.PollId);
                var userId = User.ApiGetUserId();
                if (poll == null)
                {
                    ModelState.AddModelError("", _localizer["PollNotFound"]);
                    return BadRequest(Errors.GetErrorList(ModelState));
                }

                if (poll.Deadline <= DateTime.UtcNow)
                {
                    ModelState.AddModelError("", _localizer["PollCompleted"]);
                    return BadRequest(Errors.GetErrorList(ModelState));
                }

                if (!await _pollService.UserCanVote(poll.Id, User.ApiGetUserId()))
                    return BadRequest(Errors.GetSingleErrorList("", _localizer["UserCannotVoteAfterAddedPollStart"]));

                if (!await _userService.IsVoter(User.ApiGetUserId()))
                    return BadRequest(Errors.GetSingleErrorList("", _localizer["PollVoterError"]));

                //check if the user voted in poll
                var userVoted = await _voteService.UserVotedInPoll(userId, model.PollId);
                if (userVoted)
                {
                    ModelState.AddModelError("", _localizer["PollRecordExist"]);
                    return BadRequest(Errors.GetErrorList(ModelState));
                }

                if (model.Options.Sum(v => v.SharePercent) != 100)
                {
                    ModelState.AddModelError("", _localizer["VoteSumError"]);
                    return BadRequest(Errors.GetErrorList(ModelState));
                }

                model.UserId = userId;
                await _pollViewModelService.SaveSharePollVotes(model);
                var result = _mapper.Map<Poll, PollListViewModel>(poll);
                result.UserVoted = true;
                return Ok(result);
            }

            return BadRequest(Errors.GetErrorList(ModelState));
        }
    }
}