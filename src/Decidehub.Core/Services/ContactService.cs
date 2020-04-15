using System.Threading.Tasks;
using Decidehub.Core.Entities;
using Decidehub.Core.Interfaces;

namespace Decidehub.Core.Services
{
    public class ContactService : IContactService
    {
        private readonly IAsyncRepository<Contact> _contactRepository;

        public ContactService(IAsyncRepository<Contact> contactRepository)
        {
            _contactRepository = contactRepository;
        }

        public async Task AddMessage(Contact contact)
        {
            await _contactRepository.AddAsync(contact);
        }
    }
}