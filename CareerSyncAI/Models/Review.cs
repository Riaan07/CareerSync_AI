namespace CareerSyncAI.Models;

public class Review : IEntity
{
    public int Id { get; set; }
    public int GraduateId { get; set; }
    public int Rating { get; set; } // 1-5
    public string Comment { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
