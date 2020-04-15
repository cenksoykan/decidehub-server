using System.Collections.Generic;
using System.Threading.Tasks;
using Decidehub.Core.Entities;
using Decidehub.Core.Enums;
using Decidehub.Core.Interfaces;
using Decidehub.Core.Specifications;

namespace Decidehub.Core.Services
{
    public class PolicyService : IPolicyService
    {
        private readonly IAsyncRepository<Policy> _policyRepository;

        public PolicyService(IAsyncRepository<Policy> policyRepository)
        {
            _policyRepository = policyRepository;
        }

        public async Task<Policy> GetCurrentPolicy()
        {
            return await _policyRepository.GetSingleBySpecAsync(new CurrentPolicySpecification());
        }

        public async Task<IEnumerable<Policy>> ListHistory()
        {
            return await _policyRepository.ListAsync(new HistoricalPolicySpecification());
        }

        public async Task<bool> HasActivePoll()
        {
            return await _policyRepository.AnyAsync(new ActivePollPolicySpecification());
        }

        public async Task<Policy> Add(Policy policy)
        {
            return await _policyRepository.AddAsync(policy);
        }

        public async Task<Policy> GetPolicyById(long id)
        {
            return await _policyRepository.GetSingleBySpecAsync(new PolicyByIdSpecification(id));
        }

        public async Task AcceptPolicy(long id)
        {
            var newPolicy = await GetPolicyById(id);
            var currentPolicy = await GetCurrentPolicyForTenant(newPolicy.TenantId);
            newPolicy.PolicyStatus = PolicyStatus.Active;
            await _policyRepository.UpdateAsync(newPolicy);

            if (currentPolicy != null)
            {
                currentPolicy.PolicyStatus = PolicyStatus.Overridden;
                await _policyRepository.UpdateAsync(currentPolicy);
            }
        }

        public async Task RejectPolicy(long id)
        {
            var policy = await GetPolicyById(id);
            policy.PolicyStatus = PolicyStatus.Rejected;
            await _policyRepository.UpdateAsync(policy);
        }

        private async Task<Policy> GetCurrentPolicyForTenant(string tenantId)
        {
            return await _policyRepository.GetSingleBySpecAsync(new CurrentPolicyForTenantSpecification(tenantId));
        }
    }
}