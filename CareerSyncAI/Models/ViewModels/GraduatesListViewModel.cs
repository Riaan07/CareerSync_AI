namespace CareerSyncAI.Models.ViewModels;

public class GraduatesListViewModel
{
    public PagedResult<GraduateListRow> Graduates { get; set; } = new();
    public string? Search { get; set; }
}

public class GraduateListRow
{
    public int GraduateId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Qualification { get; set; }
    public bool IsActive { get; set; }
    public int CvCount { get; set; }
    public int MatchCount { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class AuditLogViewModel
{
    public PagedResult<AuditLogRow> Entries { get; set; } = new();
}

public class AuditLogRow
{
    public string UserName { get; set; } = string.Empty;
    public string ActionType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}
