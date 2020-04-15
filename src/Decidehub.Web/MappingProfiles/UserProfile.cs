using AutoMapper;
using Decidehub.Core.Identity;
using Decidehub.Web.ViewModels.Api;

namespace Decidehub.Web.MappingProfiles
{
    /// <inheritdoc />
    public class UserProfile : Profile
    {
        /// <inheritdoc />
        public UserProfile()
        {
            CreateMap<ApplicationUser, UserViewModel>()
                .ForMember(dest => dest.InitialAuthorityPercent,
                    opts => opts.MapFrom(src => src.UserDetail.InitialAuthorityPercent))
                .ForMember(dest => dest.UserImage,
                    opts => opts.MapFrom(src => src.UserImage == null ? null : src.UserImage.UserImageStr))
                .ForMember(dest => dest.IsActive,
                    opts => opts.MapFrom(src => src.EmailConfirmed))
                .ForMember(dest => dest.HasAuthority,
                    opts => opts.MapFrom(src => src.UserDetail.AuthorityPercent > 0));

            CreateMap<ApplicationUser, AuthorityPollUsersViewModel>()
                .ForMember(dest => dest.UserImage,
                    opts => opts.MapFrom(src => src.UserImage == null ? null : src.UserImage.UserImageStr))
                .ForMember(dest => dest.IsActive,
                    opts => opts.MapFrom(src => src.EmailConfirmed));


            CreateMap<UserViewModel, ApplicationUser>();
        }
    }
}