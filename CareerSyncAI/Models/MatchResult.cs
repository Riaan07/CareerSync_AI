using CareerSyncAI.Models.Enums;

namespace CareerSyncAI.Models;

public class MatchResult : IEntity
{
    public int Id { get; set; }
    public int CVId { get; set; }
    public int JobDescriptionId { get; set; }
    public int OverallScore { get; set; }
    public int TechnicalSkillsScore { get; set; }
    public int ExperienceScore { get; set; }
    public int QualificationsScore { get; set; }
    public List<string> MatchedSkills { get; set; } = new();
    public List<string> GapSkills { get; set; } = new();
    public AnalysisStatus Status { get; set; } = AnalysisStatus.Completed;
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}
