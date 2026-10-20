using CareerSyncAI.Models;

namespace CareerSyncAI.Data;

public interface IJobDescriptionRepository : IRepositoryBase<JobDescription>
{
    IReadOnlyList<JobDescription> GetByAppUserId(int appUserId);
}
