using System;
using System.Collections.Generic;
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
    [Route("api/v1/policy")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class ApiPolicyController : Controller
    {
        private readonly IStringLocalizer<ApiPolicyController> _localizer;
        private readonly IMapper _mapper;
        private readonly IPolicyService _policyService;
        private readonly IPollService _pollService;
        private readonly IPollApiViewModelService _pollViewModelService;
        private readonly ITenantProvider _tenantProvider;

        public ApiPolicyController(IPolicyService policyService, IPollService pollService,
            IStringLocalizer<ApiPolicyController> localizer, IPollApiViewModelService pollViewModelService,
            ITenantProvider tenantProvider, IMapper mapper)
        {
            _policyService = policyService;
            _pollService = pollService;
            _localizer = localizer;
            _pollViewModelService = pollViewModelService;
            _tenantProvider = tenantProvider;
            _mapper = mapper;
        }

        /// <summary>
        ///     Gets current policy
        /// </summary>
        /// <returns>Current policy</returns>
        [HttpGet]
        [AllowAnonymous]
        public async Task<PolicyViewModel> Get()
        {
            var currentPolicy = await _policyService.GetCurrentPolicy() ?? new Policy
                {Body = "&nbsp;", Title = "Yönetmelik", UserId = null, PolicyStatus = PolicyStatus.Active};
            return _mapper.Map<PolicyViewModel>(currentPolicy);
        }


        /// <summary>
        ///     Get policy with id
        /// </summary>
        /// <returns>Policy with id</returns>
        [HttpGet]
        [Route("{id}")]
        public async Task<PolicyViewModel> GetById(long id)
        {
            var policy = await _policyService.GetPolicyById(id);
            return _mapper.Map<PolicyViewModel>(policy);
        }

        /// <summary>
        ///     Lists all previous policies
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("history")]
        public async Task<IEnumerable<PolicyViewModel>> History()
        {
            var history = await _policyService.ListHistory();
            return _mapper.Map<IEnumerable<PolicyViewModel>>(history);
        }

        /// <summary>
        ///     Save the policy and start a vote
        /// </summary>
        /// <param name="viewModel">New policy details</param>
        /// <returns>Success or Fail</returns>
        [HttpPost]
        [Route("save")]
        public async Task<IActionResult> Save([FromBody] NewPolicyViewModel viewModel)
        {
            var hasActivePoll = await _policyService.HasActivePoll();

            if (hasActivePoll)
            {
                ModelState.AddModelError("", _localizer["HasActivePoll"]);
                return BadRequest(Errors.GetErrorList(ModelState));
            }

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


            var userId = User.ApiGetUserId();
            var policy = await _policyService.Add(new Policy
            {
                UserId = userId,
                TenantId = _tenantProvider.GetTenantId(),
                Body = viewModel.Body,
                Title = viewModel.Title,
                CreatedAt = DateTime.UtcNow,
                PolicyStatus = PolicyStatus.Voting
            });
            var poll = await _pollViewModelService.NewPolicyChangePoll(new PolicyChangePollViewModel
            {
                UserId = userId,
                StartedBy = userId,
                Name = "Yönetmelik Değişim Oylaması",
                Description = viewModel.PollDescription,
                PolicyId = policy.Id
            });

            await _pollService.NotifyUsers(poll.PollType, PollNotificationTypes.Started, poll);

            return Ok(_mapper.Map<PolicyChangePoll, PolicyChangePollViewModel>(poll));
        }
    }
}