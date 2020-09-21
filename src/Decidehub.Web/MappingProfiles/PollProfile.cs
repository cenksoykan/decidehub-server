using System.Collections.Generic;
using AutoMapper;
using Decidehub.Core.Entities;
using Decidehub.Core.Enums;
using Decidehub.Core.Extensions;
using Decidehub.Web.Extensions;
using Decidehub.Web.ViewModels.Api;
using Newtonsoft.Json;

namespace Decidehub.Web.MappingProfiles
{
    /// <inheritdoc />
    public class PollProfile : Profile
    {
        /// <inheritdoc />
        public PollProfile()
        {
            CreateMap<AuthorityPoll, AuthorityPollListViewModel>()
                .ForMember(dest => dest.Description, opts => opts.MapFrom(src => src.QuestionBody));

            CreateMap<AuthorityPollListViewModel, AuthorityPoll>();

            CreateMap<PolicyChangePoll, PolicyChangePollViewModel>()
                .ForMember(dest => dest.PollId, opts => opts.MapFrom(src => src.Id))
                .ForMember(dest => dest.PolicyId, opts => opts.MapFrom(src => src.PolicyId))
                .ForMember(dest => dest.Description, opts => opts.MapFrom(src => src.QuestionBody))
                .ForMember(dest => dest.Type,
                    opts => opts.MapFrom(src => src.PollType.ToString().FirstCharacterToLower()))
                .ForMember(dest => dest.Result, opts => opts.MapFrom(src => GetResult(src.Result)))
                .ForMember(dest => dest.StartedBy,
                    opts => opts.MapFrom(src =>
                        src.Active || src.User == null ? "" : $"{src.User.FirstName} {src.User.LastName}"));

            CreateMap<MultipleChoicePoll, MultipleChoicePollViewModel>()
                .ForMember(dest => dest.PollId, opts => opts.MapFrom(src => src.Id))
                .ForMember(dest => dest.Description, opts => opts.MapFrom(src => src.QuestionBody))
                .ForMember(dest => dest.Type,
                    opts => opts.MapFrom(src => src.PollType.ToString().FirstCharacterToLower()))
                .ForMember(dest => dest.Options,
                    opts => opts.MapFrom(src => JsonConvert.DeserializeObject<List<string>>(src.OptionsJsonString)))
                .ForMember(dest => dest.MultipleChoiceResult,
                    opts => opts.MapFrom(src =>
                        src.PollType == PollTypes.MultipleChoicePoll
                            ? GetResult(src.Result) == null ? src.Result : GetResult(src.Result)
                            : null))
                .ForMember(dest => dest.Result, opts => opts.MapFrom(src => GetResult(src.Result)))
                .ForMember(dest => dest.StartedBy,
                    opts => opts.MapFrom(src =>
                        src.Active || src.User == null ? "" : $"{src.User.FirstName} {src.User.LastName}"));

            CreateMap<SharePoll, SharePollViewModel>()
                .ForMember(dest => dest.PollId, opts => opts.MapFrom(src => src.Id))
                .ForMember(dest => dest.Description, opts => opts.MapFrom(src => src.QuestionBody))
                .ForMember(dest => dest.Type,
                    opts => opts.MapFrom(src => src.PollType.ToString().FirstCharacterToLower()))
                .ForMember(dest => dest.Options,
                    opts => opts.MapFrom(src => JsonConvert.DeserializeObject<List<string>>(src.OptionsJsonString)))
                .ForMember(dest => dest.Result, opts => opts.MapFrom(src => GetResult(src.Result)))
                .ForMember(dest => dest.StartedBy,
                    opts => opts.MapFrom(src =>
                        src.Active || src.User == null ? "" : $"{src.User.FirstName} {src.User.LastName}"));

            CreateMap<Poll, PollListViewModel>()
                .ForMember(dest => dest.UserId, opts => opts.Ignore())
                .ForMember(dest => dest.PollId, opts => opts.MapFrom(src => src.Id))
                .ForMember(dest => dest.Type,
                    opts => opts.MapFrom(src => src.PollType.ToString().FirstCharacterToLower()))
                .ForMember(dest => dest.StartedBy,
                    opts => opts.MapFrom(src =>
                        src.Active || src.User == null ? "" : $"{src.User.FirstName} {src.User.LastName}"))
                .ForMember(dest => dest.Description, opts => opts.MapFrom(src => src.QuestionBody))
                .ForMember(dest => dest.MultipleChoiceResult,
                    opts => opts.MapFrom(src =>
                        src.PollType == PollTypes.MultipleChoicePoll
                            ? GetResult(src.Result) == null ? src.Result : GetResult(src.Result)
                            : null))
                .ForMember(dest => dest.Result, opts => opts.MapFrom(src => GetResult(src.Result)));

            CreateMap<Poll, UserNotVotedPollListViewModel>()
                .ForMember(dest => dest.UserId, opts => opts.Ignore())
                .ForMember(dest => dest.PollId, opts => opts.MapFrom(src => src.Id))
                .ForMember(dest => dest.Type,
                    opts => opts.MapFrom(src => src.PollType.ToString().FirstCharacterToLower()))
                .ForMember(dest => dest.Description, opts => opts.MapFrom(src => src.QuestionBody))
                .ForMember(dest => dest.MultipleChoiceResult,
                    opts => opts.MapFrom(src =>
                        src.PollType == PollTypes.MultipleChoicePoll || src.PollType == PollTypes.SharePoll
                            ? src.OptionsJsonString
                            : null))
                .ForMember(dest => dest.PolicyId,
                    opts => opts.MapFrom(src =>
                        src is PolicyChangePoll ? ((PolicyChangePoll) src).PolicyId : (long?) null))
                .ForMember(dest => dest.Result, opts => opts.MapFrom(src => GetResult(src.Result)))
                .ForMember(dest => dest.StartedBy,
                    opts => opts.MapFrom(src =>
                        src.Active || src.User == null ? "" : $"{src.User.FirstName} {src.User.LastName}"));

            CreateMap<Poll, UserVotedPollListViewModel>()
                .ForMember(dest => dest.UserId, opts => opts.Ignore())
                .ForMember(dest => dest.PollId, opts => opts.MapFrom(src => src.Id))
                .ForMember(dest => dest.Type,
                    opts => opts.MapFrom(src => src.PollType.ToString().FirstCharacterToLower()))
                .ForMember(dest => dest.Description, opts => opts.MapFrom(src => src.QuestionBody))
                .ForMember(dest => dest.MultipleChoiceResult,
                    opts => opts.MapFrom(src =>
                        src.PollType == PollTypes.MultipleChoicePoll
                            ? GetResult(src.Result) == null ? src.Result : GetResult(src.Result)
                            : null))
                .ForMember(dest => dest.Result, opts => opts.MapFrom(src => GetResult(src.Result)))
                .ForMember(dest => dest.StartedBy,
                    opts => opts.MapFrom(src =>
                        src.Active || src.User == null ? "" : $"{src.User.FirstName} {src.User.LastName}"));

            CreateMap<Poll, WaitingPollListViewModel>()
                .ForMember(dest => dest.UserId, opts => opts.Ignore())
                .ForMember(dest => dest.PollId, opts => opts.MapFrom(src => src.Id))
                .ForMember(dest => dest.Type,
                    opts => opts.MapFrom(src => src.PollType.ToString().FirstCharacterToLower()))
                .ForMember(dest => dest.Description, opts => opts.MapFrom(src => src.QuestionBody))
                .ForMember(dest => dest.MultipleChoiceResult,
                    opts => opts.MapFrom(src =>
                        src.PollType == PollTypes.MultipleChoicePoll
                            ? GetResult(src.Result) == null ? src.Result : GetResult(src.Result)
                            : null))
                .ForMember(dest => dest.Result, opts => opts.MapFrom(src => GetResult(src.Result)))
                .ForMember(dest => dest.StartedBy,
                    opts => opts.MapFrom(src =>
                        src.Active || src.User == null ? "" : $"{src.User.FirstName} {src.User.LastName}"));

            CreateMap<Poll, CompletedPollListViewModel>()
                .ForMember(dest => dest.UserId, opts => opts.Ignore())
                .ForMember(dest => dest.PollId, opts => opts.MapFrom(src => src.Id))
                .ForMember(dest => dest.Type,
                    opts => opts.MapFrom(src => src.PollType.ToString().FirstCharacterToLower()))
                .ForMember(dest => dest.Description, opts => opts.MapFrom(src => src.QuestionBody))
                .ForMember(dest => dest.StartedBy,
                    opts => opts.MapFrom(src => src.User == null ? "" : $"{src.User.FirstName} {src.User.LastName}"))
                .ForMember(dest => dest.MultipleChoiceResult,
                    opts => opts.MapFrom(src =>
                        src.PollType == PollTypes.MultipleChoicePoll
                            ? GetResult(src.Result) == null ? src.Result : GetResult(src.Result)
                            : null))
                .ForMember(dest => dest.Result, opts => opts.MapFrom(src => GetResult(src.Result)))
                .ForMember(dest => dest.StartedBy,
                    opts => opts.MapFrom(src =>
                        src.Active || src.User == null ? "" : $"{src.User.FirstName} {src.User.LastName}"));

            CreateMap<AuthorityPoll, PollListViewModel>()
                .ForMember(dest => dest.UserId, opts => opts.Ignore())
                .ForMember(dest => dest.PollId, opts => opts.MapFrom(src => src.Id))
                .ForMember(dest => dest.Type,
                    opts => opts.MapFrom(src => src.PollType.ToString().FirstCharacterToLower()))
                .ForMember(dest => dest.Description, opts => opts.MapFrom(src => src.QuestionBody))
                .ForMember(dest => dest.Result, opts => opts.MapFrom(src => GetResult(src.Result)))
                .ForMember(dest => dest.StartedBy,
                    opts => opts.MapFrom(src =>
                        src.Active || src.User == null ? "" : $"{src.User.FirstName} {src.User.LastName}"));

            CreateMap<SharePoll, PollListViewModel>()
                .ForMember(dest => dest.UserId, opts => opts.Ignore())
                .ForMember(dest => dest.PollId, opts => opts.MapFrom(src => src.Id))
                .ForMember(dest => dest.Type,
                    opts => opts.MapFrom(src => src.PollType.ToString().FirstCharacterToLower()))
                .ForMember(dest => dest.Description, opts => opts.MapFrom(src => src.QuestionBody))
                .ForMember(dest => dest.Result, opts => opts.MapFrom(src => GetResult(src.Result)))
                .ForMember(dest => dest.StartedBy,
                    opts => opts.MapFrom(src =>
                        src.Active || src.User == null ? "" : $"{src.User.FirstName} {src.User.LastName}"));

            CreateMap<AuthorityPoll, AuthorityPollListViewModel>()
                .ForMember(dest => dest.PollId, opts => opts.MapFrom(src => src.Id))
                .ForMember(dest => dest.Type,
                    opts => opts.MapFrom(src => src.PollType.ToString().FirstCharacterToLower()))
                .ForMember(dest => dest.Description, opts => opts.MapFrom(src => src.QuestionBody))
                .ForMember(dest => dest.Result, opts => opts.MapFrom(src => GetResult(src.Result)))
                .ForMember(dest => dest.StartedBy,
                    opts => opts.MapFrom(src =>
                        src.Active || src.User == null ? "" : $"{src.User.FirstName} {src.User.LastName}"));
        }

        private static string GetResult(string result)
        {
            if (result == "Kararsız" || result == PollResults.Undecided.ToString())
                return PollResults.Undecided.DescriptionLang("tr");
            else if (result == "Tamamlanmış" || result == PollResults.Completed.ToString())
                return PollResults.Completed.DescriptionLang("tr");
            else if (result == "Olumsuz" || result == PollResults.Negative.ToString())
                return PollResults.Negative.DescriptionLang("tr");
            else if (result == "Olumlu" || result == PollResults.Positive.ToString())
                return PollResults.Positive.DescriptionLang("tr");
            else if (result == "Yetersiz yetki oranı sebebiyle tamamlanamadı"
                     || result == PollResults.InsufficientAuthority.ToString())
                return PollResults.InsufficientAuthority.DescriptionLang("tr");
            else return result;
        }
    }
}