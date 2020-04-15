using System.Collections.Generic;

namespace Decidehub.Web.ViewModels.Api
{
    public class SharePollViewModel : PollListViewModel
    {
        public List<SharePollUserValuesViewModel> Users { get; set; }
        public bool IsPublic { get; set; }
    }
}