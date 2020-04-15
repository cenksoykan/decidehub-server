using System.Collections.Generic;
using System.Threading.Tasks;
using Decidehub.Core.Identity;
using Decidehub.Web.ViewModels.Api;

namespace Decidehub.Web.Interfaces
{
    public interface IUserApiViewModelService
    {
        Task<IList<UserViewModel>> ListUsersWithImages();
        Task<ApplicationUser> CreateUser(CreateUserViewModel userModel);
        Task<ApplicationUser> EditUser(CreateUserViewModel model, bool isAdmin);
        Task<UserViewModel> GetUserById(string id);
        Task<UserViewModel> ToViewModel(ApplicationUser user);
        Task<IEnumerable<UserViewModel>> ListUsersWithoutImages();
    }
}