using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Decidehub.Core.Entities;
using Decidehub.Core.Enums;
using Decidehub.Core.Interfaces;
using Decidehub.Core.Specifications.SettingSpecifications;

namespace Decidehub.Core.Services
{
    public class SettingService : ISettingService
    {
        private readonly IAsyncRepository<Setting> _settingRepository;

        public SettingService(IAsyncRepository<Setting> settingRepository)
        {
            _settingRepository = settingRepository;
        }

        public async Task AddSetting(Setting setting)
        {
            await _settingRepository.AddAsync(setting);
        }


        public async Task<IList<Setting>> GetSettings(string tenantId)
        {
            var defaultSettings = GetDefaultSettings(tenantId);

            var settings = (await GetAllSettings(tenantId)).Where(x => Enum.IsDefined(typeof(Settings), x.Key))
                .ToList();

            var missingSettings = defaultSettings.Where(d => settings.All(s => s.Key != d.Key)).Select(setting =>
                new Setting
                {
                    IsVisible = true,
                    Key = setting.Key,
                    TenantId = tenantId,
                    Value = setting.Value
                }).ToList();

            if (missingSettings.Any())
            {
                await AddSettings(missingSettings);
                settings.AddRange(missingSettings);
            }

            return settings.ToList();
        }

        public async Task<Setting> GetSettingValueByType(Settings setting, string tenantId)
        {
            return await _settingRepository.GetSingleBySpecAsync(
                new SettingByKeyTenantSpecification(setting, tenantId));
        }

        public async Task SaveSettings(IEnumerable<Setting> settings)
        {
            foreach (var setting in settings)
            {
                var existingSetting = await _settingRepository.GetSingleBySpecAsync(
                    new SettingByKeyTenantSpecification((Settings) Enum.Parse(typeof(Settings), setting.Key),
                        setting.TenantId));

                if (existingSetting == null)
                    await _settingRepository.AddAsync(setting);
                else
                {
                    existingSetting.Value = setting.Value;
                    await _settingRepository.UpdateAsync(existingSetting);
                }
            }
        }


        public async Task<double> GetVotingDurationByPollType(PollTypes pollType, string tenantId)
        {
            var val = await GetSettingValueByType(Settings.VotingDuration, tenantId);
            var votingDuration = Convert.ToDouble(val.Value);
            return pollType == PollTypes.AuthorityPoll ? 2 * votingDuration : votingDuration;
        }

        private async Task AddSettings(IEnumerable<Setting> settings)
        {
            try
            {
                await _settingRepository.AddRangeAsync(settings);
            }
            catch (Exception)
            {
                // ignored
            }
        }

        private async Task<IEnumerable<Setting>> GetAllSettings(string tenantId)
        {
            return await _settingRepository.ListAsync(new VisibleSettingsSpecification(tenantId));
        }

        private static IEnumerable<Setting> GetDefaultSettings(string tenant)
        {
            var settings = new[]
            {
                new Setting
                {
                    Key = Settings.VotingFrequency.ToString(),
                    Value = "90",
                    IsVisible = true,
                    TenantId = tenant
                },
                new Setting
                {
                    Key = Settings.AuthorityVotingRequiredUserPercentage.ToString(),
                    Value = "50",
                    IsVisible = true,
                    TenantId = tenant
                },
                new Setting
                {
                    Key = Settings.VotingDuration.ToString(),
                    Value = "24",
                    IsVisible = true,
                    TenantId = tenant
                }
            };
            return settings.ToList();
        }
    }
}