namespace CareerSyncAI.Models.ViewModels;

public class GraduateDetailViewModel
{
    public int GraduateId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? StudentNumber { get; set; }
    public string? Qualification { get; set; }
    public string? LinkedInUrl { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }

    public CV? CurrentCv { get; set; }
    public List<GraduateMatchRow> MatchHistory { get; set; } = new();
    public List<Message> Messages { get; set; } = new();
}

public class GraduateMatchRow
{
    public string JobTitle { get; set; } = string.Empty;
    public string Company { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public int Score { get; set; }
}
