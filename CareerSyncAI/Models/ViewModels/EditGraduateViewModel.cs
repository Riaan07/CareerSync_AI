using System.ComponentModel.DataAnnotations;

namespace CareerSyncAI.Models.ViewModels;

public class EditGraduateViewModel
{
    public int GraduateId { get; set; }

    [Required]
    [Display(Name = "Full name")]
    public string FullName { get; set; } = string.Empty;

    [Display(Name = "Contact number")]
    [SmsPhoneNumber]
    public string? PhoneNumber { get; set; }

    [Display(Name = "Student number")]
    public string? StudentNumber { get; set; }

    [Display(Name = "Qualification")]
    public string? Qualification { get; set; }

    [Display(Name = "Account active")]
    public bool IsActive { get; set; } = true;
}
