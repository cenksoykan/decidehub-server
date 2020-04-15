using System.Collections.Generic;

namespace Decidehub.Web.ViewModels.Api
{
    public class SettingPollValuesViewModel : PollListViewModel
    {
        public List<SettingViewModel> PollSettings { get; set; }
    }
}