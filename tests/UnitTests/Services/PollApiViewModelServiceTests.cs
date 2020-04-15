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
using Decidehub.Web.Interfaces;
using Decidehub.Web.Services;
using Decidehub.Web.ViewModels.Api;
using Moq;
using Newtonsoft.Json;
using Xunit;

namespace UnitTests.Services
{
    public class PollApiViewModelServiceTests
    {
        public PollApiViewModelServiceTests()
        {
            var tenantProviderMock = new Mock<ITenantProvider>();
            tenantProviderMock.Setup(serv => serv.GetTenantId()).Returns("test");
            _context = Helpers.GetContext("test");
            var tenantsDbContext = Helpers.GetTenantContext();
            ITenantRepository tenantRepository = new EntityTenantRepository(tenantsDbContext);
            IAsyncRepository<Setting> settingRepository = new EfRepository<Setting>(_context);
            IPollRepository pollRepository = new EntityPollRepository(_context, tenantsDbContext);
            ISettingService settingService = new SettingService(settingRepository);
            ITenantService tenantService = new TenantService(tenantRepository, settingService, null);
            IUserRepository userRepository = new EntityUserRepository(_context, null);
            IUserService userService = new UserService(userRepository, null);
            IAsyncRepository<Vote> voteRepository = new EfRepository<Vote>(_context);
            var voteService = new VoteService(voteRepository, userService);
            IPollService pollService = new PollService(pollRepository, settingService, userService, tenantService,
                voteService, null, null);
            var mapper = Helpers.GetMapper();
            _pollApiViewModelService = new PollApiViewModelService(tenantProviderMock.Object, pollService, mapper,
                voteService, userService, settingService);
        }

        private readonly ApplicationDbContext _context;
        private readonly IPollApiViewModelService _pollApiViewModelService;

        [Fact]
        public async Task Should_Correctly_Get_Next_AuthorityPollDate()
        {
            var setting = new Setting
            {
                Key = Settings.VotingFrequency.ToString(),
                Value = "30",
                IsVisible = true
            };
            _context.Settings.Add(setting);
            _context.SaveChanges();
            var poll = new AuthorityPoll
            {
                Name = "AuthorityPoll",
                Active = false,
                CreateTime = DateTime.UtcNow.AddDays(-31),
                Deadline = DateTime.UtcNow.AddDays(-30)
            };
            _context.Polls.Add(poll);
            _context.SaveChanges();
            var result = await _pollApiViewModelService.GetNextAuthorityPollStartDate();
            Assert.Equal(poll.Deadline.AddDays(30), result);
        }

        [Fact]
        public async Task Should_Create_AuthorityPoll()
        {
            var model = new AuthorityPollViewModel
            {
                Name = "test",
                UserId = 1.ToString()
            };
            var poll = await _pollApiViewModelService.NewAuthorityPoll(model);
            var result = _context.AuthorityPolls.FirstOrDefault(x => x.Id == poll.Id);
            Assert.NotNull(result);
            Assert.Equal(model.Name, poll.Name);
            Assert.Equal(model.UserId, poll.UserId);
        }

        [Fact]
        public async Task Should_Create_MultipleChoicePoll()
        {
            var model = new MultipleChoicePollViewModel
            {
                Name = "test",
                Options = new List<string> {"a", "b", "c"},
                Description = "test dec"
            };
            var poll = await _pollApiViewModelService.NewMultipleChoicePoll(model);
            var result = _context.MultipleChoicePolls.FirstOrDefault(x => x.Id == poll.Id);
            Assert.NotNull(result);
            Assert.Equal(model.Name, result.Name);
            Assert.Equal(JsonConvert.SerializeObject(model.Options), result.OptionsJsonString);
            Assert.Equal(model.Description, result.QuestionBody);
        }

        [Fact]
        public async Task Should_Create_PolicyChangePoll()
        {
            var model = new PolicyChangePollViewModel
            {
                Description = "test desc",
                UserId = 1.ToString(),
                Name = "test"
            };
            var poll = await _pollApiViewModelService.NewPolicyChangePoll(model);
            var result = _context.PolicyChangePolls.FirstOrDefault(a => a.Id == poll.Id);
            Assert.NotNull(result);
            Assert.Equal(model.Description, result.QuestionBody);
            Assert.Equal(model.UserId, result.UserId);
            Assert.Equal(model.Name, result.Name);
        }

        [Fact]
        public async Task Should_Get_PollStatus()
        {
            var poll = new PolicyChangePoll
            {
                Name = "PolicyChangePoll test",
                Active = true,
                CreateTime = new DateTime(2018, 6, 2),
                Deadline = DateTime.UtcNow.AddHours(26),
                QuestionBody = "PolicyChangePoll test"
            };
            _context.Add(poll);
            _context.SaveChanges();
            _context.Users.Add(new ApplicationUser
            {
                Email = "test@workhow.com",
                FirstName = "test",
                LastName = "Test",
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
                FirstName = "Test",
                LastName = "Test",
                CreatedAt = DateTime.UtcNow,
                SecurityStamp = new Guid().ToString(),
                EmailConfirmed = false,
                Id = 2.ToString(),
                IsDeleted = false,
                UserDetail = new UserDetail {AuthorityPercent = 1, LanguagePreference = "tr"}
            });
            _context.SaveChanges();
            _context.Votes.Add(new Vote {PollId = poll.Id, Value = 1, VoterId = 1.ToString()});
            _context.SaveChanges();
            var result = await _pollApiViewModelService.GetPollStatus(poll.Id);
            Assert.Single(result.NotVotedUsers);
            Assert.Single(result.VotedUsers);
            Assert.Equal(poll.Id, result.PollId);
        }

        [Fact]
        public async Task Should_Get_PollStatus_For_SharePoll()
        {
            var poll = new SharePoll
            {
                Name = "sharePoll",
                Active = false,
                CreateTime = new DateTime(2018, 7, 2),
                Deadline = new DateTime(2018, 7, 2).AddDays(-2),
                QuestionBody = "test paylasim"
            };
            _context.Add(poll);
            _context.SaveChanges();
            _context.Users.Add(new ApplicationUser
            {
                Email = "test@workhow.com",
                FirstName = "test",
                LastName = "Test",
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
                FirstName = "Test",
                LastName = "Test",
                CreatedAt = DateTime.UtcNow,
                SecurityStamp = new Guid().ToString(),
                EmailConfirmed = false,
                Id = 2.ToString(),
                IsDeleted = false,
                UserDetail = new UserDetail {AuthorityPercent = 1, LanguagePreference = "tr"}
            });
            _context.SaveChanges();
            _context.Votes.Add(new Vote
                {PollId = poll.Id, Value = 1000, VoterId = 1.ToString(), VotedUserId = 2.ToString()});
            _context.SaveChanges();
            var result = await _pollApiViewModelService.GetPollStatus(poll.Id) as SharePollStatusViewModel;
            Assert.NotNull(result);
            Assert.Single(result.NotVotedUsers);
            Assert.Single(result.VotedUsers);
            Assert.Equal(poll.Id, result.PollId);
        }

        [Fact]
        public async Task Should_GetUsersForAuthorityPoll()
        {
            var poll = new AuthorityPoll
            {
                Name = "AuthorityPoll",
                Active = true,
                CreateTime = DateTime.UtcNow,
                Deadline = DateTime.UtcNow.AddDays(2)
            };
            _context.Polls.Add(poll);
            _context.SaveChanges();
            _context.Users.Add(new ApplicationUser
            {
                Email = "test@workhow.com",
                FirstName = "test",
                LastName = "Test",
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
                FirstName = "Test",
                LastName = "Test",
                CreatedAt = DateTime.UtcNow,
                SecurityStamp = new Guid().ToString(),
                EmailConfirmed = false,
                Id = 2.ToString(),
                IsDeleted = false,
                UserDetail = new UserDetail {AuthorityPercent = 1, LanguagePreference = "tr"}
            });
            _context.SaveChanges();
            var result = await _pollApiViewModelService.GetUsersForAuthorityVoting(poll);
            Assert.Equal(2, result.Users.Count);
            Assert.Equal(poll.Id, result.PollId);
            Assert.Equal(poll.Name, result.Name);
        }

        [Fact]
        public async Task Should_Save_AuthorityVotes()
        {
            var model = new AuthorityPollSaveViewModel
            {
                PollId = 1,
                Votes = new List<AuthorityPollUserValues>
                {
                    new AuthorityPollUserValues
                    {
                        UserId = 2.ToString(),
                        Value = 500
                    },
                    new AuthorityPollUserValues
                    {
                        UserId = 3.ToString(),
                        Value = 500
                    }
                }
            };
            await _pollApiViewModelService.SaveAuthorityVote(model, "1");
            var result = _context.Votes.Where(x => x.PollId == model.PollId);
            Assert.Equal(2, result.Count());
        }

        [Fact]
        public async Task Should_SaveSalaryVotes()
        {
            var users = new List<SharePollUserValuesViewModel>
            {
                new SharePollUserValuesViewModel
                {
                    UserId = 2.ToString(),
                    SharePercent = 40
                },
                new SharePollUserValuesViewModel
                {
                    UserId = 3.ToString(),
                    SharePercent = 60
                }
            };
            var model = new SharePollViewModel
            {
                PollId = 1,
                UserId = 1.ToString(),
                Users = users
            };
            await _pollApiViewModelService.SaveSharePoll(model);
            var result = _context.Votes.Where(x => x.PollId == 1);
            Assert.Equal(2, result.Count());
        }
    }
}