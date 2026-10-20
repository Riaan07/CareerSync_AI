namespace CareerSyncAI.Models.ViewModels;

public class MatchHistoryViewModel
{
    public PagedResult<MatchHistoryRow> Matches { get; set; } = new();
    public string? Search { get; set; }

    public List<LearningResource> MatchingCourses { get; set; } = new();
}

public class MatchHistoryRow
{
    public int MatchId { get; set; }
    public string JobTitle { get; set; } = string.Empty;
    public string? Company { get; set; }
    public string CvFileName { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public int Score { get; set; }
    public int GapCount { get; set; }
}

public class MatchDetailViewModel
{
    public MatchResult Match { get; set; } = new();
    public string JobTitle { get; set; } = string.Empty;
    public string? Company { get; set; }
    public string JobDescriptionText { get; set; } = string.Empty;
    public string CvFileName { get; set; } = string.Empty;
    public List<LearningResource> RecommendedResources { get; set; } = new();
    public MatchingWeights Weights { get; set; } = new();
}
