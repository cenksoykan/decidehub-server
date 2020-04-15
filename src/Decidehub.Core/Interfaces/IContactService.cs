using System.Threading.Tasks;
using Decidehub.Core.Entities;

namespace Decidehub.Core.Interfaces
{
    public interface IContactService
    {
        Task AddMessage(Contact contact);
    }
}