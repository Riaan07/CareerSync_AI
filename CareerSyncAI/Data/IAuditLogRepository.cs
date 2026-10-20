using CareerSyncAI.Models;

namespace CareerSyncAI.Data;

public interface IAuditLogRepository : IRepositoryBase<AuditLogEntry>
{
    IReadOnlyList<AuditLogEntry> GetByUserId(int userId);
    IReadOnlyList<AuditLogEntry> GetRecent(int count);
}
