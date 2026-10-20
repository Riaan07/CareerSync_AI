using System.ComponentModel.DataAnnotations;

namespace CareerSyncAI.Models.ViewModels;

public class AdminProfileViewModel
{
    [Required]
    [Display(Name = "Full name")]
    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty; // read-only in the view

    [Required]
    [SmsPhoneNumber]
    [Display(Name = "Contact number")]
    public string PhoneNumber { get; set; } = string.Empty;

    [Display(Name = "Department")]
    public string? Department { get; set; }
}
