using System.Threading.Tasks;

namespace Decidehub.Core.Interfaces
{
    public interface IEmailSender
    {
        Task SendEmailAsync(string email, string subject, string message,string tenant=null);
    }
}