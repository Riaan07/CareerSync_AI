namespace CareerSyncAI.Models;

public class MatchingWeights : IEntity
{
    public const int DefaultTechnical = 50;
    public const int DefaultExperience = 25;
    public const int DefaultQualifications = 25;

    public int Id { get; set; }
    public int TechnicalSkillsWeight { get; set; } = DefaultTechnical;
    public int ExperienceWeight { get; set; } = DefaultExperience;
    public int QualificationsWeight { get; set; } = DefaultQualifications;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public int? UpdatedByAdminId { get; set; }
}
