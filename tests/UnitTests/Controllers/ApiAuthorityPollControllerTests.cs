using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Decidehub.Core.Entities;
using Decidehub.Core.Enums;
using Decidehub.Core.Identity;
using Decidehub.Core.Interfaces;
using Decidehub.Core.Services;
using Decidehub.Infrastructure.Data;
using Decidehub.Infrastructure.Data.Repositories;
using Decidehub.Web.Controllers.Api;
using Decidehub.Web.Interfaces;
using Decidehub.Web.Services;
using Decidehub.Web.ViewModels.Api;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Moq;
using Xunit;

namespace UnitTests.Controllers
{
    public class ApiAuthorityPollControllerTests
    {
        public ApiAuthorityPollControllerTests()
        {
            _context = Helpers.GetContext("test");
            _currentUser = new ApplicationUser
            {
                Email = "test@workhow.com",
                FirstName = "test",
                LastName = "Test",
                CreatedAt = DateTime.UtcNow,
                SecurityStamp = new Guid().ToString(),
                EmailConfirmed = true,
                Id = 1.ToString(),
                IsDeleted = false,
                UserDetail = new UserDetail {AuthorityPercent = 0, LanguagePreference = "tr"}
            };
            _context.Users.Add(_currentUser);
            _context.SaveChanges();
            var tenantsDbContext = Helpers.GetTenantContext();
            IUserRepository userRepository = new EntityUserRepository(_context, null);
            IUserService userService = new UserService(userRepository, null);
            IPollRepository pollRepository = new EntityPollRepository(_context, tenantsDbContext);
            IAsyncRepository<Setting> settingRepository = new EfRepository<Setting>(_context);
            ISettingService settingService = new SettingService(settingRepository);
            ITenantRepository tenantRepository = new EntityTenantRepository(tenantsDbContext);
            ITenantService tenantService = new TenantService(tenantRepository, settingService, null);
            IAsyncRepository<Vote> voteRepository = new EfRepository<Vote>(_context);
            var voteService = new VoteService(voteRepository, userService);
            var tenantProviderMock = new Mock<ITenantProvider>();
            tenantProviderMock.Setup(serv => serv.GetTenantId()).Returns("test");
            var mapper = Helpers.GetMapper();
            IPollService pollService = new PollService(pollRepository, settingService, userService, tenantService,
                voteService, null, null);
            IPollApiViewModelService pollApiViewModelService = new PollApiViewModelService(tenantProviderMock.Object,
                pollService, mapper, voteService, userService, settingService);
            _authorityLocalizerMock = new Mock<IStringLocalizer<ApiAuthorityPollController>>();
            _controller = new ApiAuthorityPollController(userService, pollService, pollApiViewModelService, mapper,
                voteService, _authorityLocalizerMock.Object, settingService, tenantProviderMock.Object);
            var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.PrimarySid, "1")
            }));

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext {User = user}
            };
        }

        private readonly ApplicationDbContext _context;
        private readonly ApiAuthorityPollController _controller;
        private readonly Mock<IStringLocalizer<ApiAuthorityPollController>> _authorityLocalizerMock;
        private readonly ApplicationUser _currentUser;

        [Fact]
        public async Task Should_Check_VotesSum()
        {
            var poll = new AuthorityPoll
            {
                Name = "AuthorityPoll test",
                Active = true,
                CreateTime = DateTime.UtcNow.AddHours(1),
                Deadline = DateTime.UtcNow.AddDays(1),
                QuestionBody = "AuthorityPoll test"
            };
            _context.Polls.Add(poll);
            _context.SaveChanges();
            var model = new AuthorityPollSaveViewModel
            {
                PollId = poll.Id,
                Votes = new List<AuthorityPollUserValues>
                {
                    new AuthorityPollUserValues
                    {
                        UserId = 2.ToString(), Value = 500
                    }
                }
            };

            const string key = "AuthorityPollSumError";
            var localizedString = new LocalizedString(key, key);
            _authorityLocalizerMock.Setup(_ => _[key]).Returns(localizedString);
            var result = await _controller.Vote(model);
            var actionResult = Assert.IsType<BadRequestObjectResult>(result);
            var actionResultList = actionResult.Value as List<ErrorViewModel>;
            Assert.Equal(key, actionResultList[0].Description);
        }   
        
        [Fact]
        public async Task Should_Reject_SelfVotes()
        {
            var poll = new AuthorityPoll
            {
                Name = "AuthorityPoll test",
                Active = true,
                CreateTime = DateTime.UtcNow.AddHours(1),
                Deadline = DateTime.UtcNow.AddDays(1),
                QuestionBody = "AuthorityPoll test"
            };
            _context.Polls.Add(poll);
            _context.SaveChanges();
            var model = new AuthorityPollSaveViewModel
            {
                PollId = poll.Id,
                Votes = new List<AuthorityPollUserValues>
                {
                    new AuthorityPollUserValues
                    {
                        UserId = 1.ToString(), Value = 1000
                    }
                }
            };

            const string key = "AuthorityPollInvalidVoteError";
            var localizedString = new LocalizedString(key, key);
            _authorityLocalizerMock.Setup(_ => _[key]).Returns(localizedString);
            var result = await _controller.Vote(model);
            var actionResult = Assert.IsType<BadRequestObjectResult>(result);
            var actionResultList = actionResult.Value as List<ErrorViewModel>;
            Assert.Equal(key, actionResultList[0].Description);
        } 
        [Fact]
        public async Task Should_Reject_NegativeVotes()
        {
            var poll = new AuthorityPoll
            {
                Name = "AuthorityPoll test",
                Active = true,
                CreateTime = DateTime.UtcNow.AddHours(1),
                Deadline = DateTime.UtcNow.AddDays(1),
                QuestionBody = "AuthorityPoll test"
            };
            _context.Polls.Add(poll);
            _context.SaveChanges();
            var model = new AuthorityPollSaveViewModel
            {
                PollId = poll.Id,
                Votes = new List<AuthorityPollUserValues>
                {
                    new AuthorityPollUserValues
                    {
                        UserId = 2.ToString(), Value = -1
                    }
                }
            };

            const string key = "AuthorityPollInvalidVoteError";
            var localizedString = new LocalizedString(key, key);
            _authorityLocalizerMock.Setup(_ => _[key]).Returns(localizedString);
            var result = await _controller.Vote(model);
            var actionResult = Assert.IsType<BadRequestObjectResult>(result);
            var actionResultList = actionResult.Value as List<ErrorViewModel>;
            Assert.Equal(key, actionResultList[0].Description);
        } 
        
        [Fact]
        public async Task Should_Reject_VotesOutOfRange()
        {
            var poll = new AuthorityPoll
            {
                Name = "AuthorityPoll test",
                Active = true,
                CreateTime = DateTime.UtcNow.AddHours(1),
                Deadline = DateTime.UtcNow.AddDays(1),
                QuestionBody = "AuthorityPoll test"
            };
            _context.Polls.Add(poll);
            _context.SaveChanges();
            var model = new AuthorityPollSaveViewModel
            {
                PollId = poll.Id,
                Votes = new List<AuthorityPollUserValues>
                {
                    new AuthorityPollUserValues
                    {
                        UserId = 2.ToString(), Value = 1001
                    }
                }
            };

            const string key = "AuthorityPollInvalidVoteError";
            var localizedString = new LocalizedString(key, key);
            _authorityLocalizerMock.Setup(_ => _[key]).Returns(localizedString);
            var result = await _controller.Vote(model);
            var actionResult = Assert.IsType<BadRequestObjectResult>(result);
            var actionResultList = actionResult.Value as List<ErrorViewModel>;
            Assert.Equal(key, actionResultList[0].Description);
        }

        [Fact]
        public async Task Should_Create_AuthorityPoll()
        {
            _context.Roles.Add(new ApplicationRole {Id = 1.ToString(), Name = "Admin"});
            _context.UserRoles.Add(new IdentityUserRole<string> {RoleId = 1.ToString(), UserId = _currentUser.Id});


            _context.Users.Add(new ApplicationUser
            {
                Email = "test2@workhow.com",
                FirstName = "Test",
                LastName = "Test",
                CreatedAt = DateTime.UtcNow,
                SecurityStamp = new Guid().ToString(),
                EmailConfirmed = true,
                Id = 2.ToString(),
                IsDeleted = false,
                UserDetail = new UserDetail {AuthorityPercent = 1, LanguagePreference = "tr"}
            });
            _context.Users.Add(new ApplicationUser
            {
                Email = "test2@workhow.com",
                FirstName = "Test2",
                LastName = "Test2",
                CreatedAt = DateTime.UtcNow,
                SecurityStamp = new Guid().ToString(),
                EmailConfirmed = true,
                Id = 3.ToString(),
                IsDeleted = false,
                UserDetail = new UserDetail {AuthorityPercent = 1, LanguagePreference = "tr"}
            });
            _context.SaveChanges();

            _context.SaveChanges();

            var key = "AuthorityPollActivePollError";
            var key2 = "AuthorityDistPoll";
            var localizedString = new LocalizedString(key, key);
            var localizedString2 = new LocalizedString(key2, key2);
            _authorityLocalizerMock.Setup(_ => _[key]).Returns(localizedString);
            _authorityLocalizerMock.Setup(_ => _[key2]).Returns(localizedString2);

            var result = await _controller.NewAuthorityPoll();
            var actionResult = Assert.IsType<OkObjectResult>(result);
            var actionResultObj = actionResult.Value as PollListViewModel;
            var addedPoll = _context.AuthorityPolls.ToList()[0];
            Assert.Equal(actionResultObj.PollId, addedPoll.Id);
            Assert.Equal(actionResultObj.Name, addedPoll.Name);
        }

        [Fact]
        public async Task Should_Get_Next_AuthorityPoll()
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
            var result = await _controller.GetNextAuthorityPollStartDate();
            var actionResult = Assert.IsType<OkObjectResult>(result);
            var actionResultObj = actionResult.Value as DateTime?;

            Assert.Equal(poll.Deadline.AddDays(30), actionResultObj);
        }

        [Fact]
        public async Task Should_Return_Error_IfActiveAuthorityPollExists()
        {
            _context.Roles.Add(new ApplicationRole {Id = 1.ToString(), Name = "Admin"});
            _context.UserRoles.Add(new IdentityUserRole<string> {RoleId = 1.ToString(), UserId = _currentUser.Id});


            _context.Users.Add(new ApplicationUser
            {
                Email = "test2@workhow.com",
                FirstName = "Test",
                LastName = "Test",
                CreatedAt = DateTime.UtcNow,
                SecurityStamp = new Guid().ToString(),
                EmailConfirmed = true,
                Id = 2.ToString(),
                IsDeleted = false,
                UserDetail = new UserDetail {AuthorityPercent = 1, LanguagePreference = "tr"}
            });
            _context.Users.Add(new ApplicationUser
            {
                Email = "test2@workhow.com",
                FirstName = "Test2",
                LastName = "Test2",
                CreatedAt = DateTime.UtcNow,
                SecurityStamp = new Guid().ToString(),
                EmailConfirmed = true,
                Id = 3.ToString(),
                IsDeleted = false,
                UserDetail = new UserDetail {AuthorityPercent = 1, LanguagePreference = "tr"}
            });
            _context.SaveChanges();
            var completedPoll = new AuthorityPoll
            {
                Name = "Authority Poll",
                Active = false,
                CreateTime = DateTime.UtcNow.AddDays(-1),
                Deadline = DateTime.UtcNow.AddDays(-1),
                QuestionBody = "test"
            };
            var activePoll = new AuthorityPoll
            {
                Name = "Authority Poll 2",
                Active = true,
                CreateTime = DateTime.UtcNow,
                Deadline = DateTime.UtcNow.AddDays(3),
                QuestionBody = "test 2"
            };
            _context.AuthorityPolls.Add(completedPoll);
            _context.AuthorityPolls.Add(activePoll);
            _context.SaveChanges();

            var key = "AuthorityPollActivePollError";
            var localizedString = new LocalizedString(key, key);
            _authorityLocalizerMock.Setup(_ => _[key]).Returns(localizedString);

            var result = await _controller.NewAuthorityPoll();
            var actionResult = Assert.IsType<BadRequestObjectResult>(result);
            var actionResultList = actionResult.Value as List<ErrorViewModel>;
            Assert.Equal(key, actionResultList[0].Description);
        }

        [Fact]
        public async Task Should_Return_Error_IfActiveUsersCountLessThan3()
        {
            _context.Roles.Add(new ApplicationRole {Id = 1.ToString(), Name = "Admin"});
            _context.UserRoles.Add(new IdentityUserRole<string> {RoleId = 1.ToString(), UserId = _currentUser.Id});
            _context.SaveChanges();

            var key = "AuthorityPollUserInitialAuthorityError";
            var localizedString = new LocalizedString(key, key);
            _authorityLocalizerMock.Setup(_ => _[key]).Returns(localizedString);
            var result = await _controller.NewAuthorityPoll();
            var actionResult = Assert.IsType<BadRequestObjectResult>(result);
            var actionResultList = actionResult.Value as List<ErrorViewModel>;
            Assert.Equal(key, actionResultList[0].Description);
        }

        [Fact]
        public async Task Should_Return_Error_IfCurrentUserIsNotAdmin()
        {
            var key = "AuthorityDistPoll";
            var key2 = "AuthorityPollUserAdminError";
            var localizedString = new LocalizedString(key, key);
            var localizedString2 = new LocalizedString(key2, key2);
            _authorityLocalizerMock.Setup(_ => _[key]).Returns(localizedString);
            _authorityLocalizerMock.Setup(_ => _[key2]).Returns(localizedString2);

            var result = await _controller.NewAuthorityPoll();
            var actionResult = Assert.IsType<BadRequestObjectResult>(result);
            var actionResultList = actionResult.Value as List<ErrorViewModel>;
            Assert.Equal(key2, actionResultList[0].Description);
        }

        [Fact]
        public async Task Should_Return_Error_IfPollIsCompleted()
        {
            var completedPoll = new AuthorityPoll
            {
                Name = "Authority Poll",
                Active = false,
                CreateTime = DateTime.UtcNow.AddDays(-1),
                Deadline = DateTime.UtcNow.AddDays(-1),
                QuestionBody = "test"
            };
            _context.Polls.Add(completedPoll);
            _context.SaveChanges();

            var model = new AuthorityPollSaveViewModel
            {
                PollId = (int) completedPoll.Id
            };
            var key = "PollCompleted";
            var localizedString = new LocalizedString(key, key);
            _authorityLocalizerMock.Setup(_ => _[key]).Returns(localizedString);
            var result = await _controller.Vote(model);
            var actionResult = Assert.IsType<BadRequestObjectResult>(result);
            var actionResultList = actionResult.Value as List<ErrorViewModel>;
            Assert.Equal(key, actionResultList[0].Description);
        }

        [Fact]
        public async Task Should_Return_Error_IfPollTypeIsNotAuthorityPoll()
        {
            var completedPoll = new PolicyChangePoll
            {
                Name = "PolicyChangePoll test",
                Active = false,
                CreateTime = DateTime.UtcNow,
                Deadline = DateTime.UtcNow.AddDays(-2),
                QuestionBody = "PolicyChangePoll test"
            };
            _context.Polls.Add(completedPoll);
            _context.SaveChanges();

            var model = new AuthorityPollSaveViewModel
            {
                PollId = (int) completedPoll.Id
            };
            var key = "PollTypeNotAuthority";
            var localizedString = new LocalizedString(key, key);
            _authorityLocalizerMock.Setup(_ => _[key]).Returns(localizedString);
            var result = await _controller.Vote(model);
            var actionResult = Assert.IsType<BadRequestObjectResult>(result);
            var actionResultList = actionResult.Value as List<ErrorViewModel>;
            Assert.Equal(key, actionResultList[0].Description);
        }

        [Fact]
        public async Task Should_Return_Error_IfUserAddedAfterPollStarted()
        {
            var poll = new AuthorityPoll
            {
                Name = "AuthorityPoll test",
                Active = true,
                CreateTime = DateTime.UtcNow.AddHours(-1),
                Deadline = DateTime.UtcNow.AddDays(1),
                QuestionBody = "AuthorityPoll test"
            };
            _context.Polls.Add(poll);
            _context.SaveChanges();

            var model = new AuthorityPollSaveViewModel
            {
                PollId = (int) poll.Id
            };
            var key = "UserCannotVoteAfterAddedPollStart";
            var localizedString = new LocalizedString(key, key);
            _authorityLocalizerMock.Setup(_ => _[key]).Returns(localizedString);
            var result = await _controller.Vote(model);
            var actionResult = Assert.IsType<BadRequestObjectResult>(result);
            var actionResultList = actionResult.Value as List<ErrorViewModel>;
            Assert.Equal(key, actionResultList[0].Description);
        }

        [Fact]
        public async Task Should_Return_Error_IfUserVotedInPoll()
        {
            var poll = new AuthorityPoll
            {
                Name = "AuthorityPoll test",
                Active = true,
                CreateTime = DateTime.UtcNow.AddHours(1),
                Deadline = DateTime.UtcNow.AddDays(1),
                QuestionBody = "AuthorityPoll test"
            };
            _context.Polls.Add(poll);
            _context.SaveChanges();
            _context.Votes.Add(new Vote
                {PollId = poll.Id, Value = 1000, VoterId = 1.ToString(), VotedUserId = 2.ToString()});
            _context.SaveChanges();
            var model = new AuthorityPollSaveViewModel
            {
                PollId = poll.Id
            };
            var key = "PollRecordExist";
            var localizedString = new LocalizedString(key, key);
            _authorityLocalizerMock.Setup(_ => _[key]).Returns(localizedString);
            var result = await _controller.Vote(model);
            var actionResult = Assert.IsType<BadRequestObjectResult>(result);
            var actionResultList = actionResult.Value as List<ErrorViewModel>;
            Assert.Equal(key, actionResultList[0].Description);
        }

        [Fact]
        public async Task Should_ReturnError_IfPollDoesntExists()
        {
            var model = new AuthorityPollSaveViewModel
            {
                PollId = 1
            };
            var key = "PollNotFound";
            var localizedString = new LocalizedString(key, key);
            _authorityLocalizerMock.Setup(_ => _[key]).Returns(localizedString);
            var result = await _controller.Vote(model);
            var actionResult = Assert.IsType<BadRequestObjectResult>(result);
            var actionResultList = actionResult.Value as List<ErrorViewModel>;
            Assert.Equal(key, actionResultList[0].Description);
        }

        [Fact]
        public async Task Should_Save_Vote()
        {
            var poll = new AuthorityPoll
            {
                Name = "AuthorityPoll test",
                Active = true,
                CreateTime = DateTime.UtcNow.AddHours(1),
                Deadline = DateTime.UtcNow.AddDays(1),
                QuestionBody = "AuthorityPoll test"
            };
            _context.Polls.Add(poll);
            _context.SaveChanges();
            var model = new AuthorityPollSaveViewModel
            {
                PollId = poll.Id,
                Votes = new List<AuthorityPollUserValues>
                {
                    new AuthorityPollUserValues
                    {
                        UserId = 2.ToString(), Value = 500
                    },
                    new AuthorityPollUserValues
                    {
                        UserId = 3.ToString(), Value = 500
                    }
                }
            };
            var result = await _controller.Vote(model);
            var vote = _context.Votes.Where(x => x.VoterId == 1.ToString());
            var actionResult = Assert.IsType<OkObjectResult>(result);
            var actionResultObj = actionResult.Value as PollListViewModel;
            Assert.Equal(2, vote.Count());
            Assert.True(actionResultObj.UserVoted);
        }
    }
}