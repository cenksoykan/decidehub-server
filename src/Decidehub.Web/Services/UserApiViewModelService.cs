using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Decidehub.Core.Entities;
using Decidehub.Core.Identity;
using Decidehub.Core.Interfaces;
using Decidehub.Web.Interfaces;
using Decidehub.Web.ViewModels.Api;
using Microsoft.AspNetCore.Identity;

namespace Decidehub.Web.Services
{
    public class UserApiViewModelService : IUserApiViewModelService
    {
        private readonly IMapper _mapper;
        private readonly ITenantProvider _tenantProvider;
        private readonly IUserService _userService;

        public UserApiViewModelService(IUserService userService, IMapper mapper, ITenantProvider tenantProvider)
        {
            _userService = userService;
            _mapper = mapper;
            _tenantProvider = tenantProvider;
        }

        public async Task<IList<UserViewModel>> ListUsersWithImages()
        {
            var users = await _userService.GetUsersWithImage();

            var admins = await _userService.ListAdmins();
            var adminIds = admins.Select(a => a.Id).ToHashSet();
            var model = _mapper.Map<IList<ApplicationUser>, IList<UserViewModel>>(users);
            foreach (var user in model) user.IsAdmin = adminIds.Contains(user.Id);

            return model.OrderByDescending(u => u.IsActive)
                .ThenBy(u => u.FirstName)
                .ThenBy(u => u.LastName)
                .ThenBy(u => u.CreatedAt)
                .ToList();
        }

        public async Task<IEnumerable<UserViewModel>> ListUsersWithoutImages()
        {
            var users = await _userService.GetUsersAsync();
            var admins = await _userService.ListAdmins();
            var adminIds = admins.Select(a => a.Id).ToHashSet();
            var model = _mapper.Map<IList<ApplicationUser>, IList<UserViewModel>>(users);
            foreach (var user in model) user.IsAdmin = adminIds.Contains(user.Id);

            return model.OrderByDescending(u => u.IsActive)
                .ThenBy(u => u.FirstName)
                .ThenBy(u => u.LastName)
                .ThenBy(u => u.CreatedAt)
                .ToList();
        }

        public async Task<ApplicationUser> CreateUser(CreateUserViewModel userModel)
        {
            var getAdmin = await _userService.GetUserById(userModel.ProcessedById);
            var appUser = new ApplicationUser
            {
                Id = Guid.NewGuid().ToString(),
                Email = userModel.Email,
                FirstName = userModel.FirstName,
                LastName = userModel.LastName,
                UserName = userModel.Email,
                UserDetail = new UserDetail
                {
                    AuthorityPercent = 0,
                    InitialAuthorityPercent = userModel.InitialAuthorityPercent,
                    LanguagePreference = getAdmin != null ? getAdmin.UserDetail.LanguagePreference : "tr"
                }
            };

            var user = await _userService.AddUser(appUser,new List<IdentityUserRole<string>>());
            await _userService.AssignRoleToUser(user.Id, "Admin", user.TenantId);
            return user;
        }

        public async Task<ApplicationUser> EditUser(CreateUserViewModel model, bool isAdmin)
        {
            var user = await _userService.GetUserWithImageById(model.Id);
            var getAdmin = await _userService.GetUserById(model.ProcessedById);

            var admins = await _userService.ListAdmins();
            var adminIds = admins.Select(a => a.Id).ToHashSet();

            user.Email = model.Email;
            user.FirstName = model.FirstName;
            user.LastName = model.LastName;
            user.UserName = model.Email + "_" + _tenantProvider.GetTenantId();
            if (model.UserImage != null)
            {
                if (user.UserImage == null) user.UserImage = new UserImage();

                user.UserImage.UserImageStr = model.UserImage;
            }

            if (isAdmin)
            {
                user.UserDetail.InitialAuthorityPercent = model.InitialAuthorityPercent;

                if (model.IsAdmin && !adminIds.Contains(model.Id))
                    await _userService.AssignRoleToUser(user.Id, "Admin", user.TenantId);
                else if (!model.IsAdmin && adminIds.Contains(model.Id) && model.Id != model.ProcessedById)
                    await _userService.RemoveUserFromRole(user.Id, "Admin", user.TenantId);

            }

            user.UserDetail.LanguagePreference = getAdmin != null ? getAdmin.UserDetail.LanguagePreference : "tr";

            await _userService.EditUser(user);
            return user;
        }

        public async Task<UserViewModel> GetUserById(string id)
        {
            var user = await _userService.GetUserWithImageById(id);
            return await ToViewModel(user);
        }

        public async Task<UserViewModel> ToViewModel(ApplicationUser user)
        {
            var admins = await _userService.ListAdmins();
            var adminIds = admins.Select(a => a.Id).ToHashSet();
            UserViewModel model = null;
            if (user != null)
            {
                model = _mapper.Map<ApplicationUser, UserViewModel>(user);
                model.IsAdmin = adminIds.Contains(user.Id);
            }

            return model;
        }
    }
}