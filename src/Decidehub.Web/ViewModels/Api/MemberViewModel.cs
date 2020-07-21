namespace Decidehub.Web.ViewModels.Api
{
    public class MemberViewModel : BaseViewModel<string>
    {
        public string FirstName { get; set; }

        public string LastName { get; set; }

        public string Email { get; set; }

        public string UserImage { get; set; }
    }
}