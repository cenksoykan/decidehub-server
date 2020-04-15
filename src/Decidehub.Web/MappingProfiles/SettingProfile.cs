using AutoMapper;
using Decidehub.Core.Entities;
using Decidehub.Web.ViewModels.Api;

namespace Decidehub.Web.MappingProfiles
{
    public class SettingProfile : Profile
    {
        public SettingProfile()
        {
            CreateMap<Setting, SettingViewModel>();

            CreateMap<SettingViewModel, Setting>();
        }
    }
}