using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Decidehub.Core.Entities;
using Decidehub.Core.Enums;
using Decidehub.Core.Identity;
using Decidehub.Core.Interfaces;
using Decidehub.Core.Services;
using Decidehub.Infrastructure.Data;
using Decidehub.Infrastructure.Data.Repositories;
using Newtonsoft.Json;
using Xunit;

namespace UnitTests.Services
{
    public class PollServiceTests
    {
        public PollServiceTests()
        {
            _context = Helpers.GetContext("test");
            IAsyncRepository<Setting> settingRepository = new EfRepository<Setting>(_context);
            var tenantsContext = Helpers.GetTenantContext();
            _pollRepository = new EntityPollRepository(_context, tenantsContext);
            _settingService = new SettingService(settingRepository);
        }

        private readonly IPollRepository _pollRepository;
        private readonly ISettingService _settingService;
        private readonly ApplicationDbContext _context;
        private const int VotingFrequency = 90;
        private const int AuthorityVotingRequiredUserPercentage = 50;
        private const int VotingDuration = 24;
        private const int AuthorityPollVotingDuration = 2 * VotingDuration;

        [Fact]
        public async Task Should_Add_AuthorityPoll_And_PollSetting()
        {
            var pollService = new PollService(_pollRepository, _settingService, null, null, null, null, null);
            var poll = new AuthorityPoll
            {
                Name = "Authority Poll",
                Active = true,
                CreateTime = DateTime.UtcNow,
                QuestionBody = "test",
                TenantId = "test"
            };
            var addedPoll = await pollService.AddPoll(poll);
            var pollSetting = _context.PollSetting.FirstOrDefault(x => x.PollId == addedPoll.Id);
            Assert.NotNull(pollSetting);
            var settingList = JsonConvert.DeserializeObject<List<Setting>>(pollSetting.SettingJsonString);
            Assert.Equal(1, _context.AuthorityPolls.Count());
            Assert.Equal(VotingFrequency.ToString(),
                settingList.FirstOrDefault(x => x.Key == Settings.VotingFrequency.ToString())?.Value);
            Assert.Equal(AuthorityVotingRequiredUserPercentage.ToString(),
                settingList.FirstOrDefault(x => x.Key == Settings.AuthorityVotingRequiredUserPercentage.ToString())
                    ?.Value);
            Assert.Equal(AuthorityPollVotingDuration.ToString(),
                settingList.FirstOrDefault(x => x.Key == Settings.VotingDuration.ToString())?.Value);
            Assert.Equal(poll.CreateTime.AddHours(AuthorityPollVotingDuration).ToString("dd.MM.yyyy HH:mm"),
                addedPoll.Deadline.ToString("dd.MM.yyyy HH:mm"));
        }

        [Fact]
        public async Task Should_Add_Poll_And_PollSetting()
        {
            var pollService = new PollService(_pollRepository, _settingService, null, null, null, null, null);
            var poll = new PolicyChangePoll
            {
                Name = "test",
                Active = true,
                CreateTime = DateTime.UtcNow,
                QuestionBody = "test dfs",
                TenantId = "test"
            };
            var addedPoll = await pollService.AddPoll(poll);
            var pollSetting = _context.PollSetting.FirstOrDefault(x => x.PollId == addedPoll.Id);
            Assert.NotNull(pollSetting);
            var settingList = JsonConvert.DeserializeObject<List<Setting>>(pollSetting.SettingJsonString);
            Assert.Equal(1, _context.PolicyChangePolls.Count());
            Assert.Equal(VotingFrequency.ToString(),
                settingList.FirstOrDefault(x => x.Key == Settings.VotingFrequency.ToString())?.Value);
            Assert.Equal(AuthorityVotingRequiredUserPercentage.ToString(),
                settingList.FirstOrDefault(x => x.Key == Settings.AuthorityVotingRequiredUserPercentage.ToString())
                    ?.Value);
            Assert.Equal(VotingDuration.ToString(),
                settingList.FirstOrDefault(x => x.Key == Settings.VotingDuration.ToString())?.Value);
            Assert.Equal(poll.CreateTime.AddHours(VotingDuration).ToString("dd.MM.yyyy HH:mm"),
                addedPoll.Deadline.ToString("dd.MM.yyyy HH:mm"));
        }

        [Fact]
        public void Should_Check_IfPollIsCompleted()
        {
            var pollService = new PollService(_pollRepository, _settingService, null, null, null, null, null);
            var poll = new PolicyChangePoll
            {
                Name = "PolicyChangePoll test",
                Active = true,
                CreateTime = new DateTime(2018, 6, 2),
                Deadline = DateTime.UtcNow.AddHours(VotingDuration + 24),
                QuestionBody = "PolicyChangePoll test"
            };
            var poll2 = new PolicyChangePoll
            {
                Name = "test",
                Active = false,
                CreateTime = new DateTime(2018, 7, 3),
                Deadline = DateTime.UtcNow.AddHours(-24),
                QuestionBody = "test 123"
            };
            _context.Polls.Add(poll);
            _context.Polls.Add(poll2);
            _context.SaveChanges();
            var res1 = pollService.IsCompleted(poll);
            var res2 = pollService.IsCompleted(poll2);
            Assert.False(res1);
            Assert.True(res2);
        }

        [Fact]
        public async Task Should_Check_IfPollIsNotVoted()
        {
            IAsyncRepository<Vote> voteRepository = new EfRepository<Vote>(_context);
            IVoteService voteService = new VoteService(voteRepository, null);
            var pollService = new PollService(_pollRepository, _settingService, null, null, voteService, null, null);
            var poll = new PolicyChangePoll
            {
                Name = "PolicyChangePoll test",
                Active = true,
                CreateTime = new DateTime(2018, 6, 2),
                Deadline = DateTime.UtcNow.AddHours(VotingDuration + 24),
                QuestionBody = "PolicyChangePoll test"
            };
            var poll2 = new PolicyChangePoll
            {
                Name = "PolicyChangePoll test2",
                Active = true,
                CreateTime = new DateTime(2018, 6, 2),
                Deadline = DateTime.UtcNow.AddHours(24),
                QuestionBody = "PolicyChangePoll test2"
            };
            _context.Polls.Add(poll);
            _context.Polls.Add(poll2);
            _context.Users.Add(new ApplicationUser
            {
                Email = "test@workhow.com",
                FirstName = "test",
                LastName = "Test",
                TenantId = "test",
                CreatedAt = DateTime.UtcNow,
                SecurityStamp = new Guid().ToString(),
                EmailConfirmed = false,
                Id = 1.ToString(),
                IsDeleted = false,
                UserDetail = new UserDetail {AuthorityPercent = 1, LanguagePreference = "tr"}
            });
            _context.SaveChanges();
            _context.Votes.Add(new Vote {PollId = poll.Id, Value = 1, VoterId = 1.ToString()});
            _context.SaveChanges();
            var res1 = await pollService.IsNotVotedPoll(1.ToString(), poll);
            var res2 = await pollService.IsNotVotedPoll(1.ToString(), poll2);
            Assert.False(res1);
            Assert.True(res2);
        }

        [Fact]
        public async Task Should_Check_IfPollIsVoted()
        {
            IAsyncRepository<Vote> voteRepository = new EfRepository<Vote>(_context);
            IVoteService voteService = new VoteService(voteRepository, null);
            var pollService = new PollService(_pollRepository, _settingService, null, null, voteService, null, null);
            var poll = new PolicyChangePoll
            {
                Name = "PolicyChangePoll test",
                Active = true,
                CreateTime = new DateTime(2018, 6, 2),
                Deadline = DateTime.UtcNow.AddHours(VotingDuration + 24),
                QuestionBody = "PolicyChangePoll test"
            };
            var poll2 = new PolicyChangePoll
            {
                Name = "PolicyChangePoll test2",
                Active = true,
                CreateTime = new DateTime(2018, 6, 2),
                Deadline = DateTime.UtcNow.AddHours(24),
                QuestionBody = "PolicyChangePoll test2"
            };
            _context.Polls.Add(poll);
            _context.Polls.Add(poll2);
            _context.Users.Add(new ApplicationUser
            {
                Email = "test@workhow.com",
                FirstName = "test",
                LastName = "Test",
                TenantId = "test",
                CreatedAt = DateTime.UtcNow,
                SecurityStamp = new Guid().ToString(),
                EmailConfirmed = false,
                Id = 1.ToString(),
                IsDeleted = false,
                UserDetail = new UserDetail {AuthorityPercent = 1, LanguagePreference = "tr"}
            });
            _context.SaveChanges();
            _context.Votes.Add(new Vote {PollId = poll.Id, Value = 1, VoterId = 1.ToString()});
            _context.SaveChanges();
            var res1 = await pollService.IsVotedPoll(1.ToString(), poll);
            var res2 = await pollService.IsVotedPoll(1.ToString(), poll2);
            Assert.True(res1);
            Assert.False(res2);
        }

        [Fact]
        public async Task Should_Check_PollStatus_ByType()
        {
            var poll = new PolicyChangePoll
            {
                Name = "PolicyChangePoll test",
                Active = true,
                CreateTime = new DateTime(2018, 6, 2),
                Deadline = new DateTime(2018, 6, 2).AddDays(2),
                QuestionBody = "PolicyChangePoll test"
            };
            _context.Polls.Add(poll);
            _context.SaveChanges();
            var pollService = new PollService(_pollRepository, _settingService, null, null, null, null, null);
            var checkPolicyChange = await pollService.HasActivePollOfType<PolicyChangePoll>();
            var checkSalary = await pollService.HasActivePollOfType<SharePoll>();
            Assert.True(checkPolicyChange);
            Assert.False(checkSalary);
        }

        [Fact]
        public async Task Should_Check_User_Can_Vote()
        {
            var poll = new PolicyChangePoll
            {
                Name = "PolicyChangePoll test",
                Active = true,
                CreateTime = new DateTime(2018, 6, 2),
                Deadline = DateTime.UtcNow.AddHours(VotingDuration + 24),
                QuestionBody = "PolicyChangePoll test"
            };
            _context.Polls.Add(poll);
            _context.SaveChanges();
            _context.Users.Add(new ApplicationUser
            {
                Email = "test@workhow.com",
                FirstName = "test",
                LastName = "Test",
                TenantId = "test",
                CreatedAt = DateTime.UtcNow,
                SecurityStamp = new Guid().ToString(),
                EmailConfirmed = false,
                Id = 1.ToString(),
                IsDeleted = false,
                UserDetail = new UserDetail {AuthorityPercent = 1, LanguagePreference = "tr"}
            });
            _context.Users.Add(new ApplicationUser
            {
                Email = "test2@workhow.com",
                FirstName = "test",
                LastName = "Test",
                CreatedAt = new DateTime(2018, 5, 2),
                SecurityStamp = new Guid().ToString(),
                EmailConfirmed = false,
                Id = 2.ToString(),
                IsDeleted = false,
                UserDetail = new UserDetail {AuthorityPercent = 0, LanguagePreference = "tr"}
            });
            _context.SaveChanges();
            var userRepository = new EntityUserRepository(_context, null);
            var userService = new UserService(userRepository, null);
            var pollService = new PollService(_pollRepository, _settingService, userService, null, null, null, null);
            var user1Result = await pollService.UserCanVote(poll.Id, 1.ToString());
            var user2Result = await pollService.UserCanVote(poll.Id, 2.ToString());
            Assert.False(user1Result);
            Assert.True(user2Result);
        }

        [Fact]
        public async Task Should_Delete_PollAndVotes()
        {
            IAsyncRepository<Vote> voteRepository = new EfRepository<Vote>(_context);
            IVoteService voteService = new VoteService(voteRepository, null);
            var pollService = new PollService(_pollRepository, _settingService, null, null, voteService, null, null);
            var poll = new PolicyChangePoll
            {
                Name = "PolicyChangePoll test",
                Active = true,
                CreateTime = new DateTime(2018, 6, 2),
                Deadline = DateTime.UtcNow.AddHours(VotingDuration + 24),
                QuestionBody = "PolicyChangePoll test"
            };
            _context.Polls.Add(poll);
            _context.SaveChanges();
            _context.Votes.Add(new Vote {PollId = poll.Id, Value = 1, VoterId = 1.ToString()});
            _context.SaveChanges();

            await pollService.DeletePoll(poll.Id);
            var getPoll = _context.Polls.FirstOrDefault(x => x.Id == poll.Id);
            var getVotes = _context.Votes.Where(x => x.PollId == poll.Id);
            Assert.Null(getPoll);
            Assert.Equal(0, getVotes.Count());
        }

        [Fact]
        public async Task Should_End_Poll()
        {
            var pollService = new PollService(_pollRepository, _settingService, null, null, null, null, null);
            var poll = new PolicyChangePoll
            {
                Name = "PolicyChangePoll test",
                Active = true,
                CreateTime = new DateTime(2018, 6, 2),
                Deadline = DateTime.UtcNow.AddHours(VotingDuration + 24),
                QuestionBody = "PolicyChangePoll test"
            };
            _context.Polls.Add(poll);
            _context.SaveChanges();
            await pollService.EndPoll(poll.Id);
            var getPoll = _context.Polls.FirstOrDefault(x => x.Id == poll.Id);
            Assert.NotNull(getPoll);
            Assert.False(getPoll.Active);
            Assert.True(DateTime.UtcNow > getPoll.Deadline);
        }

        [Fact]
        public async Task Should_Get_Active_Poll_Count()
        {
            var pollService = new PollService(_pollRepository, _settingService, null, null, null, null, null);
            var poll = new PolicyChangePoll
            {
                Name = "PolicyChangePoll test",
                Active = true,
                CreateTime = new DateTime(2018, 6, 2),
                Deadline = DateTime.UtcNow.AddHours(VotingDuration + 24),
                QuestionBody = "PolicyChangePoll test"
            };
            var poll2 = new PolicyChangePoll
            {
                Name = "test",
                Active = true,
                CreateTime = new DateTime(2018, 7, 3),
                Deadline = DateTime.UtcNow.AddHours(VotingDuration),
                QuestionBody = "test 123"
            };
            _context.Polls.Add(poll);
            _context.Polls.Add(poll2);
            _context.SaveChanges();
            var count = await pollService.GetActivePollCount();
            Assert.Equal(2, count);
        }

        [Fact]
        public async Task Should_Get_ActivePolls()
        {
            var pollService = new PollService(_pollRepository, _settingService, null, null, null, null, null);
            var poll = new PolicyChangePoll
            {
                Name = "PolicyChangePoll test",
                Active = true,
                CreateTime = new DateTime(2018, 6, 2),
                Deadline = DateTime.UtcNow.AddHours(VotingDuration + 24),
                QuestionBody = "PolicyChangePoll test"
            };
            var poll2 = new PolicyChangePoll
            {
                Name = "test",
                Active = true,
                CreateTime = new DateTime(2018, 7, 3),
                Deadline = DateTime.UtcNow.AddHours(VotingDuration),
                QuestionBody = "test 123"
            };
            _context.Polls.Add(poll);
            _context.Polls.Add(poll2);
            _context.SaveChanges();
            var polls = await pollService.GetActivePolls(false);
            Assert.Equal(2, polls.Count);
        }

        [Fact]
        public async Task Should_Get_All_PollCount()
        {
            var poll = new PolicyChangePoll
            {
                Name = "PolicyChangePoll test",
                Active = false,
                CreateTime = new DateTime(2018, 6, 2),
                Deadline = DateTime.UtcNow.AddHours(-2),
                QuestionBody = "PolicyChangePoll test",
                TenantId = "test2"
            };
            var poll2 = new PolicyChangePoll
            {
                Name = "PolicyChangePoll test2",
                Active = false,
                CreateTime = new DateTime(2018, 6, 2),
                Deadline = DateTime.UtcNow,
                QuestionBody = "PolicyChangePoll test2"
            };
            _context.Polls.Add(poll);
            _context.Polls.Add(poll2);
            _context.SaveChanges();
            var pollService = new PollService(_pollRepository, _settingService, null, null, null, null, null);
            var count = await pollService.GetCompletedPollCount(true);
            Assert.Equal(2, count);
        }

        [Fact]
        public async Task Should_Get_All_Polls()
        {
            var poll = new PolicyChangePoll
            {
                Name = "PolicyChangePoll test",
                Active = false,
                CreateTime = new DateTime(2018, 6, 2),
                Deadline = DateTime.UtcNow.AddHours(-2),
                QuestionBody = "PolicyChangePoll test",
                TenantId = "test2"
            };
            var poll2 = new PolicyChangePoll
            {
                Name = "PolicyChangePoll test2",
                Active = false,
                CreateTime = new DateTime(2018, 6, 2),
                Deadline = DateTime.UtcNow,
                QuestionBody = "PolicyChangePoll test2"
            };
            _context.Polls.Add(poll);
            _context.Polls.Add(poll2);
            _context.SaveChanges();
            var pollService = new PollService(_pollRepository, _settingService, null, null, null, null, null);
            var polls = await pollService.GetAllPolls();
            Assert.Equal(1, polls.Count);
        }

        [Fact]
        public async Task Should_Get_Completed_Poll_Count()
        {
            var poll = new PolicyChangePoll
            {
                Name = "PolicyChangePoll test",
                Active = false,
                CreateTime = new DateTime(2018, 6, 2),
                Deadline = DateTime.UtcNow.AddHours(-2),
                QuestionBody = "PolicyChangePoll test"
            };
            var poll2 = new PolicyChangePoll
            {
                Name = "PolicyChangePoll test2",
                Active = true,
                CreateTime = new DateTime(2018, 6, 2),
                Deadline = DateTime.UtcNow,
                QuestionBody = "PolicyChangePoll test2"
            };
            _context.Polls.Add(poll);
            _context.Polls.Add(poll2);
            _context.SaveChanges();
            var pollService = new PollService(_pollRepository, _settingService, null, null, null, null, null);
            var count = await pollService.GetCompletedPollCount();
            Assert.Equal(1, count);
        }

        [Fact]
        public async Task Should_Get_Completed_Polls()
        {
            var poll = new PolicyChangePoll
            {
                Name = "PolicyChangePoll test",
                Active = false,
                CreateTime = new DateTime(2018, 6, 2),
                Deadline = DateTime.UtcNow.AddHours(-2),
                QuestionBody = "PolicyChangePoll test"
            };
            _context.Polls.Add(poll);
            _context.SaveChanges();
            var pollService = new PollService(_pollRepository, _settingService, null, null, null, null, null);
            var completedPolls = await pollService.GetCompletedPolls(null);
            Assert.Equal(1, completedPolls.Count);
        }

        [Fact]
        public async Task Should_Get_CompletedPolls_SpecifiedCount()
        {
            var poll = new PolicyChangePoll
            {
                Name = "PolicyChangePoll test",
                Active = false,
                CreateTime = new DateTime(2018, 6, 2),
                Deadline = DateTime.UtcNow.AddHours(-2),
                QuestionBody = "PolicyChangePoll test"
            };
            var poll2 = new PolicyChangePoll
            {
                Name = "PolicyChangePoll test2",
                Active = false,
                CreateTime = new DateTime(2018, 3, 2),
                Deadline = DateTime.UtcNow.AddHours(-1),
                QuestionBody = "PolicyChangePoll test2"
            };
            _context.Polls.Add(poll);
            _context.Polls.Add(poll2);
            _context.SaveChanges();
            var pollService = new PollService(_pollRepository, _settingService, null, null, null, null, null);
            var completedPolls = await pollService.GetCompletedPolls(1);
            Assert.Equal(1, completedPolls.Count);
        }

        [Fact]
        public async Task Should_Get_LatestPoll_ByType()
        {
            var poll = new PolicyChangePoll
            {
                Name = "PolicyChangePoll test",
                Active = false,
                CreateTime = new DateTime(2018, 6, 2),
                Deadline = DateTime.UtcNow.AddDays(-2),
                QuestionBody = "PolicyChangePoll test"
            };

            var poll2 = new PolicyChangePoll
            {
                Name = "test",
                Active = false,
                CreateTime = new DateTime(2018, 7, 3),
                Deadline = DateTime.UtcNow.AddDays(-1),
                QuestionBody = "test 123"
            };
            _context.Add(poll);
            _context.Add(poll2);
            _context.SaveChanges();
            var pollService = new PollService(_pollRepository, null, null, null, null, null, null);
            var getPoll = await pollService.GetLastPollOfType<PolicyChangePoll>(null);
            Assert.Equal(poll2.Id, getPoll.Id);
        }

        [Fact]
        public async Task Should_Get_Poll()
        {
            var poll = new PolicyChangePoll
            {
                Name = "PolicyChangePoll test",
                Active = true,
                CreateTime = new DateTime(2018, 6, 2),
                Deadline = new DateTime(2018, 6, 2).AddDays(2),
                QuestionBody = "PolicyChangePoll test"
            };
            _context.Polls.Add(poll);
            _context.SaveChanges();
            var pollService = new PollService(_pollRepository, null, null, null, null, null, null);
            var getPoll = await pollService.GetPoll(poll.Id, false);
            Assert.NotNull(getPoll);
        }

        [Fact]
        public async Task Should_Get_Poll_RequiredUserPercentage()
        {
            var pollService = new PollService(_pollRepository, _settingService, null, null, null, null, null);
            var poll = new PolicyChangePoll
            {
                Name = "PolicyChangePoll test",
                Active = true,
                CreateTime = new DateTime(2018, 6, 2),
                Deadline = DateTime.UtcNow.AddHours(VotingDuration + 24),
                QuestionBody = "PolicyChangePoll test"
            };

            var pollSetting = new List<Setting>
            {
                new Setting
                {
                    Key = Settings.AuthorityVotingRequiredUserPercentage.ToString(),
                    Value = "75",
                    IsVisible = true
                }
            };
            var pollSettingJson = JsonConvert.SerializeObject(pollSetting);
            poll.PollSetting = new PollSetting
            {
                SettingJsonString = pollSettingJson
            };
            _context.Polls.Add(poll);
            _context.SaveChanges();
            var result = await pollService.GetPollRequiredUserPercentage(poll.Id);
            Assert.Equal(75, result);
        }

        [Fact]
        public async Task Should_Get_Poll_TenantIgnored()
        {
            var poll = new PolicyChangePoll
            {
                Name = "PolicyChangePoll test",
                Active = true,
                CreateTime = new DateTime(2018, 6, 2),
                Deadline = new DateTime(2018, 6, 2).AddDays(2),
                QuestionBody = "PolicyChangePoll test",
                TenantId = "test3"
            };
            _context.Polls.Add(poll);
            _context.SaveChanges();
            var pollService = new PollService(_pollRepository, null, null, null, null, null, null);
            var getPoll = await pollService.GetPoll(poll.Id, true);
            Assert.NotNull(getPoll);
        }

        [Fact]
        public async Task Should_Get_Poll_Voting_Duration()
        {
            var pollService = new PollService(_pollRepository, _settingService, null, null, null, null, null);
            var poll = new PolicyChangePoll
            {
                Name = "PolicyChangePoll test",
                Active = true,
                CreateTime = new DateTime(2018, 6, 2),
                Deadline = DateTime.UtcNow.AddHours(VotingDuration + 24),
                QuestionBody = "PolicyChangePoll test"
            };

            var pollSetting = new List<Setting>
            {
                new Setting
                {
                    Key = Settings.VotingDuration.ToString(),
                    Value = "12",
                    IsVisible = true
                }
            };
            var pollSettingJson = JsonConvert.SerializeObject(pollSetting);
            poll.PollSetting = new PollSetting
            {
                SettingJsonString = pollSettingJson
            };
            _context.Polls.Add(poll);
            _context.SaveChanges();
            var duration = await pollService.GetPollVotingDuration(poll.Id);
            Assert.Equal(12, duration);
        }

        [Fact]
        public async Task Should_Get_Public_Polls()
        {
            var pollService = new PollService(_pollRepository, _settingService, null, null, null, null, null);
            var poll = new PolicyChangePoll
            {
                Name = "PolicyChangePoll test",
                Active = true,
                CreateTime = new DateTime(2018, 6, 2),
                Deadline = DateTime.UtcNow.AddHours(VotingDuration + 24),
                QuestionBody = "PolicyChangePoll test"
            };
            var poll2 = new PolicyChangePoll
            {
                Name = "test",
                Active = false,
                CreateTime = new DateTime(2018, 7, 3),
                Deadline = DateTime.UtcNow.AddHours(-24),
                QuestionBody = "test 123"
            };
            _context.Polls.Add(poll);
            _context.Polls.Add(poll2);
            _context.SaveChanges();
            var polls = await pollService.GetPublicPolls();
            Assert.Equal(2, polls.Count);
        }

        [Fact]
        public async Task Should_Get_TenantCompleted_PollCount()
        {
            var poll = new PolicyChangePoll
            {
                Name = "PolicyChangePoll test",
                Active = false,
                CreateTime = new DateTime(2018, 6, 2),
                Deadline = DateTime.UtcNow.AddHours(-2),
                QuestionBody = "PolicyChangePoll test",
                TenantId = "test2"
            };
            var poll2 = new PolicyChangePoll
            {
                Name = "PolicyChangePoll test2",
                Active = false,
                CreateTime = new DateTime(2018, 6, 2),
                Deadline = DateTime.UtcNow,
                QuestionBody = "PolicyChangePoll test2"
            };
            _context.Polls.Add(poll);
            _context.Polls.Add(poll2);
            _context.SaveChanges();
            var pollService = new PollService(_pollRepository, _settingService, null, null, null, null, null);
            var count = await pollService.GetCompletedPollCount(false, "test2");
            Assert.Equal(1, count);
        }

        [Fact]
        public async Task Should_Get_TenantPollCount_ByType()
        {
            var pollService = new PollService(_pollRepository, _settingService, null, null, null, null, null);
            var poll = new PolicyChangePoll
            {
                Name = "PolicyChangePoll test",
                Active = true,
                CreateTime = new DateTime(2018, 6, 2),
                Deadline = DateTime.UtcNow.AddHours(VotingDuration + 24),
                QuestionBody = "PolicyChangePoll test"
            };
            var poll2 = new PolicyChangePoll
            {
                Name = "test",
                Active = false,
                CreateTime = new DateTime(2018, 7, 3),
                Deadline = DateTime.UtcNow.AddHours(-24),
                QuestionBody = "test 123"
            };
            _context.Polls.Add(poll);
            _context.Polls.Add(poll2);
            _context.SaveChanges();

            var count = await pollService.GetPollCountByType<PolicyChangePoll>("test");
            Assert.Equal(2, count);
        }

        [Fact]
        public async Task Should_Get_User_Not_Voted_Polls()
        {
            var userRepository = new EntityUserRepository(_context, null);
            var userService = new UserService(userRepository, null);
            var pollService = new PollService(_pollRepository, _settingService, userService, null, null, null, null);
            var poll = new PolicyChangePoll
            {
                Name = "PolicyChangePoll test",
                Active = true,
                CreateTime = new DateTime(2018, 6, 2),
                Deadline = DateTime.UtcNow.AddHours(VotingDuration + 24),
                QuestionBody = "PolicyChangePoll test"
            };
            var poll2 = new PolicyChangePoll
            {
                Name = "test",
                Active = true,
                CreateTime = new DateTime(2018, 7, 3),
                Deadline = DateTime.UtcNow.AddHours(VotingDuration),
                QuestionBody = "test 123"
            };
            _context.Polls.Add(poll);
            _context.Polls.Add(poll2);
            _context.SaveChanges();

            _context.Users.Add(new ApplicationUser
            {
                Email = "test@workhow.com",
                FirstName = "test",
                LastName = "Test",
                TenantId = "test",
                CreatedAt = DateTime.UtcNow,
                SecurityStamp = new Guid().ToString(),
                EmailConfirmed = false,
                Id = 1.ToString(),
                IsDeleted = false,
                UserDetail = new UserDetail {AuthorityPercent = 1, LanguagePreference = "tr"}
            });
            _context.SaveChanges();

            _context.Votes.Add(new Vote {PollId = poll.Id, Value = 1, VoterId = 1.ToString()});
            _context.SaveChanges();
            var result = await pollService.GetUserNotVotedPolls(1.ToString());
            Assert.Equal(1, result.Count);
        }

        [Fact]
        public async Task Should_Get_User_VotedPolls()
        {
            var poll = new PolicyChangePoll
            {
                Name = "PolicyChangePoll test",
                Active = true,
                CreateTime = new DateTime(2018, 6, 2),
                Deadline = DateTime.UtcNow.AddHours(VotingDuration + 24),
                QuestionBody = "PolicyChangePoll test"
            };
            _context.Polls.Add(poll);
            _context.SaveChanges();
            _context.Users.Add(new ApplicationUser
            {
                Email = "test@workhow.com",
                FirstName = "test",
                LastName = "Test",
                TenantId = "test",
                CreatedAt = DateTime.UtcNow,
                SecurityStamp = new Guid().ToString(),
                EmailConfirmed = false,
                Id = 1.ToString(),
                IsDeleted = false,
                UserDetail = new UserDetail {AuthorityPercent = 1, LanguagePreference = "tr"}
            });
            _context.SaveChanges();

            _context.Votes.Add(new Vote {PollId = poll.Id, Value = 1, VoterId = 1.ToString()});
            _context.SaveChanges();
            var pollService = new PollService(_pollRepository, _settingService, null, null, null, null, null);
            var votedPolls = await pollService.GetUserVotedPolls(1.ToString());
            Assert.Equal(1, votedPolls.Count);
            Assert.Contains(votedPolls, x => x.Id == poll.Id);
        }

        [Fact]
        public async Task Should_Set_PollResult()
        {
            var poll = new PolicyChangePoll
            {
                Name = "PolicyChangePoll test",
                Active = true,
                CreateTime = new DateTime(2018, 6, 2),
                Deadline = DateTime.UtcNow.AddHours(VotingDuration + 24),
                QuestionBody = "PolicyChangePoll test"
            };
            _context.Polls.Add(poll);
            _context.SaveChanges();
            var pollService = new PollService(_pollRepository, _settingService, null, null, null, null, null);
            await pollService.SetPollResult(poll.Id, "test");
            var getPoll = _context.Polls.FirstOrDefault(x => x.Id == poll.Id);
            Assert.Equal("test", getPoll?.Result);
        }
    }
}