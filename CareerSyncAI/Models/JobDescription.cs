namespace CareerSyncAI.Models;

public class JobDescription : IEntity
{
    public int Id { get; set; }
    public int AppUserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Company { get; set; }
    public string RawText { get; set; } = string.Empty;
    public List<string> RequiredSkills { get; set; } = new();
    public List<string> RequiredQualifications { get; set; } = new();
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
}
