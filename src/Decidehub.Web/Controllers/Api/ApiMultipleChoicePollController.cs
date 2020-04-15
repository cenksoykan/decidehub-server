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
    //  [Produces("application/json")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [Route("api/v1/poll/multipleChoice")]
    public class ApiMultipleChoicePollController : Controller
    {
        private readonly IStringLocalizer<ApiMultipleChoicePollController> _localizer;
        private readonly IMapper _mapper;
        private readonly IPollService _pollService;
        private readonly IPollApiViewModelService _pollViewModelService;
        private readonly IUserService _userService;
        private readonly IVoteService _voteService;

        public ApiMultipleChoicePollController(IPollService pollService, IPollApiViewModelService pollViewModelService,
            IMapper mapper, IVoteService voteService, IStringLocalizer<ApiMultipleChoicePollController> localizer,
            IUserService userService)
        {
            _pollService = pollService;
            _pollViewModelService = pollViewModelService;
            _mapper = mapper;
            _voteService = voteService;
            _localizer = localizer;
            _userService = userService;
        }

        /// <summary>
        ///     Starts new Multiple Choice Poll
        /// </summary>
        /// <param name="model">model containing poll name, question and string option array.</param>
        /// <returns>Started poll Info</returns>
        /// <response code="200">Started poll Info</response>
        /// <response code="400">Error model</response>
        [ProducesResponseType(200)]
        [ProducesResponseType(typeof(ErrorViewModel), 400)]
        [HttpPost]
        [Route("startPoll")]
        public async Task<IActionResult> NewMultipleChoicePoll([FromBody] MultipleChoicePollViewModel model)
        {
            if (ModelState.IsValid)
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

                if (model.Options.Count < 2 || model.Options.Any(string.IsNullOrEmpty))
                {
                    ModelState.AddModelError("", _localizer["PollOptionCountError"]);
                    return BadRequest(Errors.GetErrorList(ModelState));
                }

                var poll = await _pollViewModelService.NewMultipleChoicePoll(model);

                await _pollService.NotifyUsers(poll.PollType, PollNotificationTypes.Started, poll);
                var result = _pollViewModelService.MultipleChoicePollToViewModel((MultipleChoicePoll) poll);
                return Ok(result);
            }

            return BadRequest(Errors.GetErrorList(ModelState));
        }


        /// <summary>
        ///     Saves the users vote for MultipleChoicePoll
        /// </summary>
        /// <param name="model">model containing pollId and pollValue. PollValue must be the index of the returned option array</param>
        /// <returns>Voted poll Info</returns>
        /// <response code="200">Voted poll Info</response>
        /// <response code="400">Error model</response>
        [ProducesResponseType(typeof(PolicyChangePollViewModel), 200)]
        [ProducesResponseType(typeof(ErrorViewModel), 400)]
        [HttpPost]
        [Route("vote")]
        public async Task<IActionResult> Vote([FromBody] MultipleChoicePollVoteViewModel model)
        {
            if (ModelState.IsValid)
            {
                var poll = await _pollService.GetPoll(model.PollId);
                if (poll == null)
                {
                    ModelState.AddModelError("", _localizer["PollNotFound"]);
                    return BadRequest(Errors.GetErrorList(ModelState));
                }

                if (poll.GetType() != typeof(MultipleChoicePoll))
                    return BadRequest(Errors.GetSingleErrorList("", _localizer["PollTypeNotMultipleChoice"]));

                if (poll.Deadline <= DateTime.UtcNow)
                {
                    ModelState.AddModelError("", _localizer["PollCompleted"]);
                    return BadRequest(Errors.GetErrorList(ModelState));
                }

                var userVoted = await _voteService.UserVotedInPoll(User.ApiGetUserId(), model.PollId);
                if (userVoted)
                {
                    ModelState.AddModelError("", _localizer["PollRecordExist"]);
                    return BadRequest(Errors.GetErrorList(ModelState));
                }

                if (!await _pollService.UserCanVote(poll.Id, User.ApiGetUserId()))
                    return BadRequest(Errors.GetSingleErrorList("", _localizer["UserCannotVoteAfterAddedPollStart"]));

                if (!await _userService.IsVoter(User.ApiGetUserId()))
                    return BadRequest(Errors.GetSingleErrorList("", _localizer["PollVoterError"]));


                var vote = new Vote {PollId = model.PollId, VoterId = User.ApiGetUserId(), Value = model.Value};
                await _voteService.AddVote(vote);
                var result = _mapper.Map<Poll, PollListViewModel>(poll);
                result.UserVoted = true;
                return Ok(result);
            }

            return BadRequest(Errors.GetErrorList(ModelState));
        }
    }
}