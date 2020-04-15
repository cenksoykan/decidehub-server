using System;
using System.Collections.Generic;
using System.Linq;
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
    /// <summary>
    /// Settings API Controller
    /// </summary>
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [Produces("application/json")]
    [Route("api/v1/settings")]
    public class ApiSettingController : Controller
    {
        private readonly ISettingService _settingService;
        private readonly ITenantProvider _tenantProvider;
        private readonly IMapper _mapper;
        private readonly IUserService _userService;
        private readonly IStringLocalizer<ApiSettingController> _localizer;


        /// <inheritdoc />
        public ApiSettingController(ISettingService settingService, ITenantProvider tenantProvider, IMapper mapper,
            IUserService userService, IStringLocalizer<ApiSettingController> localizer)
        {
            _settingService = settingService;
            _mapper = mapper;
            _userService = userService;
            _localizer = localizer;
            _tenantProvider = tenantProvider;
        }

        /// <summary>
        ///  Gets the setting List
        /// </summary>
        /// <returns> Array setting  </returns>
        /// <response code="200"> settings keys and values   </response>
        [ProducesResponseType(200)]
        [HttpGet]
        public async Task<IList<SettingViewModel>> Get()
        {
            var settings = await _settingService.GetSettings(_tenantProvider.GetTenantId());
            var list = _mapper.Map<IList<Setting>, IList<SettingViewModel>>(settings.ToList());
            return list;
        }


        /// <summary>
        ///    Changes Settings
        /// </summary>
        /// <param name="model">model containing Description and Setting key, value list.</param>
        /// <returns> New settings</returns>
        /// <response code="200"> Success </response>
        /// <response code="400">Error model</response> 
        [ProducesResponseType(200)]
        [ProducesResponseType(typeof(ErrorViewModel), 400)]
        [HttpPost]
        public async Task<ActionResult> Update([FromBody] SettingSaveViewModel model)
        {
            var apiUserId = User.ApiGetUserId();

            try
            {
                var adminRole = await _userService.UserInRole(apiUserId, "Admin");

                if (!adminRole)
                {
                    ModelState.AddModelError("", _localizer["AdminUserError"]);
                    return BadRequest(Errors.GetErrorList(ModelState));
                }


                var tenantId = _tenantProvider.GetTenantId();

                var settings = model.Settings.Select(item => new Setting
                    {IsVisible = true, Key = item.Key, Value = item.Value, TenantId = tenantId}).ToList();

                await _settingService.SaveSettings(settings);
                return Ok(await _settingService.GetSettings(tenantId));
            }
            catch (Exception)
            {
                return Ok(new List<Setting>());
            }
        }
    }
}