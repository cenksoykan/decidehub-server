using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Decidehub.Core.Entities;
using Decidehub.Core.Enums;
using Decidehub.Core.Interfaces;
using Decidehub.Core.Services;
using Decidehub.Infrastructure.Data;
using Xunit;

namespace UnitTests.Services
{
    public class SettingServiceTests
    {
        private readonly ApplicationDbContext _context;
        private readonly ISettingService _settingService;
        public SettingServiceTests()
        {
            _context = Helpers.GetContext("test");
            IAsyncRepository<Setting> settingRepository = new EfRepository<Setting>(_context);
            _settingService = new SettingService(settingRepository);
        }
        [Fact]
        public async Task Should_Add_Setting()
        {
            var setting = new Setting
            {
                IsVisible = true,
                Key = Settings.AuthorityVotingRequiredUserPercentage.ToString(),
                Value = "80"
            };
            await _settingService.AddSetting(setting);
            var getSetting = _context.Settings.FirstOrDefault(x => x.Id == setting.Id);
            Assert.NotNull(getSetting);
            Assert.Equal(setting.IsVisible, getSetting.IsVisible);
            Assert.Equal(setting.Key, getSetting.Key);
            Assert.Equal(setting.Value, getSetting.Value);
        }
        [Fact]
        public async Task Should_GetAndAddDefaultSettings()
        {
            var list = await _settingService.GetSettings("test");
            var count= Enum.GetValues(typeof(Settings)).Length;
            Assert.Equal(count, list.Count);
        }
        [Fact]
        public async Task Should_GetSetting_ValueByType()
        {
            var setting = new Setting
            {
                IsVisible = true,
                Key = Settings.AuthorityVotingRequiredUserPercentage.ToString(),
                Value = "80"
            };
            _context.Settings.Add(setting);
            _context.SaveChanges();
            var getSetting = await _settingService.GetSettingValueByType(Settings.AuthorityVotingRequiredUserPercentage, "test");
            Assert.NotNull(getSetting);
            Assert.Equal(setting.Id, getSetting.Id);
            Assert.Equal(setting.Key, getSetting.Key);
        }
        [Fact]
        public async Task Should_SaveSettings()
        {
            var setting = new Setting
            {
                IsVisible = true,
                Key = Settings.AuthorityVotingRequiredUserPercentage.ToString(),
                Value = "80"
            };
            var setting2 = new Setting
            {
                IsVisible = true,
                Key = Settings.AuthorityVotingRequiredUserPercentage.ToString(),
                Value = "45"
            };
            _context.Settings.Add(setting);
            _context.Settings.Add(setting2);
            _context.SaveChanges();
            setting.Value = "70";
            setting2.Value = "65";
            var list = new List<Setting> { setting, setting2 };
            await _settingService.SaveSettings(list);

            var val1 = _context.Settings.FirstOrDefault(x => x.Id == setting.Id);
            var val2 = _context.Settings.FirstOrDefault(x => x.Id == setting2.Id);
            Assert.Equal(setting.Value, val1.Value);
            Assert.Equal(setting2.Value, val2.Value);
        }
        [Fact]
        public async Task Should_GetVotingDuration()
        {
            var setting = new Setting
            {
                IsVisible = true,
                Key = Settings.VotingDuration.ToString(),
                Value = "16"
            };
            _context.Settings.Add(setting);
            _context.SaveChanges();
            var res = await _settingService.GetVotingDurationByPollType(PollTypes.PolicyChangePoll, "test");
            var res2 = await _settingService.GetVotingDurationByPollType(PollTypes.AuthorityPoll, "test");
            Assert.Equal(Convert.ToDouble(setting.Value), res);
            Assert.Equal(Convert.ToDouble(setting.Value) * 2, res2);
        }
    }
}
