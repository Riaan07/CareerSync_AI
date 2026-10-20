namespace CareerSyncAI.Data;

public class RepositoryWrapper : IRepositoryWrapper
{
    private readonly CareerSyncDbContext _context;

    private IAppUserRepository? _appUser;
    private ICVRepository? _cv;
    private IJobDescriptionRepository? _jobDescription;
    private IMatchResultRepository? _matchResult;
    private ILearningResourceRepository? _learningResource;
    private ISkillDictionaryRepository? _skillDictionary;
    private IAuditLogRepository? _auditLog;
    private IMessageRepository? _message;
    private IReviewRepository? _review;
    private IMatchingWeightsRepository? _matchingWeights;

    public RepositoryWrapper(CareerSyncDbContext context)
    {
        _context = context;
    }

    public IAppUserRepository AppUser => _appUser ??= new AppUserRepository(_context);
    public ICVRepository CV => _cv ??= new CVRepository(_context);
    public IJobDescriptionRepository JobDescription => _jobDescription ??= new JobDescriptionRepository(_context);
    public IMatchResultRepository MatchResult => _matchResult ??= new MatchResultRepository(_context);
    public ILearningResourceRepository LearningResource => _learningResource ??= new LearningResourceRepository(_context);
    public ISkillDictionaryRepository SkillDictionary => _skillDictionary ??= new SkillDictionaryRepository(_context);
    public IAuditLogRepository AuditLog => _auditLog ??= new AuditLogRepository(_context);
    public IMessageRepository Message => _message ??= new MessageRepository(_context);
    public IReviewRepository Review => _review ??= new ReviewRepository(_context);
    public IMatchingWeightsRepository MatchingWeights => _matchingWeights ??= new MatchingWeightsRepository(_context);
}
