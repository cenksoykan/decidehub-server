using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Decidehub.Web.Interfaces;
using Decidehub.Web.ViewModels.Api;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Decidehub.Web.Controllers.Api
{
    [Route("api/v1/members")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class MemberController : Controller
    {
        private readonly IUserApiViewModelService _userViewModelService;

        public MemberController(IUserApiViewModelService userViewModelService)
        {
            _userViewModelService = userViewModelService;
        }

        /// <summary>
        ///     Gets member list
        /// </summary>
        /// <returns>Member List</returns>
        /// <response code="200">Member List</response>
        [ProducesResponseType(typeof(IEnumerable<UserViewModel>), 200)]
        [HttpGet]
        public async Task<IEnumerable<MemberViewModel>> Get()
        {
            var users = await _userViewModelService.ListUsersWithImages();

            return users.Where(r => r.IsActive).Select(r => new MemberViewModel
            {
                Id = r.Id,
                Email = r.Email,
                FirstName = r.FirstName,
                LastName = r.LastName,
                UserImage = r.UserImage
            });
        }
    }
}