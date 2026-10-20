using CareerSyncAI.Models;

namespace CareerSyncAI.Data;

public interface ICVRepository : IRepositoryBase<CV>
{
    IReadOnlyList<CV> GetByAppUserId(int appUserId);
}
