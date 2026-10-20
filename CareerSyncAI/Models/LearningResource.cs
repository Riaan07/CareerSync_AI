using CareerSyncAI.Models.Enums;

namespace CareerSyncAI.Models;

public class LearningResource : IEntity
{
    public int Id { get; set; }
    public string SkillName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public decimal DurationHours { get; set; }
    public string Url { get; set; } = string.Empty;
    public LearningResourceType Type { get; set; } = LearningResourceType.Course;
}
