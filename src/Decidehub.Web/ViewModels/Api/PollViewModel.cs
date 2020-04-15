using System.ComponentModel.DataAnnotations;

namespace Decidehub.Web.ViewModels.Api
{
    public class PollViewModel
    {
        [Required(ErrorMessage = "NameRequired")]
        public string Name { get; set; }

        public string Description { get; set; }
        public string UserId { get; set; }
        public int Id { get; set; }
    }
}