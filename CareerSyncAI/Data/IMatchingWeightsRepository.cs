using CareerSyncAI.Models;

namespace CareerSyncAI.Data;

public interface IMatchingWeightsRepository : IRepositoryBase<MatchingWeights>
{
    MatchingWeights GetCurrent();
}
