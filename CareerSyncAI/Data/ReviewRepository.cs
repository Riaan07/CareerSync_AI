using CareerSyncAI.Models;

namespace CareerSyncAI.Data;

public class ReviewRepository : RepositoryBase<Review>, IReviewRepository
{
    public ReviewRepository(CareerSyncDbContext context) : base(context) { }

    public Review? GetByGraduateId(int graduateId) =>
        GetAll().FirstOrDefault(r => r.GraduateId == graduateId);
}
