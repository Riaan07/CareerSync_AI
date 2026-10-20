namespace CareerSyncAI.Models;

public class Message : IEntity
{
    public int Id { get; set; }
    public int SentByAdminId { get; set; }
    public int RecipientGraduateId { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime SentAt { get; set; } = DateTime.UtcNow;
    public bool IsRead { get; set; } = false;
}
