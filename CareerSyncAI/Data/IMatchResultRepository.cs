using CareerSyncAI.Models;

namespace CareerSyncAI.Data;

public interface IMatchResultRepository : IRepositoryBase<MatchResult>
{
    IReadOnlyList<MatchResult> GetByCvId(int cvId);
    IReadOnlyList<MatchResult> GetByJobDescriptionId(int jobDescriptionId);
}
