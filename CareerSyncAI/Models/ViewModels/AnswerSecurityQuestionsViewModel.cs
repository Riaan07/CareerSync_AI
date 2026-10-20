using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace CareerSyncAI.Models.ViewModels;

public class AnswerSecurityQuestionsViewModel
{
    [Required]
    public string Email { get; set; } = string.Empty;

    [ValidateNever] public string Question1 { get; set; } = string.Empty;
    [ValidateNever] public string Question2 { get; set; } = string.Empty;

    [Required(ErrorMessage = "Enter your answer.")]
    [Display(Name = "Your answer")]
    public string Answer1 { get; set; } = string.Empty;

    [Required(ErrorMessage = "Enter your answer.")]
    [Display(Name = "Your answer")]
    public string Answer2 { get; set; } = string.Empty;
}
