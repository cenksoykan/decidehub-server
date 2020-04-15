using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Decidehub.Core.Entities;
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
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Moq;
using Newtonsoft.Json;
using Xunit;

namespace UnitTests.Controllers
{
    public class ApiMultipleChoicePollControllerTests
    {
        public ApiMultipleChoicePollControllerTests()
        {
            _context = Helpers.GetContext("test");
            _currentUser = new ApplicationUser
            {
                Email = "test@workhow.com",
                FirstName = "test",
                LastName = "Test",
                CreatedAt = DateTime.UtcNow,
                SecurityStamp = new Guid().ToString(),
                EmailConfirmed = false,
                Id = 1.ToString(),
                IsDeleted = false,
                UserDetail = new UserDetail {AuthorityPercent = 0, LanguagePreference = "tr"}
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
            _multipleChoiceLocalizerMock = new Mock<IStringLocalizer<ApiMultipleChoicePollController>>();
            IPollApiViewModelService pollApiViewModelService = new PollApiViewModelService(tenantProviderMock.Object,
                pollService, mapper, voteService, userService, settingService);
            _controller = new ApiMultipleChoicePollController(pollService, pollApiViewModelService, mapper, voteService,
                _multipleChoiceLocalizerMock.Object, userService);
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
        private readonly ApiMultipleChoicePollController _controller;
        private readonly Mock<IStringLocalizer<ApiMultipleChoicePollController>> _multipleChoiceLocalizerMock;
        private readonly ApplicationUser _currentUser;

        [Fact]
        public async Task Should_Create_MultipleChoicePoll()
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

            var model = new MultipleChoicePollViewModel
            {
                Name = "test",
                Options = new List<string> {"a", "b", "c"}
            };
            var result = await _controller.NewMultipleChoicePoll(model);
            var actionResult = Assert.IsType<OkObjectResult>(result);
            var actionResultObj = actionResult.Value as MultipleChoicePollViewModel;
            var poll = _context.MultipleChoicePolls.ToList()[0];
            Assert.Equal(actionResultObj.PollId, poll.Id);
            Assert.Equal(actionResultObj.Name, poll.Name);
            Assert.Equal(actionResultObj.Options, JsonConvert.DeserializeObject<List<string>>(poll.OptionsJsonString));
        }

        [Fact]
        public async Task Should_Return_Error_If_ActiveAuthorityPoll_Exists()
        {
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
            _multipleChoiceLocalizerMock.Setup(_ => _[key]).Returns(localizedString);

            var model = new MultipleChoicePollViewModel
            {
                Name = "test"
            };
            var result = await _controller.NewMultipleChoicePoll(model);
            var actionResult = Assert.IsType<BadRequestObjectResult>(result);
            var actionResultList = actionResult.Value as List<ErrorViewModel>;
            Assert.Equal(key, actionResultList[0].Description);
        }

        [Fact]
        public async Task Should_Return_Error_If_AnyOptionsContainsEmptyValue()
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

            var key = "PollOptionCountError";
            var localizedString = new LocalizedString(key, key);
            _multipleChoiceLocalizerMock.Setup(_ => _[key]).Returns(localizedString);
            var model = new MultipleChoicePollViewModel
            {
                Name = "test",
                Options = new List<string> {"a", ""}
            };
            var result = await _controller.NewMultipleChoicePoll(model);
            var actionResult = Assert.IsType<BadRequestObjectResult>(result);
            var actionResultList = actionResult.Value as List<ErrorViewModel>;
            Assert.Equal(key, actionResultList[0].Description);
        }

        [Fact]
        public async Task Should_Return_Error_If_AuthorityPoll_IsNotCompleted()
        {
            var key = "CantStartPollBeforeAuthorityComplete";
            var localizedString = new LocalizedString(key, key);
            _multipleChoiceLocalizerMock.Setup(_ => _[key]).Returns(localizedString);

            var model = new MultipleChoicePollViewModel
            {
                Name = "test"
            };
            var result = await _controller.NewMultipleChoicePoll(model);
            var actionResult = Assert.IsType<BadRequestObjectResult>(result);
            var actionResultList = actionResult.Value as List<ErrorViewModel>;
            Assert.Equal(key, actionResultList[0].Description);
        }

        [Fact]
        public async Task Should_Return_Error_If_OptionsCountLessThen2()
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

            var key = "PollOptionCountError";
            var localizedString = new LocalizedString(key, key);
            _multipleChoiceLocalizerMock.Setup(_ => _[key]).Returns(localizedString);
            var model = new MultipleChoicePollViewModel
            {
                Name = "test",
                Options = new List<string> {"a"}
            };
            var result = await _controller.NewMultipleChoicePoll(model);
            var actionResult = Assert.IsType<BadRequestObjectResult>(result);
            var actionResultList = actionResult.Value as List<ErrorViewModel>;
            Assert.Equal(key, actionResultList[0].Description);
        }

        [Fact]
        public async Task Should_Return_Error_If_PollDoesntExists()
        {
            var model = new MultipleChoicePollVoteViewModel
            {
                PollId = 1,
                Value = 1
            };
            var key = "PollNotFound";
            var localizedString = new LocalizedString(key, key);
            _multipleChoiceLocalizerMock.Setup(_ => _[key]).Returns(localizedString);
            var result = await _controller.Vote(model);
            var actionResult = Assert.IsType<BadRequestObjectResult>(result);
            var actionResultList = actionResult.Value as List<ErrorViewModel>;
            Assert.Equal(key, actionResultList[0].Description);
        }

        [Fact]
        public async Task Should_Return_Error_IfPollIsCompleted()
        {
            var completedPoll = new MultipleChoicePoll
            {
                Name = "test",
                OptionsJsonString = JsonConvert.SerializeObject(new List<string> {"a", "b", "c"}),
                Active = false,
                CreateTime = DateTime.UtcNow,
                Deadline = DateTime.UtcNow.AddDays(-2)
            };

            _context.Polls.Add(completedPoll);
            _context.SaveChanges();

            var model = new MultipleChoicePollVoteViewModel
            {
                PollId = completedPoll.Id,
                Value = 1
            };
            var key = "PollCompleted";
            var localizedString = new LocalizedString(key, key);
            _multipleChoiceLocalizerMock.Setup(_ => _[key]).Returns(localizedString);
            var result = await _controller.Vote(model);
            var actionResult = Assert.IsType<BadRequestObjectResult>(result);
            var actionResultList = actionResult.Value as List<ErrorViewModel>;
            Assert.Equal(key, actionResultList[0].Description);
        }

        [Fact]
        public async Task Should_Return_Error_IfPollTypeIsNotMultipleChoice()
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

            var model = new MultipleChoicePollVoteViewModel
            {
                PollId = (int) completedPoll.Id,
                Value = 1
            };
            var key = "PollTypeNotMultipleChoice";
            var localizedString = new LocalizedString(key, key);
            _multipleChoiceLocalizerMock.Setup(_ => _[key]).Returns(localizedString);
            var result = await _controller.Vote(model);
            var actionResult = Assert.IsType<BadRequestObjectResult>(result);
            var actionResultList = actionResult.Value as List<ErrorViewModel>;
            Assert.Equal(key, actionResultList[0].Description);
        }

        [Fact]
        public async Task Should_Return_Error_IfUserAddedAfterPollStarted()
        {
            var poll = new MultipleChoicePoll
            {
                Name = "test",
                OptionsJsonString = JsonConvert.SerializeObject(new List<string> {"a", "b", "c"}),
                Active = true,
                CreateTime = DateTime.UtcNow.AddHours(-1),
                Deadline = DateTime.UtcNow.AddDays(1)
            };
            _context.Polls.Add(poll);
            _context.SaveChanges();

            var model = new MultipleChoicePollVoteViewModel
            {
                PollId = poll.Id,
                Value = 1
            };
            var key = "UserCannotVoteAfterAddedPollStart";
            var localizedString = new LocalizedString(key, key);
            _multipleChoiceLocalizerMock.Setup(_ => _[key]).Returns(localizedString);
            var result = await _controller.Vote(model);
            var actionResult = Assert.IsType<BadRequestObjectResult>(result);
            var actionResultList = actionResult.Value as List<ErrorViewModel>;
            Assert.Equal(key, actionResultList[0].Description);
        }

        [Fact]
        public async Task Should_Return_Error_IfUserIsNotaVoter()
        {
            var poll = new MultipleChoicePoll
            {
                Name = "test",
                OptionsJsonString = JsonConvert.SerializeObject(new List<string> {"a", "b", "c"}),
                Active = true,
                CreateTime = DateTime.UtcNow.AddHours(1),
                Deadline = DateTime.UtcNow.AddDays(1)
            };
            _context.Polls.Add(poll);
            _context.SaveChanges();
            var model = new MultipleChoicePollVoteViewModel
            {
                PollId = poll.Id,
                Value = 1
            };
            var key = "PollVoterError";
            var localizedString = new LocalizedString(key, key);
            _multipleChoiceLocalizerMock.Setup(_ => _[key]).Returns(localizedString);
            var result = await _controller.Vote(model);
            var actionResult = Assert.IsType<BadRequestObjectResult>(result);
            var actionResultList = actionResult.Value as List<ErrorViewModel>;
            Assert.Equal(key, actionResultList[0].Description);
        }

        [Fact]
        public async Task Should_Return_Error_IfUserVotedInPoll()
        {
            _currentUser.UserDetail.AuthorityPercent = 30;
            var poll = new MultipleChoicePoll
            {
                Name = "test",
                OptionsJsonString = JsonConvert.SerializeObject(new List<string> {"a", "b", "c"}),
                Active = false,
                CreateTime = DateTime.UtcNow.AddHours(1),
                Deadline = DateTime.UtcNow.AddDays(1)
            };

            _context.Polls.Add(poll);
            _context.SaveChanges();
            _context.Votes.Add(new Vote {PollId = poll.Id, Value = 1, VoterId = 1.ToString()});
            _context.SaveChanges();
            var model = new MultipleChoicePollVoteViewModel
            {
                PollId = poll.Id,
                Value = 1
            };
            var key = "PollRecordExist";
            var localizedString = new LocalizedString(key, key);
            _multipleChoiceLocalizerMock.Setup(_ => _[key]).Returns(localizedString);
            var result = await _controller.Vote(model);
            var actionResult = Assert.IsType<BadRequestObjectResult>(result);
            var actionResultList = actionResult.Value as List<ErrorViewModel>;
            Assert.Equal(key, actionResultList[0].Description);
        }

        [Fact]
        public async Task Should_Save_Vote()
        {
            _currentUser.UserDetail.AuthorityPercent = 30;
            var poll = new MultipleChoicePoll
            {
                Name = "test",
                OptionsJsonString = JsonConvert.SerializeObject(new List<string> {"a", "b", "c"}),
                Active = true,
                CreateTime = DateTime.UtcNow.AddHours(1),
                Deadline = DateTime.UtcNow.AddDays(1)
            };
            _context.Polls.Add(poll);
            _context.SaveChanges();
            var model = new MultipleChoicePollVoteViewModel
            {
                PollId = poll.Id,
                Value = 1
            };
            var result = await _controller.Vote(model);
            var vote = _context.Votes.FirstOrDefault(x =>
                x.PollId == poll.Id && x.Value == model.Value && x.VoterId == _currentUser.Id);
            var actionResult = Assert.IsType<OkObjectResult>(result);
            var actionResultObj = actionResult.Value as PollListViewModel;
            Assert.NotNull(vote);
            Assert.True(actionResultObj.UserVoted);
        }
    }
}