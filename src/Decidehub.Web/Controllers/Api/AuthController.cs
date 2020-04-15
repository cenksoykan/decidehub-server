using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Decidehub.Core.Identity;
using Decidehub.Core.Interfaces;
using Decidehub.Web.Helpers;
using Decidehub.Web.Interfaces;
using Decidehub.Web.Models;
using Decidehub.Web.ViewModels.AccountViewModels;
using Decidehub.Web.ViewModels.Api;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;

namespace Decidehub.Web.Controllers.Api
{
    [Route("api/v1")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class AuthController : Controller
    {
        private readonly JwtIssuerOptions _jwtOptions;
        private readonly IStringLocalizer<AuthController> _localizer;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IUserService _userService;
        private readonly IUserApiViewModelService _userViewModelService;

        public AuthController(IOptions<JwtIssuerOptions> jwtOptions, UserManager<ApplicationUser> userManager,
            IUserService userService, IUserApiViewModelService userViewModelService,
            IStringLocalizer<AuthController> localizer)
        {
            _jwtOptions = jwtOptions.Value;
            _userManager = userManager;
            _userService = userService;
            _userViewModelService = userViewModelService;
            _localizer = localizer;
        }

        /// <summary>
        ///     Authenticates the user
        /// </summary>
        /// <param name="model"> containing user email and password</param>
        /// <returns>User Info and Token for api access</returns>
        /// <response code="200">User Info and Token</response>
        /// <response code="400">ErrorViewModel</response>
        [ProducesResponseType(typeof(PolicyChangePollViewModel), 200)]
        [ProducesResponseType(typeof(ErrorViewModel), 400)]
        [AllowAnonymous]
        [HttpPost]
        public async Task<object> Post([FromBody] LoginViewModel model)
        {
            if (!ModelState.IsValid) return BadRequest(Errors.GetErrorList(ModelState));

            var user = await _userService.GetUserByEmail(model.Email);
            if (user != null)
            {
                if (!await _userManager.IsEmailConfirmedAsync(user))
                {
                    ModelState.AddModelError("", _localizer["EmailNotConfirmed"]);

                    return BadRequest(Errors.GetErrorList(ModelState));
                }

                if (await _userManager.CheckPasswordAsync(user, model.Password))
                {
                    var getUserWithImage = await _userViewModelService.GetUserById(user.Id);
                    var getRoleAdmin = await _userService.GetUserRoles(user.Id);

                    return Ok(new
                    {
                        user.FirstName,
                        user.LastName,
                        Token = await GenerateEncodedToken(model.Email, user),
                        user.Email,
                        getUserWithImage.UserImage,
                        IsAdmin = getRoleAdmin.Any(role => role.Name == "Admin"),
                        user.TenantId,
                        Lang = user.UserDetail.LanguagePreference,
                        user.Id
                    });
                }

                var list = new List<ErrorViewModel>
                {
                    new ErrorViewModel
                    {
                        Title = _localizer["InvalidLoginAttempt"],
                        Description = _localizer["InvalidUserOrPass"]
                    }
                };
                return BadRequest(list);
            }

            {
                var list = new List<ErrorViewModel>
                {
                    new ErrorViewModel
                    {
                        Title = _localizer["InvalidLoginAttempt"],
                        Description = _localizer["InvalidUserOrPass"]
                    }
                };
                return BadRequest(list);
            }
        }

        private async Task<string> GenerateEncodedToken(string email, ApplicationUser user)
        {
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, email),
                new Claim(JwtRegisteredClaimNames.Jti, await _jwtOptions.JtiGenerator()),
                new Claim(ClaimTypes.PrimarySid, user.Id),
                new Claim("tenant", user.TenantId)
            };

            // Create the JWT security token and encode it.
            var jwt = new JwtSecurityToken(_jwtOptions.Issuer, _jwtOptions.Audience, claims, _jwtOptions.NotBefore,
                _jwtOptions.Expiration, _jwtOptions.SigningCredentials);

            var encodedJwt = new JwtSecurityTokenHandler().WriteToken(jwt);

            return encodedJwt;
        }
    }
}