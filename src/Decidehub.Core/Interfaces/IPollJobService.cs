using System.Threading.Tasks;

namespace Decidehub.Core.Interfaces
{
    public interface IPollJobService
    {
        Task CheckPollCompletion();
        Task AuthorityPollStart();
    }
}