using CareerSyncAI.Models;

namespace CareerSyncAI.Data;

public interface IReviewRepository : IRepositoryBase<Review>
{
    Review? GetByGraduateId(int graduateId);
}
