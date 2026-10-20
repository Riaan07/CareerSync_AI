using System.ComponentModel.DataAnnotations;
using CareerSyncAI.Services;

namespace CareerSyncAI.Models.ViewModels;

internal static class SecurityQuestionValidation
{
    public static IEnumerable<ValidationResult> Validate(
        string? question1, string question1Field,
        string? question2, string question2Field)
    {
        if (!string.IsNullOrEmpty(question1) && !SecurityQuestions.IsValid(question1))
            yield return new ValidationResult("Choose a question from the list.", new[] { question1Field });

        if (!string.IsNullOrEmpty(question2) && !SecurityQuestions.IsValid(question2))
            yield return new ValidationResult("Choose a question from the list.", new[] { question2Field });

        if (!string.IsNullOrEmpty(question1) && question1 == question2)
            yield return new ValidationResult("Choose two different questions.", new[] { question2Field });
    }
}
