using CareerSyncAI.Data;
using CareerSyncAI.Models;

namespace CareerSyncAI.Tests.Fakes;

/// <summary>
/// A hand-rolled fake rather than a mocking library, to keep the test
/// project's dependencies to just xUnit. Only SkillDictionary is backed
/// by real in-memory data — DocumentAnalysisService is the only thing
/// under test here, and it only ever touches that one repository.
/// </summary>
public class FakeSkillDictionaryRepository : ISkillDictionaryRepository
{
    private readonly List<SkillDictionaryTerm> _terms;
    public FakeSkillDictionaryRepository(List<SkillDictionaryTerm> terms) => _terms = terms;

    public IReadOnlyList<SkillDictionaryTerm> GetAll() => _terms;
    public SkillDictionaryTerm? GetById(int id) => _terms.FirstOrDefault(t => t.Id == id);
    public SkillDictionaryTerm Add(SkillDictionaryTerm entity) { _terms.Add(entity); return entity; }
    public bool Update(SkillDictionaryTerm entity) => true;
    public bool Delete(int id) => _terms.RemoveAll(t => t.Id == id) > 0;
    public bool TermExists(string term) => _terms.Any(t => t.Term.Equals(term, StringComparison.OrdinalIgnoreCase));
}

/// <summary>Returns fixed weights (defaults 50/25/25 unless a test sets others).</summary>
public class FakeMatchingWeightsRepository : IMatchingWeightsRepository
{
    public MatchingWeights Current { get; set; } = new();
    public MatchingWeights GetCurrent() => Current;
    public IReadOnlyList<MatchingWeights> GetAll() => new[] { Current };
    public MatchingWeights? GetById(int id) => Current;
    public MatchingWeights Add(MatchingWeights entity) { Current = entity; return entity; }
    public bool Update(MatchingWeights entity) { Current = entity; return true; }
    public bool Delete(int id) => false;
}

public class FakeRepositoryWrapper : IRepositoryWrapper
{
    public ISkillDictionaryRepository SkillDictionary { get; }
    public FakeMatchingWeightsRepository Weights { get; } = new();
    public IMatchingWeightsRepository MatchingWeights => Weights;

    public FakeRepositoryWrapper(List<SkillDictionaryTerm> terms)
    {
        SkillDictionary = new FakeSkillDictionaryRepository(terms);
    }

    public IAppUserRepository AppUser => throw new NotImplementedException();
    public ICVRepository CV => throw new NotImplementedException();
    public IJobDescriptionRepository JobDescription => throw new NotImplementedException();
    public IMatchResultRepository MatchResult => throw new NotImplementedException();
    public ILearningResourceRepository LearningResource => throw new NotImplementedException();
    public IAuditLogRepository AuditLog => throw new NotImplementedException();
    public IMessageRepository Message => throw new NotImplementedException();
    public IReviewRepository Review => throw new NotImplementedException();
}
