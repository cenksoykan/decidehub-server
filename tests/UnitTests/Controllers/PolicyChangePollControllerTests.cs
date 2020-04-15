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
using Decidehub.Web.ViewModels.Api;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Moq;
using Xunit;

namespace UnitTests.Controllers
{
    public class PolicyChangePollControllerTests
    {
        public PolicyChangePollControllerTests()
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
            _policyChangeLocalizerMock = new Mock<IStringLocalizer<ApiPolicyChangePollController>>();
            _controller = new ApiPolicyChangePollController(pollService, mapper, voteService,
                _policyChangeLocalizerMock.Object, userService);
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
        private readonly ApiPolicyChangePollController _controller;
        private readonly Mock<IStringLocalizer<ApiPolicyChangePollController>> _policyChangeLocalizerMock;
        private readonly ApplicationUser _currentUser;

        [Fact]
        public async Task Shold_Return_Error_IfVoteModelStateInvalid()
        {
            _controller.ModelState.AddModelError("PollId", "Required");
            _controller.ModelState.AddModelError("PollValue", "Required");
            var model = new PolicyChangePollVoteViewModel
            {
                PollValue = 1
            };
            var result = await _controller.Vote(model);
            var actionResult = Assert.IsType<BadRequestObjectResult>(result);

            Assert.Equal(typeof(List<ErrorViewModel>), actionResult.Value.GetType());
        }

        [Fact]
        public async Task Should_Return_Error_If_PollDoesntExists()
        {
            var model = new PolicyChangePollVoteViewModel
            {
                PollId = 1,
                PollValue = 1
            };
            var key = "PollNotFound";
            var localizedString = new LocalizedString(key, key);
            _policyChangeLocalizerMock.Setup(_ => _[key]).Returns(localizedString);
            var result = await _controller.Vote(model);
            var actionResult = Assert.IsType<BadRequestObjectResult>(result);
            var actionResultList = actionResult.Value as List<ErrorViewModel>;
            Assert.Equal(key, actionResultList[0].Description);
        }

        [Fact]
        public async Task Should_Return_Error_IfPollIsCompleted()
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

            var model = new PolicyChangePollVoteViewModel
            {
                PollId = completedPoll.Id,
                PollValue = 1
            };
            var key = "PollCompleted";
            var localizedString = new LocalizedString(key, key);
            _policyChangeLocalizerMock.Setup(_ => _[key]).Returns(localizedString);
            var result = await _controller.Vote(model);
            var actionResult = Assert.IsType<BadRequestObjectResult>(result);
            var actionResultList = actionResult.Value as List<ErrorViewModel>;
            Assert.Equal(key, actionResultList[0].Description);
        }

        [Fact]
        public async Task Should_Return_Error_IfPollTypeIsNotPolicyChange()
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

            var model = new PolicyChangePollVoteViewModel
            {
                PollId = (int) completedPoll.Id,
                PollValue = 1
            };
            var key = "PollTypeNotPolicyChange";
            var localizedString = new LocalizedString(key, key);
            _policyChangeLocalizerMock.Setup(_ => _[key]).Returns(localizedString);
            var result = await _controller.Vote(model);
            var actionResult = Assert.IsType<BadRequestObjectResult>(result);
            var actionResultList = actionResult.Value as List<ErrorViewModel>;
            Assert.Equal(key, actionResultList[0].Description);
        }

        [Fact]
        public async Task Should_Return_Error_IfUserAddedAfterPollStarted()
        {
            var poll = new PolicyChangePoll
            {
                Name = "PolicyChangePoll test",
                Active = true,
                CreateTime = DateTime.UtcNow.AddHours(-1),
                Deadline = DateTime.UtcNow.AddDays(1),
                QuestionBody = "PolicyChangePoll test"
            };
            _context.Polls.Add(poll);
            _context.SaveChanges();

            var model = new PolicyChangePollVoteViewModel
            {
                PollId = poll.Id,
                PollValue = 1
            };
            var key = "UserCannotVoteAfterAddedPollStart";
            var localizedString = new LocalizedString(key, key);
            _policyChangeLocalizerMock.Setup(_ => _[key]).Returns(localizedString);
            var result = await _controller.Vote(model);
            var actionResult = Assert.IsType<BadRequestObjectResult>(result);
            var actionResultList = actionResult.Value as List<ErrorViewModel>;
            Assert.Equal(key, actionResultList[0].Description);
        }

        [Fact]
        public async Task Should_Return_Error_IfUserIsNotaVoter()
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
            var model = new PolicyChangePollVoteViewModel
            {
                PollId = poll.Id,
                PollValue = 1
            };
            var key = "PollVoterError";
            var localizedString = new LocalizedString(key, key);
            _policyChangeLocalizerMock.Setup(_ => _[key]).Returns(localizedString);
            var result = await _controller.Vote(model);
            var actionResult = Assert.IsType<BadRequestObjectResult>(result);
            var actionResultList = actionResult.Value as List<ErrorViewModel>;
            Assert.Equal(key, actionResultList[0].Description);
        }

        [Fact]
        public async Task Should_Return_Error_IfUserVotedInPoll()
        {
            _currentUser.UserDetail.AuthorityPercent = 30;
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
            _context.Votes.Add(new Vote {PollId = poll.Id, Value = 1, VoterId = 1.ToString()});
            _context.SaveChanges();
            var model = new PolicyChangePollVoteViewModel
            {
                PollId = poll.Id,
                PollValue = 1
            };
            var key = "PollRecordExist";
            var localizedString = new LocalizedString(key, key);
            _policyChangeLocalizerMock.Setup(_ => _[key]).Returns(localizedString);
            var result = await _controller.Vote(model);
            var actionResult = Assert.IsType<BadRequestObjectResult>(result);
            var actionResultList = actionResult.Value as List<ErrorViewModel>;
            Assert.Equal(key, actionResultList[0].Description);
        }

        [Fact]
        public async Task Should_Save_Vote()
        {
            _currentUser.UserDetail.AuthorityPercent = 30;
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
            var model = new PolicyChangePollVoteViewModel
            {
                PollId = poll.Id,
                PollValue = 1
            };
            var result = await _controller.Vote(model);
            var vote = _context.Votes.FirstOrDefault(x => x.PollId == poll.Id && x.Value == model.PollValue);
            var actionResult = Assert.IsType<OkObjectResult>(result);
            var actionResultObj = actionResult.Value as PollListViewModel;
            Assert.NotNull(vote);
            Assert.True(actionResultObj.UserVoted);
        }
    }
}