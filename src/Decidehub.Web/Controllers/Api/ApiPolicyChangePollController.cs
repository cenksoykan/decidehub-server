using System;
using System.Threading.Tasks;
using AutoMapper;
using Decidehub.Core.Entities;
using Decidehub.Core.Interfaces;
using Decidehub.Web.Extensions;
using Decidehub.Web.Helpers;
using Decidehub.Web.ViewModels.Api;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace Decidehub.Web.Controllers.Api
{
    [Produces("application/json")]
    [Route("api/v1/poll/policyChange")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class ApiPolicyChangePollController : Controller
    {
        private readonly IStringLocalizer<ApiPolicyChangePollController> _localizer;
        private readonly IMapper _mapper;
        private readonly IPollService _pollService;
        private readonly IUserService _userService;
        private readonly IVoteService _voteService;

        public ApiPolicyChangePollController(IPollService pollService, IMapper mapper, IVoteService voteService,
            IStringLocalizer<ApiPolicyChangePollController> localizer, IUserService userService)
        {
            _pollService = pollService;
            _mapper = mapper;
            _voteService = voteService;
            _localizer = localizer;
            _userService = userService;
        }

        /// <summary>
        ///     Saves the users vote for PolicyChangePoll
        /// </summary>
        /// <param name="model">model containing pollId and pollValue. PollValue must be -1 for negative vote, 1 for positive vote</param>
        /// <returns>Voted poll Info</returns>
        /// <response code="200">Voted poll Info</response>
        /// <response code="400">Error model</response>
        [ProducesResponseType(typeof(PolicyChangePollViewModel), 200)]
        [ProducesResponseType(typeof(ErrorViewModel), 400)]
        [HttpPost]
        [Route("vote")]
        public async Task<IActionResult> Vote([FromBody] PolicyChangePollVoteViewModel model)
        {
            if (ModelState.IsValid)
            {
                var poll = await _pollService.GetPoll(model.PollId);
                if (poll == null)
                {
                    ModelState.AddModelError("", _localizer["PollNotFound"]);
                    return BadRequest(Errors.GetErrorList(ModelState));
                }

                if (poll.GetType() != typeof(PolicyChangePoll))
                    return BadRequest(Errors.GetSingleErrorList("", _localizer["PollTypeNotPolicyChange"]));

                if (!poll.Active || poll.Deadline <= DateTime.UtcNow)
                {
                    ModelState.AddModelError("", _localizer["PollCompleted"]);
                    return BadRequest(Errors.GetErrorList(ModelState));
                }

                if (!await _pollService.UserCanVote(poll.Id, User.ApiGetUserId()))
                    return BadRequest(Errors.GetSingleErrorList("", _localizer["UserCannotVoteAfterAddedPollStart"]));

                if (!await _userService.IsVoter(User.ApiGetUserId()))
                    return BadRequest(Errors.GetSingleErrorList("", _localizer["PollVoterError"]));

                var userVoted = await _voteService.UserVotedInPoll(User.ApiGetUserId(), model.PollId);
                if (userVoted)
                {
                    ModelState.AddModelError("", _localizer["PollRecordExist"]);
                    return BadRequest(Errors.GetErrorList(ModelState));
                }

                //check deadline
                var vote = new Vote {PollId = model.PollId, VoterId = User.ApiGetUserId(), Value = model.PollValue};

                await _voteService.AddVote(vote);
                var result = _mapper.Map<Poll, PollListViewModel>(poll);
                result.UserVoted = true;
                return Ok(result);
            }

            return BadRequest(Errors.GetErrorList(ModelState));
        }
    }
}