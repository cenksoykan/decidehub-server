using System.Threading.Tasks;

namespace Decidehub.Core.Interfaces
{
    public interface IGenericService
    {
        Task<string> GetBaseUrl(string tenant);
    }
}