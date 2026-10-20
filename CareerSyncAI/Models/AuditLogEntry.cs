namespace CareerSyncAI.Models;

public class AuditLogEntry : IEntity
{
    public int Id { get; set; }
    public int? UserId { get; set; }
    public string ActionType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
