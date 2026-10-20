using CareerSyncAI.Models;

namespace CareerSyncAI.Data;

public interface ISkillDictionaryRepository : IRepositoryBase<SkillDictionaryTerm>
{
    bool TermExists(string term);
}
