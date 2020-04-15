using System;
using System.Net;
using System.Threading.Tasks;
using Decidehub.Core.Entities;
using Decidehub.Core.Identity;
using Decidehub.Core.Interfaces;
using Decidehub.Web.Helpers;
using Decidehub.Web.ViewModels.AccountViewModels;
using Decidehub.Web.ViewModels.Api;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;

namespace Decidehub.Web.Controllers.Api
{
    [Route("api/v1/account")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class ApiAccountController : Controller
    {
        private readonly ITenantService _tenantService;
        private readonly IUserService _userService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailSender _emailSender;
        private readonly IConfiguration _config;
        private readonly IStringLocalizer<ApiAccountController> _localizer;
        private readonly ITenantProvider _tenantProvider;

        public ApiAccountController(ITenantService tenantService, IUserService userService,
            UserManager<ApplicationUser> userManager, IEmailSender emailSender,
            IConfiguration config, IStringLocalizer<ApiAccountController> localizer, ITenantProvider tenantProvider)
        {
            _tenantService = tenantService;
            _userService = userService;
            _userManager = userManager;
            _emailSender = emailSender;
            _config = config;
            _localizer = localizer;
            _tenantProvider = tenantProvider;
        }

        /// <summary>
        ///    Creates new user for chosen tenant
        /// </summary>
        /// <param name="model">model containing user info.</param>
        /// <returns>Ok </returns>
        /// <response code="200">no response just 200</response>
        /// <response code="400">Error model</response>
        [ProducesResponseType(200)]
        [ProducesResponseType(typeof(ErrorViewModel), 400)]
        [HttpPost]
        [Route("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register([FromBody] RegisterViewModel model)
        {
            model.TenantId = model.TenantId.ToLower();
            if (ModelState.IsValid)
            {
                var tenant = await _tenantService.GetTenantWithIgnoredQueries(model.TenantId);
                if (tenant == null)
                {
                    tenant = new Tenant
                    {
                        Id = model.TenantId,
                        HostName = model.HostName,
                        Lang = model.Lang
                    };
                    await _tenantService.AddTenant(tenant);
                }
                else
                {
                    ModelState.AddModelError("TenantId",
                        _localizer["TenantExists"]);
                    return BadRequest(Errors.GetErrorList(ModelState));
                }

                var user = new ApplicationUser
                {
                    Id = Guid.NewGuid().ToString(),
                    UserName = $"{model.Email}_{model.TenantId}",
                    Email = model.Email,
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    TenantId = model.TenantId,
                    UserDetail = new UserDetail
                    {
                        AuthorityPercent = 0,
                        InitialAuthorityPercent = 0,
                        TenantId = model.TenantId,
                        LanguagePreference = model.Lang
                    }
                };
                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    await _userService.AssignRoleToUser(user.Id, "Admin", model.TenantId);
                    await SendEmailConfirmLink(user, model.HostName);

                    return Ok();
                }

                AddErrors(result);
            }

            // If we got this far, something failed, redisplay form
            return BadRequest(Errors.GetErrorList(ModelState));
        }

        /// <summary>
        ///   Sends resetPassword link to user's email
        /// </summary>
        /// <param name="model">model containing user email.</param>
        /// <returns>UserId and Code </returns>
        /// <response code="200">{userId, Code}</response>
        /// <response code="400">Error model</response>
        [ProducesResponseType(200)]
        [ProducesResponseType(typeof(ErrorViewModel), 400)]
        [HttpPost]
        [AllowAnonymous]
        [Route("forgotPassword")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user == null || !await _userManager.IsEmailConfirmedAsync(user))
                {
                    ModelState.AddModelError("", _localizer["InvalidUser"]);
                    //// Don't reveal that the user does not exist or is not confirmed
                    //return RedirectToAction(nameof(ForgotPasswordConfirmation));
                    return BadRequest(Errors.GetErrorList(ModelState));
                }

                // For more information on how to enable account confirmation and password reset please
                // visit https://go.microsoft.com/fwlink/?LinkID=532713
                var code = await _userManager.GeneratePasswordResetTokenAsync(user);

                var linkStart = _config["BaseUrlApi"];

                if (!string.IsNullOrEmpty(model.Subdomain)) linkStart = $"{model.Subdomain}.{_config["BaseUrlApi"]}";

                await _emailSender.SendEmailAsync(model.Email, _localizer["ResetPassword"],
                    $"{_localizer["ToResetPassword"]} : <a href='https://{linkStart}/reset-password?token={WebUtility.UrlEncode(code)}&userId={user.Id}&email={user.Email}&gen=0'>{_localizer["ClickHere"]}</a>",
                    _tenantProvider.GetTenantId());

                return Ok(new {UserId = user.Id, Code = code});
            }

            // If we got this far, something failed, redisplay form

            return BadRequest(Errors.GetErrorList(ModelState));
        }

        /// <summary>
        ///    Resets the password of user
        /// </summary>
        /// <param name="model">model containing user password info.</param>
        /// <returns>true </returns>
        /// <response code="200">true</response>
        /// <response code="400">Error model</response>
        [ProducesResponseType(200)]
        [ProducesResponseType(typeof(ErrorViewModel), 400)]
        [HttpPost]
        [AllowAnonymous]
        [Route("resetPassword")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByIdAsync(model.UserId);

                if (user != null)
                {
                    if (model.Gen == 1 && user.GeneratePassToken == model.Code)
                    {
                        var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
                        user.GeneratePassToken = resetToken;
                        var result = await _userManager.ResetPasswordAsync(user, resetToken, model.Password);
                        if (result.Succeeded)
                        {
                            user.EmailConfirmed = true;
                            await _userService.EditUser(user);
                            return Ok(true);
                        }

                        AddErrors(result);
                    }
                    else
                    {
                        var result = await _userManager.ResetPasswordAsync(user, model.Code, model.Password);
                        if (result.Succeeded)
                        {
                            user.EmailConfirmed = true;
                            await _userService.EditUser(user);
                            return Ok(true);
                        }

                        AddErrors(result);
                    }
                }
            }

            return BadRequest(Errors.GetErrorList(ModelState));
        }

        /// <summary>
        ///    Confirms the email of user
        /// </summary>
        /// <param name="model">model containing userId and code.</param>
        /// <returns>  success true </returns>
        /// <response code="200">true</response>
        /// <response code="400">Error model</response>
        [ProducesResponseType(200)]
        [ProducesResponseType(typeof(ErrorViewModel), 400)]
        [Route("confirmEmail")]
        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> ConfirmEmail([FromBody] UserTokenViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userService.GetUserById(model.UserId, true);
                if (user == null)
                {
                    ModelState.AddModelError("", _localizer["UserNotFound"]);
                }
                else
                {
                    var result = await _userManager.ConfirmEmailAsync(user, model.Code);
                    if (result.Succeeded)
                    {
                        return Ok(true);
                    }

                    var hostName = $"{user.TenantId}.{_config["BaseUrlApi"]}";
                    await SendEmailConfirmLink(user, hostName);
                    ModelState.AddModelError(_localizer["LinkExpired"], _localizer["LinkExpireMsg"]);
                    AddErrors(result);
                }
            }


            return BadRequest(Errors.GetErrorList(ModelState));
        }


        #region Helpers

        private async Task SendEmailConfirmLink(ApplicationUser user, string hostName)
        {
            var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            //    var callbackUrl = Url.EmailConfirmationLink(user.Id.ToString(), code, Request.Scheme);
            hostName = hostName.ToLower();
            var link = $"https://{hostName}/confirm-email?userId={user.Id}&code={WebUtility.UrlEncode(code)}";
            await _emailSender.SendEmailAsync(user.Email, _localizer["ConfirmEmail"],
                $"{_localizer["ToConfirmEmail"]} : <a href='{link}' style=\"font-weight: bold; color: #2F2F2F; cursor: pointer;\">{_localizer["ClickHere"]}</a>",
                _tenantProvider.GetTenantId());
            }

        private void AddErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }

        #endregion
    }
}