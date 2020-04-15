using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Decidehub.Core.Entities;
using Decidehub.Core.Identity;
using Decidehub.Core.Interfaces;
using Decidehub.Core.Services;
using Decidehub.Infrastructure.Data;
using Decidehub.Infrastructure.Data.Repositories;
using Xunit;

namespace UnitTests.Services
{
    public class VoteServiceTests
    {
        private readonly ApplicationDbContext _context;
        private readonly IVoteService _voteService;
        public VoteServiceTests()
        {
            _context = Helpers.GetContext("test");
            IAsyncRepository<Vote> voteRepository = new EfRepository<Vote>(_context);
            var userRepository = new EntityUserRepository(_context, null);
            var userService = new UserService(userRepository, null);
            _voteService = new VoteService(voteRepository,userService);
        }
        [Fact]
        public async Task Should_Add_Vote()
        {
            var poll = new PolicyChangePoll
            {
                Name = "test",
                Active = true,
                CreateTime = DateTime.UtcNow,
                QuestionBody = "test dfs",
                TenantId = "test"

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
                UserDetail = new UserDetail { AuthorityPercent = 1, LanguagePreference = "tr" }
            });
            _context.SaveChanges();
            var vote = new Vote
            {
                PollId = poll.Id,
                Value = 1,
                VoterId = 1.ToString()
            };
            await _voteService.AddVote(vote);
            var getVote = _context.Votes.FirstOrDefault(x => x.Id == vote.Id);
            Assert.Equal(vote.PollId, getVote.PollId);
            Assert.Equal(vote.Value, getVote.Value);
            Assert.Equal(vote.VoterId, getVote.VoterId);
        }
        [Fact]
        public async Task Should_GetVotedAuthorityPercentageInPoll()
        {
            var poll = new PolicyChangePoll
            {
                Name = "test",
                Active = true,
                CreateTime = DateTime.UtcNow,
                QuestionBody = "test dfs",
                TenantId = "test"

            };
            _context.Polls.Add(poll);
            var userList = new List<ApplicationUser>
            {
                new ApplicationUser
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
                UserDetail = new UserDetail { AuthorityPercent = 20, LanguagePreference = "tr" }
            },
                new ApplicationUser
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
                UserDetail = new UserDetail { AuthorityPercent = 30, LanguagePreference = "tr" }
            }
            };
            _context.Users.AddRange(userList);
            _context.SaveChanges();
            _context.Votes.Add(new Vote { PollId = poll.Id, Value = 1, VoterId = 1.ToString() });
            _context.SaveChanges();
            var result = await _voteService.GetVotedAuthorityPercentageInPoll(poll);
            Assert.Equal(40, result);
        }
        [Fact]
        public async Task Should_GetVotedUserCountInPoll()
        {
            var poll = new PolicyChangePoll
            {
                Name = "test",
                Active = true,
                CreateTime = DateTime.UtcNow,
                QuestionBody = "test dfs",
                TenantId = "test"

            };
            _context.Polls.Add(poll);
            var userList = new List<ApplicationUser>
            {
                new ApplicationUser
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
                UserDetail = new UserDetail { AuthorityPercent = 20, LanguagePreference = "tr" }
            },
                new ApplicationUser
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
                UserDetail = new UserDetail { AuthorityPercent = 30, LanguagePreference = "tr" }
            }
            };
            _context.Users.AddRange(userList);
            _context.SaveChanges();
            _context.Votes.Add(new Vote { PollId = poll.Id, Value = 1, VoterId = 1.ToString() });
            _context.SaveChanges();
            var count = await _voteService.GetVotedUserCountInPoll(poll.Id,false);
            Assert.Equal(1, count);
        }
        [Fact]
        public async Task Should_GetVotesByPoll()
        {
            var poll = new PolicyChangePoll
            {
                Name = "test",
                Active = true,
                CreateTime = DateTime.UtcNow,
                QuestionBody = "test dfs",
                TenantId = "test"

            };
            _context.Polls.Add(poll);
            var userList = new List<ApplicationUser>
            {
                new ApplicationUser
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
                UserDetail = new UserDetail { AuthorityPercent = 20, LanguagePreference = "tr" }
            },
                new ApplicationUser
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
                UserDetail = new UserDetail { AuthorityPercent = 30, LanguagePreference = "tr" }
            }
            };
            _context.Users.AddRange(userList);
            _context.SaveChanges();
            _context.Votes.Add(new Vote { PollId = poll.Id, Value = 1, VoterId = 1.ToString() });
            _context.Votes.Add(new Vote { PollId = poll.Id, Value = 1, VoterId = 2.ToString() });
            _context.SaveChanges();
            var votes = await _voteService.GetVotesByPoll(poll.Id);
            Assert.Equal(2, votes.Count);
        }
        [Fact]
        public async Task Should_ResetVote()
        {

            var poll = new PolicyChangePoll
            {
                Name = "test",
                Active = true,
                CreateTime = DateTime.UtcNow,
                QuestionBody = "test dfs",
                TenantId = "test"

            };
            _context.Polls.Add(poll);
            var userList = new List<ApplicationUser>
            {
                new ApplicationUser
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
                UserDetail = new UserDetail { AuthorityPercent = 20, LanguagePreference = "tr" }
            },
                new ApplicationUser
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
                UserDetail = new UserDetail { AuthorityPercent = 30, LanguagePreference = "tr" }
            }
            };
            _context.Users.AddRange(userList);
            _context.SaveChanges();
            _context.Votes.Add(new Vote { PollId = poll.Id, Value = 1, VoterId = 1.ToString() });
            _context.Votes.Add(new Vote { PollId = poll.Id, Value = 1, VoterId = 2.ToString() });
            _context.SaveChanges();
            await _voteService.ResetVote(1.ToString(), poll.Id);
            var vote = _context.Votes.FirstOrDefault(x => x.PollId == poll.Id && x.VoterId == "1");
            Assert.Null(vote);
        }
        [Fact]
        public async Task Should_Check_IfUserVotedInPoll()
        {
            var poll = new PolicyChangePoll
            {
                Name = "test",
                Active = true,
                CreateTime = DateTime.UtcNow,
                QuestionBody = "test dfs",
                TenantId = "test"

            };
            _context.Polls.Add(poll);
            var userList = new List<ApplicationUser>
            {
                new ApplicationUser
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
                UserDetail = new UserDetail { AuthorityPercent = 20, LanguagePreference = "tr" }
            },
                new ApplicationUser
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
                UserDetail = new UserDetail { AuthorityPercent = 30, LanguagePreference = "tr" }
            }
            };
            _context.Users.AddRange(userList);
            _context.SaveChanges();
            _context.Votes.Add(new Vote { PollId = poll.Id, Value = 1, VoterId = 1.ToString() });
            _context.SaveChanges();

            var res1 = await _voteService.UserVotedInPoll(1.ToString(), poll.Id);
            var res2 = await _voteService.UserVotedInPoll(2.ToString(), poll.Id);
            Assert.True(res1);
            Assert.False(res2);
        }
        [Fact]
        public async Task Should_DeleteVotesOfPoll()
        {
            var poll = new PolicyChangePoll
            {
                Name = "test",
                Active = true,
                CreateTime = DateTime.UtcNow,
                QuestionBody = "test dfs",
                TenantId = "test"

            };
            _context.Polls.Add(poll);
            var userList = new List<ApplicationUser>
            {
                new ApplicationUser
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
                UserDetail = new UserDetail { AuthorityPercent = 20, LanguagePreference = "tr" }
            }
            };
            _context.Users.AddRange(userList);
            _context.SaveChanges();
            _context.Votes.Add(new Vote { PollId = poll.Id, Value = 1, VoterId = 1.ToString() });
            _context.SaveChanges();

            await _voteService.DeleteVote(poll.Id);
            var result = _context.Votes.Where(x => x.PollId == poll.Id);
            Assert.Equal(0, result.Count());
        }
    }
}
