namespace CareerSyncAI.Models.ViewModels;

public class AdminOverviewViewModel
{
    public string AdminName { get; set; } = string.Empty;

    public int TotalGraduates { get; set; }
    public int NewGraduatesThisWeek { get; set; }
    public int CvsAnalysedLast30Days { get; set; }
    public int AverageMatchScore { get; set; }
    public int GraduatesReadyToApplyCount { get; set; }
    public int GraduatesReadyToApplyPercent { get; set; }

    public List<SkillGapStat> TopSkillGaps { get; set; } = new();
    public List<AdminActivityRow> RecentActivity { get; set; } = new();
    public List<GraduateSummaryRow> Graduates { get; set; } = new();

    public int TechnicalSkillTermCount { get; set; }
    public int QualificationTermCount { get; set; }
}

public class SkillGapStat
{
    public string SkillName { get; set; } = string.Empty;
    public int PercentOfGraduates { get; set; }
}

public class AdminActivityRow
{
    public string GraduateName { get; set; } = string.Empty;
    public string Qualification { get; set; } = string.Empty;
    public string JobTitle { get; set; } = string.Empty;
    public string Company { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public int Score { get; set; }
}

public class GraduateSummaryRow
{
    public int GraduateId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = "GRADUATE";
    public bool IsActive { get; set; } = true;
}
