using CareerSyncAI.Models;

namespace CareerSyncAI.Data;

public interface ILearningResourceRepository : IRepositoryBase<LearningResource>
{
    IReadOnlyList<LearningResource> GetBySkillName(string skillName);
}
