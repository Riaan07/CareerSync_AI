using CareerSyncAI.Models;

namespace CareerSyncAI.Data;

public class CVRepository : RepositoryBase<CV>, ICVRepository
{
    public CVRepository(CareerSyncDbContext context) : base(context) { }

    public IReadOnlyList<CV> GetByAppUserId(int appUserId) =>
        GetAll().Where(c => c.AppUserId == appUserId).OrderByDescending(c => c.UploadedAt).ToList();
}
