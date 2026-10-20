using System.ComponentModel.DataAnnotations;

namespace CareerSyncAI.Models.ViewModels;

public class ProfileViewModel
{
    [Required]
    [Display(Name = "Full name")]
    public string FullName { get; set; } = string.Empty;

    [Display(Name = "University email")]
    public string Email { get; set; } = string.Empty;

    [Required]
    [SmsPhoneNumber]
    [Display(Name = "Contact number")]
    public string PhoneNumber { get; set; } = string.Empty;

    [Display(Name = "Student number")]
    public string? StudentNumber { get; set; }

    [Display(Name = "Qualification")]
    public string? Qualification { get; set; }

    [Display(Name = "LinkedIn profile (optional)")]
    [Url(ErrorMessage = "Enter a valid URL.")]
    public string? LinkedInUrl { get; set; }
}
