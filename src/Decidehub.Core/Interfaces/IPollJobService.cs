using System.Threading.Tasks;
using Hangfire.Server;

namespace Decidehub.Core.Interfaces
{
    public interface IPollJobService
    {
        Task CheckPollCompletion(PerformContext context = null);
        Task AuthorityPollStart();
    }
}