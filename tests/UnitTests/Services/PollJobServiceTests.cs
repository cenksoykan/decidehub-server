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
using Moq;
using Newtonsoft.Json;
using Xunit;

namespace UnitTests.Services
{
    public class PollJobServiceTests
    {
        public PollJobServiceTests()
        {
            _context = Helpers.GetContext("test");
            _tenantsDbContext = Helpers.GetTenantContext();
            IPollRepository pollRepository = new EntityPollRepository(_context, _tenantsDbContext);
            IUserRepository userRepository = new EntityUserRepository(_context, null);
            IUserService userService = new UserService(userRepository, null);
            IAsyncRepository<Vote> voteRepository = new EfRepository<Vote>(_context);
            var voteService = new VoteService(voteRepository, userService);
            ITenantRepository tenantRepository = new EntityTenantRepository(_tenantsDbContext);
            IAsyncRepository<Setting> settingRepository = new EfRepository<Setting>(_context);
            ISettingService settingService = new SettingService(settingRepository);
            ITenantService tenantService = new TenantService(tenantRepository, settingService, null);
            var policyService = new PolicyService(new EfRepository<Policy>(_context));
            var emailSenderMock = new Mock<IEmailSender>();
            emailSenderMock.Setup(serv =>
                serv.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()));
            var genericServiceMock = new Mock<IGenericService>();
            genericServiceMock.Setup(serv => serv.GetBaseUrl(It.IsAny<string>()))
                .Returns(Task.FromResult("decidehub.com"));
            IPollService pollService = new PollService(pollRepository, settingService, userService, tenantService,
                voteService, emailSenderMock.Object, genericServiceMock.Object);
            _pollJobService = new PollJobService(pollService, userService, voteService, settingService, pollRepository,
                tenantService, policyService);
        }

        private readonly IPollJobService _pollJobService;
        private readonly ApplicationDbContext _context;
        private readonly TenantsDbContext _tenantsDbContext;

        private Tuple<Poll, Policy, Policy> SetupPolicyPoll(decimal authorityPercent)
        {
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
                UserDetail = new UserDetail {AuthorityPercent = authorityPercent, LanguagePreference = "tr"}
            });
            var oldPolicy = new Policy
            {
                Title = "Old Policy",
                Body = "Old Body",
                UserId = 1.ToString(),
                CreatedAt = DateTime.UtcNow,
                PolicyStatus = PolicyStatus.Active,
                TenantId = "test"
            };
            var newPolicy = new Policy
            {
                Title = "Policy",
                Body = "New Body",
                UserId = 1.ToString(),
                CreatedAt = DateTime.UtcNow,
                PolicyStatus = PolicyStatus.Voting,
                TenantId = "test"
            };
            _context.Policies.Add(oldPolicy);
            _context.Policies.Add(newPolicy);
            _context.SaveChanges();

            var poll = new PolicyChangePoll
            {
                Name = "test",
                Active = true,
                CreateTime = DateTime.UtcNow.AddHours(-12),
                PolicyId = newPolicy.Id,
                QuestionBody = "test dfs",
                TenantId = "test",
                Deadline = DateTime.UtcNow.AddHours(-1)
            };
            _context.Polls.Add(poll);
            _context.SaveChanges();
            return new Tuple<Poll, Policy, Policy>(poll, oldPolicy, newPolicy);
        }

        [Fact]
        public async Task Should_Calculate_AuthorityPercents()
        {
            var tenant = new Tenant
            {
                Id = "test",
                HostName = "test.decidehub.com",
                InActive = false,
                Lang = "tr"
            };
            _tenantsDbContext.Tenants.Add(tenant);
            _tenantsDbContext.SaveChanges();
            var poll = new AuthorityPoll
            {
                Name = "test",
                Active = true,
                CreateTime = DateTime.UtcNow.AddHours(-12),
                QuestionBody = "test dfs",
                TenantId = "test",
                Deadline = DateTime.UtcNow.AddHours(-1)
            };
            _context.Polls.Add(poll);

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
                UserDetail = new UserDetail
                    {AuthorityPercent = 0, InitialAuthorityPercent = 5, LanguagePreference = "tr"}
            });

            _context.Users.Add(new ApplicationUser
            {
                Email = "test2@workhow.com",
                FirstName = "test2",
                LastName = "test2",
                TenantId = "test",
                CreatedAt = DateTime.UtcNow,
                SecurityStamp = new Guid().ToString(),
                EmailConfirmed = true,
                Id = 2.ToString(),
                IsDeleted = false,
                UserDetail = new UserDetail
                    {AuthorityPercent = 0, InitialAuthorityPercent = 3, LanguagePreference = "tr"}
            });
            _context.Users.Add(new ApplicationUser
            {
                Email = "test3@workhow.com",
                FirstName = "test3",
                LastName = "test3",
                TenantId = "test",
                CreatedAt = DateTime.UtcNow,
                SecurityStamp = new Guid().ToString(),
                EmailConfirmed = true,
                Id = 3.ToString(),
                IsDeleted = false,
                UserDetail = new UserDetail
                    {AuthorityPercent = 0, InitialAuthorityPercent = 10, LanguagePreference = "tr"}
            });


            _context.SaveChanges();

            _context.Votes.Add(new Vote {PollId = poll.Id, Value = 500, VoterId = 1.ToString(), VotedUserId = "2"});
            _context.Votes.Add(new Vote {PollId = poll.Id, Value = 500, VoterId = 1.ToString(), VotedUserId = "3"});


            _context.Votes.Add(new Vote
                {PollId = poll.Id, Value = 500, VoterId = 2.ToString(), VotedUserId = 1.ToString()});
            _context.Votes.Add(new Vote
                {PollId = poll.Id, Value = 500, VoterId = 2.ToString(), VotedUserId = 3.ToString()});

            _context.Votes.Add(new Vote
                {PollId = poll.Id, Value = 500, VoterId = 3.ToString(), VotedUserId = 1.ToString()});
            _context.Votes.Add(new Vote
                {PollId = poll.Id, Value = 500, VoterId = 3.ToString(), VotedUserId = 2.ToString()});

            _context.SaveChanges();
            await _pollJobService.CheckPollCompletion();
            var user1 = _context.UserDetails.First(u => u.UserId == "1").AuthorityPercent;
            var user2 = _context.UserDetails.First(u => u.UserId == "2").AuthorityPercent;
            var user3 = _context.UserDetails.First(u => u.UserId == "3").AuthorityPercent;
            Assert.Equal(34.0M, Math.Round(user1, 1));
            Assert.Equal(35.4M, Math.Round(user2, 1));
            Assert.Equal(30.6M, Math.Round(user3, 1));
        }

        [Fact]
        public async Task Should_Calculate_SharePoll_Result()
        {
            var tenant = new Tenant
            {
                Id = "test",
                HostName = "test.decidehub.com",
                InActive = false,
                Lang = "tr"
            };
            _tenantsDbContext.Tenants.Add(tenant);
            _tenantsDbContext.SaveChanges();
            var option = new List<string> {"test2", "test1", "test3", "test4"};
            var poll = new SharePoll
            {
                Name = "test",
                Active = true,
                CreateTime = DateTime.UtcNow.AddHours(-12),
                QuestionBody = "test dfs",
                TenantId = "test",
                Deadline = DateTime.UtcNow.AddHours(-1),
                OptionsJsonString = JsonConvert.SerializeObject(option)
            };
            _context.Polls.Add(poll);
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

            _context.Votes.Add(new Vote {PollId = poll.Id, Value = 70, VotedUserId = "test3", VoterId = 1.ToString()});
            _context.Votes.Add(new Vote {PollId = poll.Id, Value = 0, VotedUserId = "test2", VoterId = 1.ToString()});
            _context.Votes.Add(new Vote {PollId = poll.Id, Value = 30, VotedUserId = "test1", VoterId = 1.ToString()});
            _context.SaveChanges();
            await _pollJobService.CheckPollCompletion();
            var getPoll = _context.Polls.FirstOrDefault(p => p.Id == poll.Id);
            Assert.NotNull(getPoll);
            Assert.Equal("test2: 0.00%\ntest1: 30.00%\ntest3: 70.00%\ntest4: 0.00%", getPoll.Result);
        }

        [Fact]
        public async Task Should_Calculate_EmptySharePoll_Result()
        {
            var tenant = new Tenant
            {
                Id = "test",
                HostName = "test.decidehub.com",
                InActive = false,
                Lang = "tr"
            };
            _tenantsDbContext.Tenants.Add(tenant);
            _tenantsDbContext.SaveChanges();
            var option = new List<string> {"test2", "test1", "test3", "test4"};
            var poll = new SharePoll
            {
                Name = "test",
                Active = true,
                CreateTime = DateTime.UtcNow.AddHours(-12),
                QuestionBody = "test dfs",
                TenantId = "test",
                Deadline = DateTime.UtcNow.AddHours(-1),
                OptionsJsonString = JsonConvert.SerializeObject(option)
            };
            _context.Polls.Add(poll);
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

            await _pollJobService.CheckPollCompletion();
            var getPoll = _context.Polls.FirstOrDefault(p => p.Id == poll.Id);
            Assert.NotNull(getPoll);
            Assert.Equal(PollResults.InsufficientAuthority.ToString(), getPoll.Result);
        }

        [Fact]
        public async Task Should_Calculate_MultipleChoicePoll_Result()
        {
            var tenant = new Tenant
            {
                Id = "test",
                HostName = "test.decidehub.com",
                InActive = false,
                Lang = "tr"
            };
            _tenantsDbContext.Tenants.Add(tenant);
            _tenantsDbContext.SaveChanges();
            var option = new List<string> {"test1", "test2", "test3"};
            var poll = new MultipleChoicePoll
            {
                Name = "test",
                Active = true,
                CreateTime = DateTime.UtcNow.AddHours(-12),
                QuestionBody = "test dfs",
                TenantId = "test",
                Deadline = DateTime.UtcNow.AddHours(-1),
                OptionsJsonString = JsonConvert.SerializeObject(option)
            };
            _context.Polls.Add(poll);
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

            _context.Votes.Add(new Vote {PollId = poll.Id, Value = 0, VoterId = 1.ToString()});
            _context.SaveChanges();
            await _pollJobService.CheckPollCompletion();
            var getPoll = _context.Polls.FirstOrDefault(p => p.Id == poll.Id);
            Assert.NotNull(getPoll);
            Assert.Contains(option[0], getPoll.Result);
        }

        [Fact]
        public async Task Should_Calculate_MultipleChoicePoll_Result_As_UnDecided()
        {
            var tenant = new Tenant
            {
                Id = "test",
                HostName = "test.decidehub.com",
                InActive = false,
                Lang = "tr"
            };
            _tenantsDbContext.Tenants.Add(tenant);
            _tenantsDbContext.SaveChanges();
            var option = new List<string> {"test1", "test2", "test3"};
            var poll = new MultipleChoicePoll
            {
                Name = "test",
                Active = true,
                CreateTime = DateTime.UtcNow.AddHours(-12),
                QuestionBody = "test dfs",
                TenantId = "test",
                Deadline = DateTime.UtcNow.AddHours(-1),
                OptionsJsonString = JsonConvert.SerializeObject(option)
            };
            _context.Polls.Add(poll);
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
                TenantId = "test",
                CreatedAt = DateTime.UtcNow,
                SecurityStamp = new Guid().ToString(),
                EmailConfirmed = false,
                Id = 2.ToString(),
                IsDeleted = false,
                UserDetail = new UserDetail {AuthorityPercent = 1, LanguagePreference = "tr"}
            });
            _context.SaveChanges();
            _context.SaveChanges();

            _context.Votes.Add(new Vote {PollId = poll.Id, Value = 0, VoterId = 1.ToString()});
            _context.Votes.Add(new Vote {PollId = poll.Id, Value = 1, VoterId = "2"});
            _context.SaveChanges();
            await _pollJobService.CheckPollCompletion();
            var getPoll = _context.Polls.FirstOrDefault(p => p.Id == poll.Id);
            Assert.NotNull(getPoll);
            Assert.Contains(PollResults.Undecided.ToString(), getPoll.Result);
        }


        [Fact]
        public async Task Should_Calculate_PolicyChange_PollResult_As_Insufficient()
        {
            var (poll, oldPolicy, newPolicy) = SetupPolicyPoll(0);

            _context.Votes.Add(new Vote {PollId = poll.Id, Value = -1, VoterId = 1.ToString()});
            _context.SaveChanges();

            await _pollJobService.CheckPollCompletion();
            var getPoll = _context.Polls.FirstOrDefault(p => p.Id == poll.Id);
            Assert.Equal(PollResults.InsufficientAuthority.ToString(), getPoll?.Result);
            var getPolicy = _context.Policies.FirstOrDefault(p => p.Id == newPolicy.Id);
            Assert.Equal(PolicyStatus.Rejected, getPolicy?.PolicyStatus);
            var getOldPolicy = _context.Policies.FirstOrDefault(p => p.Id == oldPolicy.Id);
            Assert.Equal(PolicyStatus.Active, getOldPolicy?.PolicyStatus);
        }


        [Fact]
        public async Task Should_Calculate_PolicyChange_PollResult_As_Insufficient_NoVotes()
        {
            var (poll, oldPolicy, newPolicy) = SetupPolicyPoll(1);

            await _pollJobService.CheckPollCompletion();
            var getPoll = _context.Polls.FirstOrDefault(p => p.Id == poll.Id);
            Assert.Equal(PollResults.InsufficientAuthority.ToString(), getPoll?.Result);
            var getPolicy = _context.Policies.FirstOrDefault(p => p.Id == newPolicy.Id);
            Assert.Equal(PolicyStatus.Rejected, getPolicy?.PolicyStatus);
            var getOldPolicy = _context.Policies.FirstOrDefault(p => p.Id == oldPolicy.Id);
            Assert.Equal(PolicyStatus.Active, getOldPolicy?.PolicyStatus);
        }

        [Fact]
        public async Task Should_Calculate_PolicyChange_PollResult_As_Negative()
        {
            var (poll, oldPolicy, newPolicy) = SetupPolicyPoll(1);

            _context.Votes.Add(new Vote {PollId = poll.Id, Value = -1, VoterId = 1.ToString()});
            _context.SaveChanges();

            await _pollJobService.CheckPollCompletion();
            var getPoll = _context.Polls.FirstOrDefault(p => p.Id == poll.Id);
            Assert.Equal(PollResults.Negative.ToString(), getPoll?.Result);
            var getPolicy = _context.Policies.FirstOrDefault(p => p.Id == newPolicy.Id);
            Assert.Equal(PolicyStatus.Rejected, getPolicy?.PolicyStatus);
            var getOldPolicy = _context.Policies.FirstOrDefault(p => p.Id == oldPolicy.Id);
            Assert.Equal(PolicyStatus.Active, getOldPolicy?.PolicyStatus);
        }

        [Fact]
        public async Task Should_Calculate_PolicyChange_PollResult_As_Positive()
        {
            var (poll, oldPolicy, newPolicy) = SetupPolicyPoll(1);

            _context.Votes.Add(new Vote {PollId = poll.Id, Value = 1, VoterId = 1.ToString()});
            _context.SaveChanges();
            await _pollJobService.CheckPollCompletion();

            var getPoll = _context.Polls.FirstOrDefault(p => p.Id == poll.Id);
            Assert.Equal(PollResults.Positive.ToString(), getPoll?.Result);
            var getPolicy = _context.Policies.FirstOrDefault(p => p.Id == newPolicy.Id);
            Assert.Equal(PolicyStatus.Active, getPolicy?.PolicyStatus);
            var getOldPolicy = _context.Policies.FirstOrDefault(p => p.Id == oldPolicy.Id);
            Assert.Equal(PolicyStatus.Overridden, getOldPolicy?.PolicyStatus);
        }

        [Fact]
        public async Task Should_not_Start_AuthorityPoll_By_VotingFrequency_Value()
        {
            var tenant = new Tenant
            {
                Id = "test",
                HostName = "test.decidehub.com",
                InActive = false,
                Lang = "tr"
            };
            _tenantsDbContext.Tenants.Add(tenant);
            _tenantsDbContext.SaveChanges();
            var setting = new Setting
            {
                Key = Settings.VotingFrequency.ToString(),
                Value = "24"
            };
            _context.Settings.Add(setting);
            var poll = new AuthorityPoll
            {
                Name = "test",
                Active = false,
                CreateTime = DateTime.UtcNow.AddDays(-24),
                QuestionBody = "test dfs",
                TenantId = "test",
                Deadline = DateTime.UtcNow.AddDays(-23)
            };
            _context.Polls.Add(poll);
            _context.SaveChanges();

            await _pollJobService.AuthorityPollStart();
            var authorityPolls = _context.AuthorityPolls;
            Assert.Equal(1, authorityPolls.Count());
        }

        [Fact]
        public async Task Should_Start_AuthorityPoll_By_VotingFrequency_Value()
        {
            var tenant = new Tenant
            {
                Id = "test",
                HostName = "test.decidehub.com",
                InActive = false,
                Lang = "tr"
            };
            _tenantsDbContext.Tenants.Add(tenant);
            _tenantsDbContext.SaveChanges();
            var setting = new Setting
            {
                Key = Settings.VotingFrequency.ToString(),
                Value = "24"
            };
            _context.Settings.Add(setting);
            var poll = new AuthorityPoll
            {
                Name = "test",
                Active = false,
                CreateTime = DateTime.UtcNow.AddDays(-25),
                QuestionBody = "test dfs",
                TenantId = "test",
                Deadline = DateTime.UtcNow.AddDays(-24)
            };
            _context.Polls.Add(poll);
            _context.SaveChanges();

            await _pollJobService.AuthorityPollStart();
            var authorityPolls = _context.AuthorityPolls;
            Assert.Equal(2, authorityPolls.Count());
        }
    }
}