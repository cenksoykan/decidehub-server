using System.Collections.Generic;
using System.Threading.Tasks;

namespace Decidehub.Core.Interfaces
{
    public interface IAsyncRepository<T>
    {
        Task<T> GetSingleBySpecAsync(ISpecification<T> spec);
        Task<List<T>> ListAllAsync();
        Task<List<T>> ListAsync(ISpecification<T> spec);
        Task<T> AddAsync(T entity);
        Task AddRangeAsync(IEnumerable<T> entities);
        Task UpdateAsync(T entity);
        Task DeleteAsync(T entity);
        Task DeleteRangeAsync(IEnumerable<T> entities);
        Task<bool> AnyAsync(ISpecification<T> spec);
    }
}