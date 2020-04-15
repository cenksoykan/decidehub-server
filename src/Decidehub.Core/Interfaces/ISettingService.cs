using System.Collections.Generic;
using System.Threading.Tasks;
using Decidehub.Core.Entities;
using Decidehub.Core.Enums;

namespace Decidehub.Core.Interfaces
{
    public interface ISettingService
    {
        Task<IList<Setting>> GetSettings(string tenantId);
        Task SaveSettings(IEnumerable<Setting> settings);
        Task<Setting> GetSettingValueByType(Settings setting, string tenantId);
        Task AddSetting(Setting setting);
        Task<double> GetVotingDurationByPollType(PollTypes pollType, string tenantId);
    }
}