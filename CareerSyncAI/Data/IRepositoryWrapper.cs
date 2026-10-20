namespace CareerSyncAI.Data;

public interface IRepositoryWrapper
{
    IAppUserRepository AppUser { get; }
    ICVRepository CV { get; }
    IJobDescriptionRepository JobDescription { get; }
    IMatchResultRepository MatchResult { get; }
    ILearningResourceRepository LearningResource { get; }
    ISkillDictionaryRepository SkillDictionary { get; }
    IAuditLogRepository AuditLog { get; }
    IMessageRepository Message { get; }
    IReviewRepository Review { get; }
    IMatchingWeightsRepository MatchingWeights { get; }
}
