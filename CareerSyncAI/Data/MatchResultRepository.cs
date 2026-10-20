using CareerSyncAI.Models;

namespace CareerSyncAI.Data;

public class MatchResultRepository : RepositoryBase<MatchResult>, IMatchResultRepository
{
    public MatchResultRepository(CareerSyncDbContext context) : base(context) { }

    public IReadOnlyList<MatchResult> GetByCvId(int cvId) =>
        GetAll().Where(m => m.CVId == cvId).ToList();

    public IReadOnlyList<MatchResult> GetByJobDescriptionId(int jobDescriptionId) =>
        GetAll().Where(m => m.JobDescriptionId == jobDescriptionId).ToList();
}
