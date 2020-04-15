using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Decidehub.Core.Identity;
using Decidehub.Core.Interfaces;
using Decidehub.Web.Extensions;
using Decidehub.Web.Helpers;
using Decidehub.Web.Interfaces;
using Decidehub.Web.ViewModels.Api;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;

namespace Decidehub.Web.Controllers.Api
{
    [Route("api/v1/users")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class UserController : Controller
    {
        private readonly IConfiguration _config;
        private readonly IEmailSender _emailSender;
        private readonly IStringLocalizer<UserController> _localizer;
        private readonly ITenantService _tenantService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IUserService _userService;
        private readonly IUserApiViewModelService _userViewModelService;

        public UserController(IUserApiViewModelService userViewModelService, UserManager<ApplicationUser> userManager,
            IUserService userService, ITenantService tenantService, IEmailSender emailSender, IConfiguration config,
            IStringLocalizer<UserController> localizer)
        {
            _userViewModelService = userViewModelService;
            _userManager = userManager;
            _userService = userService;
            _emailSender = emailSender;
            _config = config;
            _localizer = localizer;
            _tenantService = tenantService;
        }

        /// <summary>
        ///     Gets user list
        /// </summary>
        /// <returns>User List</returns>
        /// <response code="200">User List</response>
        //[TypeFilter(typeof(CheckTenantTrial))]
        [ProducesResponseType(typeof(IEnumerable<UserViewModel>), 200)]
        [HttpGet]
        public async Task<IEnumerable<UserViewModel>> Get(bool noImage = false)
        {
            return noImage
                ? await _userViewModelService.ListUsersWithoutImages()
                : await _userViewModelService.ListUsersWithImages();
        }


        /// <summary>
        ///     Gets user Info
        /// </summary>
        /// <param name="id">userId</param>
        /// <returns>User Info</returns>
        /// <response code="200">User Info</response>
        /// <response code="404"></response>
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        [HttpGet]
        [Route("{id}")]
        public async Task<IActionResult> GetUser(string id)
        {
            var result = await _userViewModelService.GetUserById(id);
            if (result != null) return Ok(result);

            return NotFound();
        }


        /// <summary>
        ///     Creates or Updates user
        /// </summary>
        /// <param name="model">Containing user info, if userId exists user is updated else is created </param>
        /// <returns>User info</returns>
        /// <response code="200">User info</response>
        /// <response code="400">Error model</response>
        [ProducesResponseType(typeof(UserViewModel), 200)]
        [ProducesResponseType(typeof(ErrorViewModel), 400)]
        [HttpPost]
        [Route("addEdit")]
        public async Task<object> AddEdit([FromBody] CreateUserViewModel model)
        {
            var apiUserId = User.ApiGetUserId();
            model.ProcessedById = apiUserId;

            if (ModelState.IsValid)
            {
                var getUser = await _userManager.FindByEmailAsync(model.Email);
                if (getUser != null && getUser.Id != model.Id)
                {
                    ModelState.AddModelError("", _localizer["UserExistWithSameEmail"]);
                    return BadRequest(Errors.GetErrorList(ModelState));
                }


                var isAdmin = await _userService.UserInRole(apiUserId, "Admin");

                if (!isAdmin && apiUserId != model.Id)
                {
                    ModelState.AddModelError("", _localizer["AdminUserError"]);
                    return BadRequest(Errors.GetErrorList(ModelState));
                }


                if (model.Id != null)
                {
                    var user = await _userViewModelService.EditUser(model, isAdmin);
                    return Ok(await _userViewModelService.ToViewModel(user));
                }

                if (isAdmin)
                {
                    model.CreatedAt = DateTime.UtcNow;
                    var user = await _userViewModelService.CreateUser(model);

                    await SendGeneratePasswordLink(user);
                    return Ok(await _userViewModelService.ToViewModel(user));
                }
            }

            return BadRequest(Errors.GetErrorList(ModelState));
        }

        /// <summary>
        ///     Deletes the user
        /// </summary>
        /// <param name="userId"> to be deleted userId</param>
        /// <returns>deleted userId</returns>
        /// <response code="200">userId</response>
        /// <response code="400">Error model</response>
        [ProducesResponseType(typeof(PolicyChangePollViewModel), 200)]
        [ProducesResponseType(typeof(ErrorViewModel), 400)]
        [HttpDelete]
        [Route("delete/{userId}")]
        public async Task<IActionResult> DeleteUser(string userId)
        {
            try
            {
                var apiUserId = User.ApiGetUserId();
                var adminRole = await _userService.UserInRole(apiUserId, "Admin");

                if (adminRole && apiUserId == userId || !adminRole && apiUserId != userId)
                {
                    ModelState.AddModelError("", _localizer["AdminUserError"]);
                    return BadRequest(Errors.GetErrorList(ModelState));
                }

                await _userService.DeleteUser(userId);

                return Ok(userId);
            }
            catch (Exception)
            {
                return BadRequest(Errors.GetSingleErrorList(_localizer["Error"], _localizer["UserDeleteError"]));
            }
        }

        /// <summary>
        ///     Sends membership invitation to not active users
        /// </summary>
        /// <param name="userId"> to be sent userId</param>
        /// <returns>true</returns>
        /// <response code="200">true</response>
        /// <response code="400">false</response>
        [ProducesResponseType(typeof(PolicyChangePollViewModel), 200)]
        [ProducesResponseType(typeof(ErrorViewModel), 400)]
        [HttpGet]
        [Route("sendInvitation/{userId}")]
        public async Task<IActionResult> ResendGeneratePasswordLink(string userId)
        {
            try
            {
                var user = await _userService.GetUserById(userId);
                if (user != null && !user.EmailConfirmed) await SendGeneratePasswordLink(user);

                return Ok(true);
            }
            catch (Exception)
            {
                return BadRequest(false);
            }
        }

        /// <summary>
        ///     Checks whether the currentUser isVoter or not
        /// </summary>
        /// <returns>if isvoter true otherwise else</returns>
        /// <response code="200">true/false</response>
        [ProducesResponseType(typeof(PolicyChangePollViewModel), 200)]
        [Route("isVoter")]
        [HttpGet]
        public async Task<bool> IsVoter()
        {
            return await _userService.IsVoter(User.ApiGetUserId());
        }

        /// <summary>
        ///     Sets language preference of users
        /// </summary>
        /// <param name="userId"> Id of User</param>
        /// <param name="lang"> language abbrv.</param>
        /// <returns>true</returns>
        /// <response code="200">lang</response>
        /// <response code="400">false</response>
        [ProducesResponseType(typeof(PolicyChangePollViewModel), 200)]
        [ProducesResponseType(typeof(ErrorViewModel), 400)]
        [HttpGet]
        [Route("setLanguage/user/{userId}/lang/{lang}")]
        public async Task<IActionResult> SetUserLangPreference(string userId, string lang)
        {
            try
            {
                await _userService.SetUserLangPreference(userId, lang);

                return Ok(lang);
            }
            catch (Exception)
            {
                return BadRequest(false);
            }
        }

        [Route("deleteAccount")]
        [HttpDelete]
        public async Task<IActionResult> DeleteAccount()
        {
            var adminRole = await _userService.UserInRole(User.ApiGetUserId(), "Admin");
            if (!adminRole)
            {
                ModelState.AddModelError("", _localizer["AdminUserError"]);
                return BadRequest(Errors.GetSingleErrorList("", _localizer["AdminUserError"]));
            }

            var userId = User.ApiGetUserId();
            var user = await _userService.GetUserById(userId);

            if (user != null)
            {
                await _userService.DeleteUsers();
                await _tenantService.DeleteTenant(user.TenantId);
            }

            return Ok();
        }

        #region Helpers

        private async Task SendGeneratePasswordLink(ApplicationUser user)
        {
            if (user != null)
            {
                var code = await _userManager.GeneratePasswordResetTokenAsync(user);

                user.GeneratePassToken = code;
                await _userService.EditUser(user);

                await _emailSender.SendEmailAsync(user.Email, "Decidehub " + _localizer["MembershipInvitation"],
                    $"{_localizer["MembershipInvitationMsg"]} {_localizer["ToGeneratePassword"]} : <a href='https://{user.TenantId}.{_config["BaseUrlApi"]}/reset-password?token={WebUtility.UrlEncode(code)}&userId={user.Id}&email={user.Email}&gen=1&name={user.FirstName} {user.LastName}' style=\"font-weight: bold; color: #2F2F2F; cursor: pointer;\">{_localizer["ClickHere"]}</a>");
            }
        }

        #endregion
    }
}