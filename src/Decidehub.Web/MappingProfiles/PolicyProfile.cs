using AutoMapper;
using Decidehub.Core.Entities;
using Decidehub.Web.ViewModels.Api;

namespace Decidehub.Web.MappingProfiles
{
    /// <inheritdoc />
    public class PolicyProfile : Profile
    {
        /// <inheritdoc />
        public PolicyProfile()
        {
            CreateMap<Policy, PolicyViewModel>()
                .ForMember(dest => dest.UserName,
                    opts => opts.MapFrom(src => src.User == null ? "" : $"{src.User.FirstName} {src.User.LastName}"));
        }
    }
}