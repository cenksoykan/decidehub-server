using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Decidehub.Core.Entities;
using Decidehub.Core.Enums;
using Decidehub.Core.Identity;
using Decidehub.Infrastructure.Data;
using Decidehub.Infrastructure.Data.Repositories;
using Xunit;

namespace UnitTests.Repositories
{
    public class EntityPollRepositoryTests
    {
        private static ApplicationDbContext GetContextAndPollTestData()
        {
            var context = Helpers.GetContext("test");
            var pollList = new List<Poll>
            {
                new PolicyChangePoll
                {
                    Id = 1,
                    Name = "test",
                    Active = true,
                    CreateTime = new DateTime(2018, 7, 3),
                    Deadline = DateTime.UtcNow.AddDays(2),
                    QuestionBody = "test 123",
                    TenantId = "test"
                },
                new PolicyChangePoll
                {
                    Id = 2,
                    Name = "test2",
                    Active = false,
                    CreateTime = new DateTime(2018, 7, 2),
                    Deadline = new DateTime(2018, 7, 2).AddDays(2),
                    QuestionBody = "test 1234",
                    TenantId = "test"
                },
                new PolicyChangePoll
                {
                    Id = 3,
                    Name = "test3",
                    Active = false,
                    CreateTime = new DateTime(2018, 7, 1),
                    Deadline = new DateTime(2018, 7, 1).AddDays(2),
                    QuestionBody = "test 12345",
                    TenantId = "test"
                },
                new SharePoll
                {
                    Id = 4,
                    Name = "sharePoll",
                    Active = false,
                    CreateTime = new DateTime(2018, 7, 2),
                    Deadline = new DateTime(2018, 7, 2).AddDays(2),
                    QuestionBody = "test paylasim",
                    TenantId = "test"
                },

                new SharePoll
                {
                    Id = 5,
                    Name = "SharePoll test",
                    Active = false,
                    CreateTime = new DateTime(2018, 8, 2),
                    Deadline = new DateTime(2018, 8, 2).AddDays(2),
                    QuestionBody = "test paylasim",
                    TenantId = "test2"
                },
                new PolicyChangePoll
                {
                    Id = 6,
                    Name = "PolicyChangePoll test",
                    Active = true,
                    CreateTime = new DateTime(2018, 6, 2),
                    Deadline = new DateTime(2018, 6, 2).AddDays(2),
                    QuestionBody = "PolicyChangePoll test",
                    TenantId = "test2"
                },
                new PolicyChangePoll
                {
                    Id = 7,
                    Name = "PolicyChangePoll test with no tenant",
                    Active = true,
                    CreateTime = new DateTime(2018, 6, 2),
                    Deadline = new DateTime(2018, 6, 2).AddDays(2),
                    QuestionBody = "PolicyChangePoll test",
                    TenantId = "test"
                }
            };
            context.Polls.AddRange(pollList);
            context.SaveChanges();
            return context;
        }

        [Fact]
        public async Task Should_Add_Poll()
        {
            var context = Helpers.GetContext("test");
            var tenantsContext = Helpers.GetTenantContext();
            var pollRepository = new EntityPollRepository(context, tenantsContext);
            var poll = new PolicyChangePoll
            {
                Name = "test",
                Active = true,
                CreateTime = DateTime.UtcNow,
                Deadline = DateTime.UtcNow.AddDays(3),
                QuestionBody = "test dfs"
            };

            var addedPoll = await pollRepository.AddPoll(poll);
            var polls = context.PolicyChangePolls;
            Assert.Equal(1, polls.Count());
            Assert.Equal(poll.Id, addedPoll.Id);
            Assert.True(addedPoll.Active);
            Assert.Equal("test", addedPoll.TenantId);
        }

        [Fact]
        public async Task Should_Check_Active_Poll()
        {
            var context = GetContextAndPollTestData();
            var tenantsContext = Helpers.GetTenantContext();
            var pollRepository = new EntityPollRepository(context, tenantsContext);
            var hasActivePolicyChangePoll = await pollRepository.HasActivePollOfType<PolicyChangePoll>();
            var hasActiveSharePoll = await pollRepository.HasActivePollOfType<SharePoll>();
            Assert.True(hasActivePolicyChangePoll);
            Assert.False(hasActiveSharePoll);
        }

        [Fact]
        public async Task Should_Delete_Poll()
        {
            var context = GetContextAndPollTestData();
            var tenantsContext = Helpers.GetTenantContext();
            var pollRepository = new EntityPollRepository(context, tenantsContext);
            await pollRepository.DeletePoll(1);
            var poll = context.Polls.FirstOrDefault(x => x.Id == 1);
            Assert.Null(poll);
        }

        [Fact]
        public async Task Should_End_Poll()
        {
            var context = GetContextAndPollTestData();
            var tenantsContext = Helpers.GetTenantContext();
            var pollRepository = new EntityPollRepository(context, tenantsContext);
            await pollRepository.EndPoll(1);
            var poll = context.Polls.FirstOrDefault(x => x.Id == 1);
            Assert.False(poll.Active);
            Assert.True(DateTime.UtcNow > poll.Deadline);
        }

        [Fact]
        public async Task Should_Get_Active_Polls()
        {
            var context = GetContextAndPollTestData();
            var tenantsContext = Helpers.GetTenantContext();
            var pollRepository = new EntityPollRepository(context, tenantsContext);
            var activePolls = await pollRepository.GetActivePolls(false, null);
            Assert.Equal(2, activePolls.Count);
        }

        [Fact]
        public async Task Should_Get_ActivePoll_Count()
        {
            var context = GetContextAndPollTestData();
            var tenantsContext = Helpers.GetTenantContext();
            var pollRepository = new EntityPollRepository(context, tenantsContext);
            var count = await pollRepository.GetActivePollCount();
            Assert.Equal(2, count);
        }

        [Fact]
        public async Task Should_Get_AllActivePoll_Count()
        {
            var context = GetContextAndPollTestData();
            var tenantsContext = Helpers.GetTenantContext();
            var pollRepository = new EntityPollRepository(context, tenantsContext);
            var count = await pollRepository.GetActivePollCount(true);
            Assert.Equal(3, count);
        }

        [Fact]
        public async Task Should_Get_AllActivePolls()
        {
            var context = GetContextAndPollTestData();
            var tenantsContext = Helpers.GetTenantContext();
            var pollRepository = new EntityPollRepository(context, tenantsContext);
            var activePolls = await pollRepository.GetActivePolls(true, null);
            Assert.Equal(3, activePolls.Count);
        }

        [Fact]
        public async Task Should_Get_AllCompleted_Poll_Count()
        {
            var context = GetContextAndPollTestData();
            var tenantsContext = Helpers.GetTenantContext();
            var pollRepository = new EntityPollRepository(context, tenantsContext);
            var count = await pollRepository.GetCompletedPollCount(true);
            Assert.Equal(4, count);
        }

        [Fact]
        public async Task Should_Get_AllPublic_Polls()
        {
            var context = Helpers.GetContext("test");
            var pollList = new List<Poll>
            {
                new PolicyChangePoll
                {
                    Id = 1,
                    Name = "test",
                    Active = true,
                    CreateTime = new DateTime(2018, 7, 3),
                    Deadline = DateTime.UtcNow.AddDays(2),
                    QuestionBody = "test 123"
                },
                new PolicyChangePoll
                {
                    Id = 2,
                    Name = "test2",
                    Active = false,
                    CreateTime = new DateTime(2018, 7, 2),
                    Deadline = new DateTime(2018, 7, 2).AddDays(2),
                    QuestionBody = "test 1234"
                },
                new PolicyChangePoll
                {
                    Id = 3,
                    Name = "test3",
                    Active = false,
                    CreateTime = new DateTime(2018, 7, 1),
                    Deadline = new DateTime(2018, 7, 1).AddDays(2),
                    QuestionBody = "test 12345",
                    TenantId = "test2"
                }
            };
            context.Polls.AddRange(pollList);
            context.SaveChanges();

            var tenantsContext = Helpers.GetTenantContext();
            var pollRepository = new EntityPollRepository(context, tenantsContext);
            var publicPolls = await pollRepository.GetPublicPolls();
            Assert.Equal(3, publicPolls.Count);
        }

        [Fact]
        public async Task Should_Get_Completed_Poll_Count()
        {
            var context = GetContextAndPollTestData();
            var tenantsContext = Helpers.GetTenantContext();
            var pollRepository = new EntityPollRepository(context, tenantsContext);
            var count = await pollRepository.GetCompletedPollCount();
            Assert.Equal(3, count);
        }

        [Fact]
        public async Task Should_Get_Completed_Polls()
        {
            var context = GetContextAndPollTestData();
            var tenantsContext = Helpers.GetTenantContext();
            var pollRepository = new EntityPollRepository(context, tenantsContext);
            var completedPolls = await pollRepository.GetCompletedPolls(null);
            Assert.Equal(3, completedPolls.Count());
        }

        [Fact]
        public async Task Should_Get_CompletedPolls_SpecifiedCount()
        {
            var context = GetContextAndPollTestData();
            var tenantsContext = Helpers.GetTenantContext();
            var pollRepository = new EntityPollRepository(context, tenantsContext);
            var completedPolls = await pollRepository.GetCompletedPolls(2);
            Assert.Equal(2, completedPolls.Count());
        }

        [Fact]
        public async Task Should_Get_DifferentTenant_ActivePolls()
        {
            var context = GetContextAndPollTestData();
            var tenantsContext = Helpers.GetTenantContext();
            var pollRepository = new EntityPollRepository(context, tenantsContext);
            var activePolls = await pollRepository.GetActivePolls(true, "test2");
            Assert.Equal(1, activePolls.Count);
            Assert.Equal("test2", activePolls[0].TenantId);
        }

        [Fact]
        public async Task Should_Get_LatestVote_ByType()
        {
            var context = GetContextAndPollTestData();
            var tenantsContext = Helpers.GetTenantContext();
            var pollRepository = new EntityPollRepository(context, tenantsContext);
            var latestPoll = await pollRepository.GetLastPollOfType<PolicyChangePoll>(null);
            Assert.NotNull(latestPoll);
            Assert.Equal("test2", latestPoll.Name);
        }

        [Fact]
        public async Task Should_Get_NotVotedUsers()
        {
            var context = GetContextAndPollTestData();
            var tenantsContext = Helpers.GetTenantContext();
            var pollRepository = new EntityPollRepository(context, tenantsContext);
            context.Users.Add(new ApplicationUser
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
            context.Users.Add(new ApplicationUser
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
            context.Users.Add(new ApplicationUser
            {
                Email = "test2@workhow.com",
                FirstName = "test",
                LastName = "Test",
                TenantId = "test",
                CreatedAt = DateTime.UtcNow,
                SecurityStamp = new Guid().ToString(),
                EmailConfirmed = false,
                Id = 3.ToString(),
                IsDeleted = true,
                UserDetail = new UserDetail {AuthorityPercent = 1, LanguagePreference = "tr"}
            });
            context.SaveChanges();

            context.Votes.Add(new Vote {PollId = 1, Value = 1, VoterId = 1.ToString()});
            context.SaveChanges();

            var users = await pollRepository.GetNotVotedUsers(1);
            Assert.Equal(1, users.Count);
            Assert.DoesNotContain(users, x => x.IsDeleted);
        }

        [Fact]
        public async Task Should_Get_Poll()
        {
            var context = GetContextAndPollTestData();
            var tenantsContext = Helpers.GetTenantContext();
            var pollRepository = new EntityPollRepository(context, tenantsContext);
            var poll = await pollRepository.GetPoll(1);
            Assert.NotNull(poll);
        }

        [Fact]
        public async Task Should_Get_Poll_Since_Inclusive()
        {
            var context = GetContextAndPollTestData();
            var tenantsContext = Helpers.GetTenantContext();
            var pollRepository = new EntityPollRepository(context, tenantsContext);
            var polls = await pollRepository.GetPollsSince(new DateTime(2018, 7, 4), null);
            Assert.Equal(3, polls.Count);
        }

        [Fact]
        public async Task Should_Get_Poll_TenantIndependent()
        {
            var context = GetContextAndPollTestData();
            var tenantsContext = Helpers.GetTenantContext();
            var pollRepository = new EntityPollRepository(context, tenantsContext);
            var poll = await pollRepository.GetPoll(5, true);
            Assert.NotNull(poll);
            Assert.NotEqual("test", poll.TenantId);
        }

        [Fact]
        public async Task Should_Get_PollCount_ByType()
        {
            var context = GetContextAndPollTestData();
            var tenantsContext = Helpers.GetTenantContext();
            var pollRepository = new EntityPollRepository(context, tenantsContext);
            var count = await pollRepository.GetPollCountByType<PolicyChangePoll>(null);
            Assert.Equal(4, count);
        }

        [Fact]
        public async Task Should_Get_Tenant_NotVotedUsers()
        {
            var context = GetContextAndPollTestData();
            var tenantsContext = Helpers.GetTenantContext();
            var pollRepository = new EntityPollRepository(context, tenantsContext);
            context.Users.Add(new ApplicationUser
            {
                Email = "test@workhow.com",
                FirstName = "test",
                LastName = "Test",
                TenantId = "test2",
                CreatedAt = DateTime.UtcNow,
                SecurityStamp = new Guid().ToString(),
                EmailConfirmed = false,
                Id = 1.ToString(),
                IsDeleted = false,
                UserDetail = new UserDetail {AuthorityPercent = 1, LanguagePreference = "tr"}
            });
            context.Users.Add(new ApplicationUser
            {
                Email = "test2@workhow.com",
                FirstName = "test",
                LastName = "Test",
                TenantId = "test2",
                CreatedAt = DateTime.UtcNow,
                SecurityStamp = new Guid().ToString(),
                EmailConfirmed = false,
                Id = 2.ToString(),
                IsDeleted = false,
                UserDetail = new UserDetail {AuthorityPercent = 1, LanguagePreference = "tr"}
            });
            context.Users.Add(new ApplicationUser
            {
                Email = "test2@workhow.com",
                FirstName = "test",
                LastName = "Test",
                CreatedAt = DateTime.UtcNow,
                SecurityStamp = new Guid().ToString(),
                EmailConfirmed = false,
                Id = 3.ToString(),
                IsDeleted = false,
                UserDetail = new UserDetail {AuthorityPercent = 1, LanguagePreference = "tr"}
            });
            context.SaveChanges();

            context.Votes.Add(new Vote {PollId = 1, Value = 1, VoterId = 1.ToString(), TenantId = "test2"});
            context.SaveChanges();

            var users = await pollRepository.GetNotVotedUsers(1, "test2");
            Assert.Equal(1, users.Count);
            Assert.DoesNotContain(users, x => x.TenantId == "test");
        }

        [Fact]
        public async Task Should_Get_TenantActivePoll_Count()
        {
            var context = GetContextAndPollTestData();
            var tenantsContext = Helpers.GetTenantContext();
            var pollRepository = new EntityPollRepository(context, tenantsContext);
            var count = await pollRepository.GetActivePollCount(tenantId: "test2");
            Assert.Equal(1, count);
        }

        [Fact]
        public async Task Should_Get_TenantCompleted_Poll_Count()
        {
            var context = GetContextAndPollTestData();
            var tenantsContext = Helpers.GetTenantContext();
            var pollRepository = new EntityPollRepository(context, tenantsContext);
            var count = await pollRepository.GetCompletedPollCount(tenantId: "test2");
            Assert.Equal(1, count);
        }

        [Fact]
        public async Task Should_Get_TenantPollCount_ByType()
        {
            var context = GetContextAndPollTestData();
            var tenantsContext = Helpers.GetTenantContext();
            var pollRepository = new EntityPollRepository(context, tenantsContext);
            var count = await pollRepository.GetPollCountByType<SharePoll>( "test2");
            Assert.Equal(1, count);
        }

        [Fact]
        public async Task Should_Get_User_NotVotedPolls()
        {
            var context = GetContextAndPollTestData();
            var tenantsContext = Helpers.GetTenantContext();
            var pollRepository = new EntityPollRepository(context, tenantsContext);

            context.Users.Add(new ApplicationUser
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
            context.SaveChanges();

            context.Votes.Add(new Vote {PollId = 1, Value = 1, VoterId = 1.ToString()});
            context.SaveChanges();

            var notVotedPolls = await pollRepository.GetUserNotVotedPolls(1.ToString());

            Assert.Equal(1, notVotedPolls.Count);
            Assert.DoesNotContain(notVotedPolls, x => x.Id == 1);
        }

        [Fact]
        public async Task Should_Get_User_VotedPolls()
        {
            var context = GetContextAndPollTestData();
            var tenantsContext = Helpers.GetTenantContext();
            var pollRepository = new EntityPollRepository(context, tenantsContext);
            context.Users.Add(new ApplicationUser
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
            context.SaveChanges();

            context.Votes.Add(new Vote {PollId = 1, Value = 1, VoterId = 1.ToString()});
            context.SaveChanges();

            var votedPolls = await pollRepository.GetUserVotedPolls(1.ToString());
            Assert.Equal(1, votedPolls.Count);
            Assert.Contains(votedPolls, x => x.Id == 1);
        }

        [Fact]
        public async Task Should_Set_PollResult()
        {
            var context = GetContextAndPollTestData();
            var tenantsContext = Helpers.GetTenantContext();
            var pollRepository = new EntityPollRepository(context, tenantsContext);
            await pollRepository.SetPollResult(1, "test");
            var poll = context.Polls.FirstOrDefault(x => x.Id == 1);
            Assert.Equal("test", poll.Result);
        }
    }
}