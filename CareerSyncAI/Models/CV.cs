namespace CareerSyncAI.Models;

public class CV : IEntity
{
    public int Id { get; set; }
    public int AppUserId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    public List<string> ExtractedSkills { get; set; } = new();
    public List<string> ExtractedQualifications { get; set; } = new();
    public string RawText { get; set; } = string.Empty;
}
