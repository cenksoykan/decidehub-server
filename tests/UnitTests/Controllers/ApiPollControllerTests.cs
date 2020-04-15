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
using Decidehub.Web.Extensions;
using Decidehub.Web.Interfaces;
using Decidehub.Web.Services;
using Decidehub.Web.ViewModels.Api;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Moq;
using Newtonsoft.Json;
using Xunit;

namespace UnitTests.Controllers
{
    public class ApiPollControllerTests
    {
        public ApiPollControllerTests()
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
                UserDetail = new UserDetail {AuthorityPercent = 30, LanguagePreference = "tr"}
            };
            _context.Users.Add(_currentUser);
            _context.SaveChanges();
            var tenantsDbContext = Helpers.GetTenantContext();
            IPollRepository pollRepository = new EntityPollRepository(_context, tenantsDbContext);
            IAsyncRepository<Setting> settingRepository = new EfRepository<Setting>(_context);
            ISettingService settingService = new SettingService(settingRepository);
            IUserRepository userRepository = new EntityUserRepository(_context, null);
            IUserService userService = new UserService(userRepository, null);
            ITenantRepository tenantRepository = new EntityTenantRepository(tenantsDbContext);
            IAsyncRepository<Vote> voteRepository = new EfRepository<Vote>(_context);
            var voteService = new VoteService(voteRepository, userService);
            ITenantService tenantService = new TenantService(tenantRepository, settingService, null);
            IPollService pollService = new PollService(pollRepository, settingService, userService, tenantService,
                voteService, null, null);
            var mapper = Helpers.GetMapper();
            var tenantProviderMock = new Mock<ITenantProvider>();
            tenantProviderMock.Setup(serv => serv.GetTenantId()).Returns("test");
            _pollLocalizerMock = new Mock<IStringLocalizer<ApiPollController>>();
            IPollApiViewModelService pollApiViewModelService = new PollApiViewModelService(tenantProviderMock.Object,
                pollService, mapper, voteService, userService, settingService);
            var genericServiceMock = new Mock<IGenericService>();
            genericServiceMock.Setup(serv => serv.GetBaseUrl(null)).Returns(Task.FromResult("decidehub.com"));
            _controller = new ApiPollController(pollService, mapper, userService, pollApiViewModelService,
                _pollLocalizerMock.Object, voteService, genericServiceMock.Object, tenantProviderMock.Object);
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
        private readonly ApiPollController _controller;
        private readonly ApplicationUser _currentUser;
        private readonly Mock<IStringLocalizer<ApiPollController>> _pollLocalizerMock;

        [Fact]
        public async Task Should_Get_AuthorityPollValues()
        {
            var completedPoll = new AuthorityPoll
            {
                Name = "Authority Poll",
                Active = false,
                CreateTime = DateTime.UtcNow.AddDays(-1),
                Deadline = DateTime.UtcNow.AddDays(-1),
                QuestionBody = "test"
            };
            _context.AuthorityPolls.Add(completedPoll);
            _context.SaveChanges();
            var result = await _controller.GetPollValues(completedPoll.Id);
            var actionResult = Assert.IsType<OkObjectResult>(result);
            var actionResultObj = actionResult.Value as AuthorityPollListViewModel;
            Assert.NotNull(actionResultObj);
            Assert.Equal(completedPoll.Id, actionResultObj.PollId);
            Assert.Equal(completedPoll.Name, actionResultObj.Name);
            Assert.Equal(completedPoll.Deadline, actionResultObj.Deadline);
            Assert.Equal(completedPoll.QuestionBody, actionResultObj.Description);
            Assert.Equal(PollListTypes.Completed.ToString().FirstCharacterToLower(), actionResultObj.ListType);
            Assert.Empty(actionResultObj.Users);
        }

        [Fact]
        public async Task Should_Get_CompletedPolls()
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
                Name = "test",
                Active = true,
                CreateTime = new DateTime(2018, 7, 3),
                Deadline = DateTime.UtcNow.AddHours(25),
                QuestionBody = "test 123"
            };
            _context.Polls.Add(poll);
            _context.Polls.Add(poll2);
            _context.SaveChanges();
            var result = await _controller.GetCompletedPolls();
            var actionResult = Assert.IsType<OkObjectResult>(result);
            var actionResultObj = actionResult.Value as IList<CompletedPollListViewModel>;
            Assert.NotNull(actionResultObj);
            Assert.Equal(1, actionResultObj.Count);
        }

        [Fact]
        public async Task Should_Get_MultipleChoicePollValues()
        {
            var poll = new MultipleChoicePoll
            {
                Name = "test",
                OptionsJsonString = JsonConvert.SerializeObject(new List<string> {"a", "b", "c"}),
                Active = true,
                CreateTime = DateTime.UtcNow,
                Deadline = DateTime.UtcNow.AddDays(2)
            };

            _context.MultipleChoicePolls.Add(poll);
            _context.SaveChanges();
            var result = await _controller.GetPollValues(poll.Id);
            var actionResult = Assert.IsType<OkObjectResult>(result);
            var actionResultObj = actionResult.Value as MultipleChoicePollViewModel;
            Assert.NotNull(actionResultObj);
            Assert.Equal(poll.Id, actionResultObj.PollId);
            Assert.Equal(poll.Name, actionResultObj.Name);
            Assert.Equal(poll.Deadline, actionResultObj.Deadline);
            Assert.Equal(poll.QuestionBody, actionResultObj.Description);
            Assert.Equal(PollListTypes.UserNotVoted.ToString().FirstCharacterToLower(), actionResultObj.ListType);
            Assert.Equal(JsonConvert.DeserializeObject(poll.OptionsJsonString), actionResultObj.Options);
        }

        [Fact]
        public async Task Should_Get_PolicyChangePollValues()
        {
            var poll = new PolicyChangePoll
            {
                Name = "PolicyChangePoll test",
                Active = true,
                CreateTime = DateTime.UtcNow.AddHours(1),
                Deadline = DateTime.UtcNow.AddDays(1),
                QuestionBody = "PolicyChangePoll test"
            };
            _context.Polls.Add(poll);
            _context.SaveChanges();
            var result = await _controller.GetPollValues(poll.Id);
            var actionResult = Assert.IsType<OkObjectResult>(result);
            var actionResultObj = actionResult.Value as PollListViewModel;

            Assert.NotNull(actionResultObj);
            Assert.Equal(poll.Id, actionResultObj.PollId);
            Assert.Equal(poll.Name, actionResultObj.Name);
            Assert.Equal(poll.Deadline, actionResultObj.Deadline);
            Assert.Equal(poll.QuestionBody, actionResultObj.Description);
            Assert.Equal(PollListTypes.UserNotVoted.ToString().FirstCharacterToLower(), actionResultObj.ListType);
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
            var result = await _controller.GetPollStatus(poll.Id);
            var actionResult = Assert.IsType<OkObjectResult>(result);
            var actionResultList = actionResult.Value as PollStatusViewModel;
            Assert.NotNull(actionResultList);
            Assert.Single(actionResultList.NotVotedUsers);
            Assert.Single(actionResultList.VotedUsers);
            Assert.Equal(poll.Id, actionResultList.PollId);
            Assert.Equal(poll.Name, actionResultList.PollName);
        }

        [Fact]
        public async Task Should_Get_UserNotVotedPolls()
        {
            var poll = new PolicyChangePoll
            {
                Name = "PolicyChangePoll test",
                Active = true,
                CreateTime = DateTime.UtcNow.AddHours(1),
                Deadline = DateTime.UtcNow.AddDays(1),
                QuestionBody = "PolicyChangePoll test"
            };
            var poll2 = new MultipleChoicePoll
            {
                Name = "test",
                OptionsJsonString = JsonConvert.SerializeObject(new List<string> {"a", "b", "c"}),
                Active = true,
                CreateTime = DateTime.UtcNow,
                Deadline = DateTime.UtcNow.AddDays(1)
            };
            _context.Polls.Add(poll);
            _context.Polls.Add(poll2);
            _context.SaveChanges();

            _context.Votes.Add(new Vote {PollId = poll.Id, VoterId = _currentUser.Id, Value = -1});
            _context.SaveChanges();

            var result = await _controller.GetUserNotVotedPolls();
            var actionResult = Assert.IsType<OkObjectResult>(result);
            var actionResultObj = actionResult.Value as IList<UserNotVotedPollListViewModel>;
            Assert.NotNull(actionResultObj);
            Assert.Equal(poll2.Id, actionResultObj[0].PollId);
        }

        [Fact]
        public async Task Should_Get_UserVotedPolls()
        {
            var poll = new PolicyChangePoll
            {
                Name = "PolicyChangePoll test",
                Active = true,
                CreateTime = DateTime.UtcNow.AddHours(1),
                Deadline = DateTime.UtcNow.AddDays(1),
                QuestionBody = "PolicyChangePoll test"
            };
            var poll2 = new MultipleChoicePoll
            {
                Name = "test",
                OptionsJsonString = JsonConvert.SerializeObject(new List<string> {"a", "b", "c"}),
                Active = true,
                CreateTime = DateTime.UtcNow,
                Deadline = DateTime.UtcNow.AddDays(1)
            };
            _context.Polls.Add(poll);
            _context.Polls.Add(poll2);
            _context.SaveChanges();

            _context.Votes.Add(new Vote {PollId = poll2.Id, VoterId = _currentUser.Id, Value = -1});
            _context.SaveChanges();

            var result = await _controller.GetUserNotVotedPolls();
            var actionResult = Assert.IsType<OkObjectResult>(result);
            var actionResultObj = actionResult.Value as IList<UserNotVotedPollListViewModel>;
            Assert.NotNull(actionResultObj);
            Assert.Equal(poll.Id, actionResultObj[0].PollId);
        }

        [Fact]
        public async Task Should_Remove_PollAndVotes()
        {
            var completedPoll = new PolicyChangePoll
            {
                Name = "PolicyChangePoll test",
                Active = true,
                CreateTime = DateTime.UtcNow,
                Deadline = DateTime.UtcNow.AddDays(2),
                QuestionBody = "PolicyChangePoll test",
                UserId = 1.ToString()
            };
            _context.Polls.Add(completedPoll);
            _context.SaveChanges();
            _context.Votes.Add(new Vote {PollId = completedPoll.Id, Value = -1, VoterId = _currentUser.Id});
            _context.SaveChanges();
            var result = await _controller.RemovePoll(completedPoll.Id);
            Assert.IsType<OkObjectResult>(result);
            var poll = _context.Polls.FirstOrDefault(x => x.Id == completedPoll.Id);
            var votes = _context.Votes.Where(x => x.PollId == completedPoll.Id);
            Assert.Null(poll);
            Assert.Equal(0, votes.Count());
        }

        [Fact]
        public async Task Should_Reset_Vote()
        {
            var poll = new PolicyChangePoll
            {
                Name = "PolicyChangePoll test",
                Active = true,
                CreateTime = DateTime.UtcNow.AddHours(1),
                Deadline = DateTime.UtcNow.AddDays(1),
                QuestionBody = "PolicyChangePoll test"
            };
            _context.Polls.Add(poll);
            _context.SaveChanges();
            _context.Votes.Add(new Vote {PollId = poll.Id, Value = -1, VoterId = _currentUser.Id});
            _context.SaveChanges();
            var result = await _controller.ResetVote(poll.Id);
            Assert.IsType<OkObjectResult>(result);
            var votes = _context.Votes.Where(c => c.PollId == poll.Id);
            Assert.Equal(0, votes.Count());
        }

        [Fact]
        public async Task Should_Return_Error_If_PollStatusPoll_DoesntExist()
        {
            var key = "PollNotFound";
            var localizedString = new LocalizedString(key, key);
            _pollLocalizerMock.Setup(_ => _[key]).Returns(localizedString);
            var result = await _controller.GetPollStatus(1);
            var actionResult = Assert.IsType<BadRequestObjectResult>(result);
            var actionResultList = actionResult.Value as List<ErrorViewModel>;
            Assert.NotNull(actionResultList);
            Assert.Equal(key, actionResultList[0].Description);
        }

        [Fact]
        public async Task Should_Return_Error_If_ResetVotePoll_DoesntExist()
        {
            var key = "PollNotFound";
            var localizedString = new LocalizedString(key, key);
            _pollLocalizerMock.Setup(_ => _[key]).Returns(localizedString);
            var result = await _controller.ResetVote(2);
            var actionResult = Assert.IsType<NotFoundObjectResult>(result);
            var actionResultList = actionResult.Value as List<ErrorViewModel>;
            Assert.NotNull(actionResultList);
            Assert.Equal(key, actionResultList[0].Description);
        }

        [Fact]
        public async Task Should_Return_Error_If_ToBeRemovedPoll_DoesntExist()
        {
            var key = "PollNotFound";
            var localizedString = new LocalizedString(key, key);
            _pollLocalizerMock.Setup(_ => _[key]).Returns(localizedString);
            var result = await _controller.RemovePoll(1);
            var actionResult = Assert.IsType<NotFoundObjectResult>(result);
            var actionResultList = actionResult.Value as List<ErrorViewModel>;
            Assert.NotNull(actionResultList);
            Assert.Equal(key, actionResultList[0].Description);
        }
        //[Fact]
        //public async Task Should_CheckFirstAuthorityPoll()
        //{

        //    var poll = new AuthorityPoll()
        //    {
        //        Name = "AuthorityPoll2 test",
        //        Active = true,
        //        CreateTime = DateTime.UtcNow.AddDays(-2),
        //        Deadline = DateTime.UtcNow,
        //        QuestionBody = "AuthorityPoll2 test"

        //    };

        //    _context.AuthorityPolls.Add(poll);
        //    _context.SaveChanges();
        //    _context.Roles.Add(new Decidehub.Core.Identity.ApplicationRole { Id = 1, Name = "Admin" });
        //    _context.UserRoles.Add(new Microsoft.AspNetCore.Identity.IdentityUserRole<string> { RoleId = 1.ToString(), UserId = currentUser.Id });
        //    _context.SaveChanges();

        //    var result = await _controller.CheckFirstAuthorityPoll();
        //    var actionResult = Assert.IsType<OkObjectResult>(result);
        //    var actionResultObj = Convert.ToBoolean(actionResult.Value);
        //    Assert.True(actionResultObj);
        //}
        [Fact]
        public async Task Should_Return_Error_IfPollDoesntExist()
        {
            var key = "PollNotFound";
            var localizedString = new LocalizedString(key, key);
            _pollLocalizerMock.Setup(_ => _[key]).Returns(localizedString);
            var result = await _controller.GetPollValues(1);
            var actionResult = Assert.IsType<NotFoundObjectResult>(result);
            var actionResultList = actionResult.Value as List<ErrorViewModel>;
            Assert.NotNull(actionResultList);
            Assert.Equal(key, actionResultList[0].Description);
        }

        [Fact]
        public async Task Shouldnt_Remove_CompletedPoll()
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

            var key = "CantRemoveCompletedPoll";
            var localizedString = new LocalizedString(key, key);
            _pollLocalizerMock.Setup(_ => _[key]).Returns(localizedString);
            var result = await _controller.RemovePoll(completedPoll.Id);
            var actionResult = Assert.IsType<BadRequestObjectResult>(result);
            var actionResultList = actionResult.Value as List<ErrorViewModel>;
            Assert.NotNull(actionResultList);
            Assert.Equal(key, actionResultList[0].Description);
        }

        [Fact]
        public async Task Shouldnt_Remove_Poll_IfCurrentUserDidntStartPoll()
        {
            var completedPoll = new PolicyChangePoll
            {
                Name = "PolicyChangePoll test",
                Active = true,
                CreateTime = DateTime.UtcNow,
                Deadline = DateTime.UtcNow.AddDays(2),
                QuestionBody = "PolicyChangePoll test",
                UserId = 2.ToString()
            };
            _context.Polls.Add(completedPoll);
            _context.SaveChanges();

            var key = "RemovePollUserError";
            var localizedString = new LocalizedString(key, key);
            _pollLocalizerMock.Setup(_ => _[key]).Returns(localizedString);
            var result = await _controller.RemovePoll(completedPoll.Id);
            var actionResult = Assert.IsType<BadRequestObjectResult>(result);
            var actionResultList = actionResult.Value as List<ErrorViewModel>;
            Assert.NotNull(actionResultList);
            Assert.Equal(key, actionResultList[0].Description);
        }
    }
}