using System.ComponentModel.DataAnnotations;

namespace CareerSyncAI.Models.ViewModels;

public class ManageSecurityQuestionsViewModel : IValidatableObject
{
    [Required, DataType(DataType.Password)]
    [Display(Name = "Current password")]
    public string CurrentPassword { get; set; } = string.Empty;

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
