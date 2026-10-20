using System.ComponentModel.DataAnnotations;

namespace CareerSyncAI.Models.ViewModels;

public class RegisterViewModel : IValidatableObject
{
    [Required]
    [Display(Name = "Full name")]
    public string FullName { get; set; } = string.Empty;

    [Required, EmailAddress]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Required]
    [SmsPhoneNumber]
    [Display(Name = "Mobile number")]
    public string PhoneNumber { get; set; } = string.Empty;

    [Display(Name = "Qualification")]
    public string? Qualification { get; set; }

    [Required, DataType(DataType.Password), MinLength(8, ErrorMessage = "Use at least 8 characters.")]
    public string Password { get; set; } = string.Empty;

    [Required, DataType(DataType.Password)]
    [Display(Name = "Confirm password")]
    [Compare(nameof(Password))]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Choose a security question.")]
    [Display(Name = "Security question 1")]
    public string SecurityQuestion1 { get; set; } = string.Empty;

    [Required(ErrorMessage = "Enter an answer."), StringLength(100, MinimumLength = 2)]
    [Display(Name = "Answer 1")]
    public string SecurityAnswer1 { get; set; } = string.Empty;

    [Required(ErrorMessage = "Choose a security question.")]
    [Display(Name = "Security question 2")]
    public string SecurityQuestion2 { get; set; } = string.Empty;

    [Required(ErrorMessage = "Enter an answer."), StringLength(100, MinimumLength = 2)]
    [Display(Name = "Answer 2")]
    public string SecurityAnswer2 { get; set; } = string.Empty;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext) =>
        SecurityQuestionValidation.Validate(
            SecurityQuestion1, nameof(SecurityQuestion1),
            SecurityQuestion2, nameof(SecurityQuestion2));
}
