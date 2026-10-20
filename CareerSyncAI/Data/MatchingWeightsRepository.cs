using CareerSyncAI.Models;

namespace CareerSyncAI.Data;

public class MatchingWeightsRepository : RepositoryBase<MatchingWeights>, IMatchingWeightsRepository
{
    public MatchingWeightsRepository(CareerSyncDbContext context) : base(context) { }

    public MatchingWeights GetCurrent() =>
        GetAll().OrderBy(w => w.Id).FirstOrDefault() ?? new MatchingWeights();
}
