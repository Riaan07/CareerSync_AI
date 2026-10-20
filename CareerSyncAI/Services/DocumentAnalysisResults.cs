namespace CareerSyncAI.Services;

public class ExtractionResult
{
    public List<string> Skills { get; set; } = new();
    public List<string> Qualifications { get; set; } = new();
}

public class MatchCalculationResult
{
    public int OverallScore { get; set; }
    public int TechnicalSkillsScore { get; set; }
    public int ExperienceScore { get; set; }
    public int QualificationsScore { get; set; }
    public List<string> MatchedSkills { get; set; } = new();
    public List<string> GapSkills { get; set; } = new();
}
