using CareerSyncAI.Models;

namespace CareerSyncAI.Data;

public class SkillDictionaryRepository : RepositoryBase<SkillDictionaryTerm>, ISkillDictionaryRepository
{
    public SkillDictionaryRepository(CareerSyncDbContext context) : base(context) { }

    public bool TermExists(string term) =>
        GetAll().Any(t => t.Term.Equals(term, StringComparison.OrdinalIgnoreCase));
}
