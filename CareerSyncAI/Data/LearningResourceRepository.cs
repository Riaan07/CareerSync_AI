using CareerSyncAI.Models;

namespace CareerSyncAI.Data;

public class LearningResourceRepository : RepositoryBase<LearningResource>, ILearningResourceRepository
{
    public LearningResourceRepository(CareerSyncDbContext context) : base(context) { }

    public IReadOnlyList<LearningResource> GetBySkillName(string skillName) =>
        GetAll().Where(r => r.SkillName.Equals(skillName, StringComparison.OrdinalIgnoreCase)).ToList();
}
