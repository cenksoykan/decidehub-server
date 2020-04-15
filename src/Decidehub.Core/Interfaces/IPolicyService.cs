using System.Collections.Generic;
using System.Threading.Tasks;
using Decidehub.Core.Entities;

namespace Decidehub.Core.Interfaces
{
    public interface IPolicyService
    {
        Task<Policy> GetCurrentPolicy();
        Task<IEnumerable<Policy>> ListHistory();
        Task<bool> HasActivePoll();
        Task<Policy> Add(Policy policy);
        Task<Policy> GetPolicyById(long id);
        Task AcceptPolicy(long id);
        Task RejectPolicy(long id);
    }
}