using CareerSyncAI.Models;

namespace CareerSyncAI.Data;

public class JobDescriptionRepository : RepositoryBase<JobDescription>, IJobDescriptionRepository
{
    public JobDescriptionRepository(CareerSyncDbContext context) : base(context) { }

    public IReadOnlyList<JobDescription> GetByAppUserId(int appUserId) =>
        GetAll().Where(j => j.AppUserId == appUserId).OrderByDescending(j => j.SubmittedAt).ToList();
}
