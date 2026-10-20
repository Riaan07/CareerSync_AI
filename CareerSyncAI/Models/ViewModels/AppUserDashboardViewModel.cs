namespace CareerSyncAI.Models.ViewModels;

public class AppUserDashboardViewModel
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int ProfileCompletionPercent { get; set; }

    public int LatestMatchScore { get; set; }
    public int ScoreChangeSinceLastUpload { get; set; }
    public int CvsAnalysedCount { get; set; }
    public int JobPostingsComparedCount { get; set; }
    public int OpenSkillGapsCount { get; set; }

    public MatchResult? LatestMatch { get; set; }
    public string? LatestMatchJobTitle { get; set; }
    public string? LatestMatchCompany { get; set; }
    public string? LatestMatchCvFileName { get; set; }

    public List<LearningResource> RecommendedResources { get; set; } = new();
    public List<RecentUploadRow> RecentUploads { get; set; } = new();
}

public class RecentUploadRow
{
    public int MatchId { get; set; }
    public string CvFileName { get; set; } = string.Empty;
    public string JobTitle { get; set; } = string.Empty;
    public string Company { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public int Score { get; set; }
}
