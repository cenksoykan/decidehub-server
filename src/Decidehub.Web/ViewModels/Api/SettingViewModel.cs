using System.Collections.Generic;

namespace Decidehub.Web.ViewModels.Api
{
    public class SettingViewModel
    {
        public string Key { get; set; }
        public string Value { get; set; }
    }

    public class SettingSaveViewModel
    {
        public List<SettingViewModel> Settings { get; set; }
    }
}