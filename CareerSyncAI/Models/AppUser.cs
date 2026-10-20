using System.ComponentModel.DataAnnotations.Schema;
using CareerSyncAI.Models.Enums;

namespace CareerSyncAI.Models;

public class AppUser : IEntity
{
    public int Id { get; set; }

    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PasswordHash { get; set; }
    public string? GoogleId { get; set; }
    public UserRole Role { get; set; } = UserRole.Graduate;
    public string? Qualification { get; set; }
    public string? StudentNumber { get; set; }
    public string? PhoneNumber { get; set; }
    public string? LinkedInUrl { get; set; }
    public string? Department { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;

    public string? SecurityQuestion1 { get; set; }
    public string? SecurityAnswer1Hash { get; set; }
    public string? SecurityQuestion2 { get; set; }
    public string? SecurityAnswer2Hash { get; set; }

    public int SecurityAnswerFailedAttempts { get; set; }
    public DateTime? SecurityAnswerLockoutEndsAt { get; set; }

    public string? ResetToken { get; set; }
    public DateTime? ResetTokenExpiresAt { get; set; }

    [NotMapped]
    public bool HasSecurityQuestions =>
        !string.IsNullOrEmpty(SecurityQuestion1) && !string.IsNullOrEmpty(SecurityAnswer1Hash) &&
        !string.IsNullOrEmpty(SecurityQuestion2) && !string.IsNullOrEmpty(SecurityAnswer2Hash);
}
