using System;
using System.Collections.Generic;
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
using Hangfire;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace Decidehub.Web.Controllers.Api
{
    [Route("api/v1/poll")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class ApiPollController : Controller
    {
        private readonly IGenericService _genericService;
        private readonly IStringLocalizer<ApiPollController> _localizer;
        private readonly IMapper _mapper;
        private readonly IPollService _pollService;
        private readonly IPollApiViewModelService _pollViewModelService;
        private readonly ITenantProvider _tenantProvider;
        private readonly IUserService _userService;
        private readonly IVoteService _voteService;


        public ApiPollController(IPollService pollService, IMapper mapper, IUserService userService,
            IPollApiViewModelService pollViewModelService, IStringLocalizer<ApiPollController> localizer,
            IVoteService voteService, IGenericService genericService, ITenantProvider tenantProvider)
        {
            _pollService = pollService;
            _mapper = mapper;
            _userService = userService;
            _pollViewModelService = pollViewModelService;
            _localizer = localizer;
            _voteService = voteService;
            _genericService = genericService;
            _tenantProvider = tenantProvider;
        }


        /// <summary>
        ///     Gets all polls
        /// </summary>
        /// <returns> array PollInfo model </returns>
        /// <response code="200"></response>
        [ProducesResponseType(typeof(PollListViewModel), 200)]
        [Route("list")]
        [HttpGet]
        public async Task<IActionResult> List()
        {
            var allPolls = new List<PollListViewModel>();
            var completedPolls = await _pollService.GetCompletedPolls();
            allPolls.AddRange(_mapper.Map<IList<Poll>, IList<CompletedPollListViewModel>>(completedPolls));
            var notVotedPolls = await _pollService.GetUserNotVotedPolls(User.ApiGetUserId());
            allPolls.AddRange(_mapper.Map<IList<Poll>, IList<UserNotVotedPollListViewModel>>(notVotedPolls));
            var votedPolls = await _pollService.GetUserVotedPolls(User.ApiGetUserId());
            allPolls.AddRange(_mapper.Map<IList<Poll>, IList<UserVotedPollListViewModel>>(votedPolls));

            return Ok(allPolls.OrderByDescending(p => p.Deadline));
        }


        /// <summary>
        ///     Gets the currentUser's not yet voted polls
        /// </summary>
        /// <returns> array PollInfo model </returns>
        /// <response code="200"></response>
        [ProducesResponseType(typeof(UserNotVotedPollListViewModel), 200)]
        [Route("userNotVotedPolls")]
        [HttpGet]
        public async Task<IActionResult> GetUserNotVotedPolls()
        {
            var polls = await _pollService.GetUserNotVotedPolls(User.ApiGetUserId());
            var mapped = _mapper.Map<IList<Poll>, IList<UserNotVotedPollListViewModel>>(polls);

            return Ok(mapped);
        }

        /// <summary>
        ///     Gets the currentUser's voted polls
        /// </summary>
        /// <returns> array PollInfo model </returns>
        /// <response code="200"></response>
        [ProducesResponseType(typeof(UserVotedPollListViewModel), 200)]
        [Route("userVotedPolls")]
        [HttpGet]
        public async Task<IActionResult> GetUserVotedPolls()
        {
            var polls = await _pollService.GetUserVotedPolls(User.ApiGetUserId());
            var mapped = _mapper.Map<IList<Poll>, IList<UserVotedPollListViewModel>>(polls);

            return Ok(mapped);
        }

        /// <summary>
        ///     Gets the completed polls
        /// </summary>
        /// <returns> array PollInfo model </returns>
        /// <response code="200"></response>
        [ProducesResponseType(typeof(CompletedPollListViewModel), 200)]
        [Route("completedPolls")]
        [HttpGet]
        public async Task<IActionResult> GetCompletedPolls()
        {
            var polls = await _pollService.GetCompletedPolls();
            var mapped = _mapper.Map<IList<Poll>, IList<CompletedPollListViewModel>>(polls);

            return Ok(mapped);
        }

        /// <summary>
        ///     Checks whether the first authorityPoll completed or not
        /// </summary>
        /// <returns> array PollInfo model </returns>
        /// <response code="200"></response>
        [ProducesResponseType(200)]
        [Route("checkFirstAuthorityPoll")]
        [HttpGet]
        public async Task<IActionResult> CheckFirstAuthorityPoll()
        {
            var result = await _pollViewModelService.CheckFirstAuthorityPoll(User.ApiGetUserId());
            return Ok(result);
        }

        /// <summary>
        ///     Gets the poll required info for voting according to pollType
        /// </summary>
        /// <param name="pollId">Id of Poll </param>
        /// <returns> poll Info for voting</returns>
        /// <response code="200">Poll info</response>
        /// <response code="400">Error model</response>
        [ProducesResponseType(typeof(PolicyChangePollViewModel), 200)]
        [ProducesResponseType(typeof(ErrorViewModel), 400)]
        [Route("values/{pollId}")]
        [HttpGet]
        public async Task<IActionResult> GetPollValues(long pollId)
        {
            var poll = await _pollService.GetPoll(pollId);
            if (poll == null) return NotFound(Errors.GetSingleErrorList("", _localizer["PollNotFound"]));

            var pollType = poll.GetType();

            if (pollType == typeof(PolicyChangePoll))
            {
                var val = _mapper.Map<PolicyChangePoll, PollListViewModel>((PolicyChangePoll) poll);
                val.ListType = await SetListType(poll);
                return Ok(val);
            }

            if (pollType == typeof(AuthorityPoll))
            {
                var val = await _pollViewModelService.GetUsersForAuthorityVoting((AuthorityPoll) poll);
                var currentUser = val.Users.FirstOrDefault(x => x.Id == User.ApiGetUserId());
                if (currentUser != null) val.Users.Remove(currentUser);
                val.ListType = await SetListType(poll);
                return Ok(val);
            }

            if (pollType == typeof(MultipleChoicePoll))
            {
                var val = _pollViewModelService.MultipleChoicePollToViewModel((MultipleChoicePoll) poll);
                val.ListType = await SetListType(poll);
                return Ok(val);
            }

            if (pollType == typeof(SharePoll)) return Ok();

            return BadRequest(Errors.GetSingleErrorList("", _localizer["UnknownPoll"]));
        }


        /// <summary>
        ///     Resets the user's vote for related poll
        /// </summary>
        /// <param name="pollId">Id of Poll </param>
        /// <returns> Poll Info</returns>
        /// <response code="200">Poll info</response>
        /// <response code="400">Error model</response>
        /// <response code="404">Error model</response>
        [ProducesResponseType(typeof(PolicyChangePollViewModel), 200)]
        [ProducesResponseType(typeof(ErrorViewModel), 400)]
        [ProducesResponseType(typeof(PolicyChangePollViewModel), 404)]
        [Route("resetVote/{pollId}")]
        [HttpGet]
        public async Task<IActionResult> ResetVote(long pollId)
        {
            try
            {
                var poll = await _pollService.GetPoll(pollId);
                if (poll == null) return NotFound(Errors.GetSingleErrorList("", _localizer["PollNotFound"]));

                await _voteService.ResetVote(User.ApiGetUserId(), pollId);
                return Ok(_mapper.Map<Poll, PollListViewModel>(poll));
            }
            catch (Exception)
            {
                return BadRequest(Errors.GetSingleErrorList("", _localizer["VoteResetError"]));
            }
        }

        /// <summary>
        ///     Removes the poll
        /// </summary>
        /// <param name="pollId">Id of Poll </param>
        /// <returns>removed pollId</returns>
        /// <response code="200">Poll info</response>
        /// <response code="400">Error model</response>
        /// <response code="404">Error model</response>
        [ProducesResponseType(typeof(PolicyChangePollViewModel), 200)]
        [ProducesResponseType(typeof(ErrorViewModel), 400)]
        [ProducesResponseType(typeof(ErrorViewModel), 404)]
        [Route("removePoll/{pollId}")]
        [HttpDelete]
        public async Task<IActionResult> RemovePoll(long pollId)
        {
            try
            {
                var poll = await _pollService.GetPoll(pollId);
                if (poll != null)
                {
                    if (!poll.Active)
                        return BadRequest(Errors.GetSingleErrorList("", _localizer["CantRemoveCompletedPoll"]));

                    if (poll.UserId != User.ApiGetUserId())
                        return BadRequest(Errors.GetSingleErrorList("", _localizer["RemovePollUserError"]));

                    await _pollService.DeletePoll(pollId);
                    return Ok(poll.Id);
                }

                return NotFound(Errors.GetSingleErrorList("", _localizer["PollNotFound"]));
            }
            catch (Exception ex)
            {
                return BadRequest(Errors.GetSingleErrorList("", _localizer["RemovePollError"], ex));
            }
        }


        /// <summary>
        ///     Sends invitation mail to not yet voted users to vote
        /// </summary>
        /// <param name="pollId">Id of Poll </param>
        /// <returns>succeded true, occured error false</returns>
        /// <response code="200">true or false</response>
        /// <response code="400">Error model</response>
        [ProducesResponseType(typeof(PolicyChangePollViewModel), 200)]
        [ProducesResponseType(typeof(ErrorViewModel), 400)]
        [Route("inviteToVote/{pollId}")]
        [HttpGet]
        public async Task<IActionResult> SendEmailToNotVotedUsers(int pollId)
        {
            try
            {
                var poll = await _pollService.GetPoll(pollId);
                var user = await _userService.GetUserById(User.ApiGetUserId());
                if (poll == null)
                {
                    ModelState.AddModelError("", _localizer["PollNotFound"]);
                    return BadRequest(Errors.GetErrorList(ModelState));
                }

                if (!poll.Active)
                {
                    ModelState.AddModelError("", _localizer["PollCompleted"]);
                    return BadRequest(Errors.GetErrorList(ModelState));
                }

                var msg =
                    $"{user.FirstName} {user.LastName} {_localizer["NamedUser"]} <a href='{await _genericService.GetBaseUrl(null)}/polls'>{poll.Name}</a> {_localizer["InvitedYouToVote"]}.";
                BackgroundJob.Enqueue(() => _pollService.SendDirectEmailToNotVotedUsers(_tenantProvider.GetTenantId(),
                    _localizer["PollInvitation"], msg, pollId));

                return Ok(true);
            }
            catch (Exception)
            {
                return Ok(false);
            }
        }

        /// <summary>
        ///     Sends invitation mail to not yet voted users to vote
        /// </summary>
        /// <param name="pollId">Id of Poll </param>
        /// <param name="userId"> userId to be sent email </param>
        /// <returns>succeded true, occured error false</returns>
        /// <response code="200">true or false</response>
        /// <response code="400">Error model</response>
        [ProducesResponseType(typeof(PolicyChangePollViewModel), 200)]
        [ProducesResponseType(typeof(ErrorViewModel), 400)]
        [Route("inviteUserToVote/poll/{pollId}/user/{userId}")]
        [HttpGet]
        public async Task<IActionResult> SendEmailToNotVotedUsers(int pollId, string userId)
        {
            try
            {
                var poll = await _pollService.GetPoll(pollId);
                var user = await _userService.GetUserById(User.ApiGetUserId());
                if (poll == null)
                {
                    ModelState.AddModelError("", _localizer["PollNotFound"]);
                    return BadRequest(Errors.GetErrorList(ModelState));
                }

                if (!poll.Active)
                {
                    ModelState.AddModelError("", _localizer["PollCompleted"]);
                    return BadRequest(Errors.GetErrorList(ModelState));
                }

                var msg =
                    $"{user.FirstName} {user.LastName} {_localizer["NamedUser"]} <a href='{await _genericService.GetBaseUrl(null)}/polls'>{poll.Name}</a> {_localizer["InvitedYouToVote"]}.";
                BackgroundJob.Enqueue(() => _pollService.SendDirectEmailToNotVotedUser(_tenantProvider.GetTenantId(),
                    _localizer["PollInvitation"], msg, userId));

                return Ok(true);
            }
            catch (Exception)
            {
                return Ok(false);
            }
        }

        /// <summary>
        ///     Gets pollStatus.
        /// </summary>
        /// ///
        /// <param name="pollId">Id of Poll </param>
        /// <returns>  PollStatusViewModel including voted, notvoted users </returns>
        /// <response code="200">PollStatusViewModel</response>
        [ProducesResponseType(typeof(PollStatusViewModel), 200)]
        [Route("pollStatus/{pollId}")]
        [HttpGet]
        public async Task<IActionResult> GetPollStatus(long pollId)
        {
            var poll = await _pollService.GetPoll(pollId);
            if (poll == null)
            {
                ModelState.AddModelError("", _localizer["PollNotFound"]);
                return BadRequest(Errors.GetErrorList(ModelState));
            }

            var polls = await _pollViewModelService.GetPollStatus(pollId);
            return Ok(polls);
        }

        #region Helpers

        private async Task<string> SetListType(Poll poll)
        {
            var listType = "";
            if (_pollService.IsCompleted(poll))
                listType = PollListTypes.Completed.ToString().FirstCharacterToLower();
            else if (await _pollService.IsNotVotedPoll(User.ApiGetUserId(), poll))
                listType = PollListTypes.UserNotVoted.ToString().FirstCharacterToLower();
            else if (await _pollService.IsVotedPoll(User.ApiGetUserId(), poll))
                listType = PollListTypes.UserVoted.ToString().FirstCharacterToLower();

            return listType;
        }

        #endregion
    }
}