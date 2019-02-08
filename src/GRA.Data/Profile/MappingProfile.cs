﻿using System.Linq;

namespace GRA.Data.Profile
{
    public class MappingProfile : AutoMapper.Profile
    {
        public MappingProfile()
        {
            CreateMap<Model.Answer, Domain.Model.Answer>().ReverseMap();
            CreateMap<Model.AuthorizationCode, Domain.Model.AuthorizationCode>().ReverseMap();
            CreateMap<Model.AvatarBundle, Domain.Model.AvatarBundle>()
                .ForMember(dest => dest.AvatarItems,
                    opt => opt.MapFrom(src => src.AvatarBundleItems.Select(_ => _.AvatarItem)))
                .ReverseMap();
            CreateMap<Model.AvatarColor, Domain.Model.AvatarColor>().ReverseMap();
            CreateMap<Model.AvatarElement, Domain.Model.AvatarElement>().ReverseMap();
            CreateMap<Model.AvatarItem, Domain.Model.AvatarItem>().ReverseMap();
            CreateMap<Model.AvatarLayer, Domain.Model.AvatarLayer>()
                .ForMember(dest => dest.AvatarColors, opt =>
                {
                    opt.MapFrom(src => src.AvatarColors.OrderBy(_ => _.SortOrder));
                    opt.ExplicitExpansion();
                })
                .ForMember(dest => dest.AvatarItems, opt =>
                {
                    opt.MapFrom(src => src.AvatarItems.OrderBy(_ => _.SortOrder));
                    opt.ExplicitExpansion();
                })
                .ReverseMap();
            CreateMap<Model.Badge, Domain.Model.Badge>().ReverseMap();
            CreateMap<Model.Book, Domain.Model.Book>().ReverseMap();
            CreateMap<Model.Branch, Domain.Model.Branch>().ReverseMap();
            CreateMap<Model.Broadcast, Domain.Model.Broadcast>().ReverseMap();
            CreateMap<Model.Carousel, Domain.Model.Carousel>().ReverseMap();
            CreateMap<Model.CarouselItem, Domain.Model.CarouselItem>().ReverseMap();
            CreateMap<Model.Category, Domain.Model.Category>().ReverseMap();
            CreateMap<Model.Challenge, Domain.Model.Challenge>()
                .ForMember(dest => dest.Categories, opt => opt.MapFrom(
                    src => src.ChallengeCategories.Select(_ => _.Category)))
                .ReverseMap();
            CreateMap<Model.ChallengeGroup, Domain.Model.ChallengeGroup>()
                .ForMember(dest => dest.ChallengeIds, opt => opt.MapFrom(
                    src => src.ChallengeGroupChallenges.Select(_ => _.ChallengeId).ToList()))
                .ReverseMap();
            CreateMap<Model.ChallengeTask, Domain.Model.ChallengeTask>().ReverseMap();
            CreateMap<Model.DailyLiteracyTip, Domain.Model.DailyLiteracyTip>().ReverseMap();
            CreateMap<Model.DailyLiteracyTipImage, Domain.Model.DailyLiteracyTipImage>().ReverseMap();
            CreateMap<Model.DashboardContent, Domain.Model.DashboardContent>().ReverseMap();
            CreateMap<Model.Drawing, Domain.Model.Drawing>().ReverseMap();
            CreateMap<Model.DrawingCriterion, Domain.Model.DrawingCriterion>()
                .ForMember(dest => dest.ProgramIds, opt => opt.MapFrom(src
                => src.CriterionPrograms.Select(_ => _.ProgramId).ToList()))
                .ReverseMap();
            CreateMap<Model.PrizeWinner, Domain.Model.PrizeWinner>()
                .ForMember(dest => dest.PrizeName,
                opt => opt.MapFrom(src => src.Drawing.Name ?? src.Trigger.AwardPrizeName))
                .ReverseMap();
            CreateMap<Model.EmailReminder, Domain.Model.EmailReminder>().ReverseMap();
            CreateMap<Model.EmailSubscriptionAuditLog, Domain.Model.EmailSubscriptionAuditLog>()
                .ForMember(dest => dest.CreatedByName, opt => opt
                    .MapFrom(src => $"{src.CreatedByUser.FirstName} {src.CreatedByUser.LastName}"))
                .ReverseMap();
            CreateMap<Model.Event, Domain.Model.Event>()
                .ForMember(dest => dest.Challenge, opt => opt.Ignore())
                .ForMember(dest => dest.ChallengeGroup, opt => opt.Ignore())
                .ReverseMap();
            CreateMap<Model.GroupInfo, Domain.Model.GroupInfo>().ReverseMap();
            CreateMap<Model.GroupType, Domain.Model.GroupType>().ReverseMap();
            CreateMap<Model.Language, Domain.Model.Language>().ReverseMap();
            CreateMap<Model.Location, Domain.Model.Location>().ReverseMap();
            CreateMap<Model.Mail, Domain.Model.Mail>().ReverseMap();
            CreateMap<Model.NewsCategory, Domain.Model.NewsCategory>()
                .ForMember(dest => dest.PostCount, opt => opt.MapFrom(src => src.Posts.Count))
                .ReverseMap();
            CreateMap<Model.NewsPost, Domain.Model.NewsPost>().ReverseMap();
            CreateMap<Model.Notification, Domain.Model.Notification>().ReverseMap();
            CreateMap<Model.Page, Domain.Model.Page>().ReverseMap();
            CreateMap<Model.PointTranslation, Domain.Model.PointTranslation>().ReverseMap();
            CreateMap<Model.Program, Domain.Model.Program>().ReverseMap();
            CreateMap<Model.PsAgeGroup, Domain.Model.PsAgeGroup>().ReverseMap();
            CreateMap<Model.PsBlackoutDate, Domain.Model.PsBlackoutDate>().ReverseMap();
            CreateMap<Model.PsBranchSelection, Domain.Model.PsBranchSelection>()
                .ForMember(dest => dest.StartsAt, opt => opt.MapFrom(src =>
                    src.ScheduleStartTime.ToShortTimeString()))
                .ForMember(dest => dest.EndsAt, opt => opt.MapFrom(src =>
                    src.ScheduleStartTime.AddMinutes(src.ScheduleDuration).ToShortTimeString()))
                .ReverseMap();
            CreateMap<Model.PsSettings, Domain.Model.PsSettings>().ReverseMap();
            CreateMap<Model.PsKit, Domain.Model.PsKit>().ReverseMap();
            CreateMap<Model.PsKitImage, Domain.Model.PsKitImage>().ReverseMap();
            CreateMap<Model.PsPerformer, Domain.Model.PsPerformer>().ReverseMap();
            CreateMap<Model.PsPerformerImage, Domain.Model.PsPerformerImage>().ReverseMap();
            CreateMap<Model.PsPerformerSchedule, Domain.Model.PsPerformerSchedule>().ReverseMap();
            CreateMap<Model.PsProgram, Domain.Model.PsProgram>().ReverseMap();
            CreateMap<Model.PsProgramImage, Domain.Model.PsProgramImage>().ReverseMap();
            CreateMap<Model.Question, Domain.Model.Question>()
                .ForMember(dest => dest.Answers, opt =>
                {
                    opt.MapFrom(src => src.Answers.OrderBy(_ => _.SortOrder));
                    opt.ExplicitExpansion();
                })
                .ReverseMap();
            CreateMap<Model.Questionnaire, Domain.Model.Questionnaire>()
                .ForMember(dest => dest.Questions,
                    opt => opt.MapFrom(src => src.Questions
                        .Where(_ => !_.IsDeleted)
                        .OrderBy(_ => _.SortOrder)))
                .ReverseMap();
            CreateMap<Model.RecoveryToken, Domain.Model.RecoveryToken>().ReverseMap();
            CreateMap<Model.ReportCriterion, Domain.Model.ReportCriterion>().ReverseMap();
            CreateMap<Model.ReportRequest, Domain.Model.ReportRequest>().ReverseMap();
            CreateMap<Model.RequiredQuestionnaire, Domain.Model.RequiredQuestionnaire>().ReverseMap();
            CreateMap<Model.Role, Domain.Model.Role>().ReverseMap();
            CreateMap<Model.School, Domain.Model.School>().ReverseMap();
            CreateMap<Model.SchoolDistrict, Domain.Model.SchoolDistrict>().ReverseMap();
            CreateMap<Model.Site, Domain.Model.Site>().ReverseMap();
            CreateMap<Model.SiteSetting, Domain.Model.SiteSetting>().ReverseMap();
            CreateMap<Model.SpatialDistanceDetail, Domain.Model.SpatialDistanceDetail>().ReverseMap();
            CreateMap<Model.SpatialDistanceHeader, Domain.Model.SpatialDistanceHeader>().ReverseMap();
            CreateMap<Model.System, Domain.Model.System>()
                .ForMember(_ => _.Branches,
                    opt => opt.MapFrom(src => src.Branches.OrderBy(_ => _.Name)))
                .ReverseMap();
            CreateMap<Model.Trigger, Domain.Model.Trigger>()
                .ForMember(dest => dest.BadgeIds, opt => opt.MapFrom(src
                => src.RequiredBadges.Select(_ => _.BadgeId).ToList()))
                .ForMember(dest => dest.ChallengeIds, opt => opt.MapFrom(src
                => src.RequiredChallenges.Select(_ => _.ChallengeId).ToList()))
                .ReverseMap();
            CreateMap<Model.User, Domain.Model.User>().ReverseMap();
            CreateMap<Model.UserLog, Domain.Model.UserLog>().ReverseMap();
            CreateMap<Model.VendorCode, Domain.Model.VendorCode>().ReverseMap();
            CreateMap<Model.VendorCodeType, Domain.Model.VendorCodeType>().ReverseMap();
        }
    }
}
