using System.ComponentModel.DataAnnotations;

namespace CareerSyncAI.Models.ViewModels;

public class MatchingAlgorithmViewModel : IValidatableObject
{
    [Range(0, 100, ErrorMessage = "Enter a value from 0 to 100.")]
    [Display(Name = "Technical skills")]
    public int TechnicalSkillsWeight { get; set; }

    [Range(0, 100, ErrorMessage = "Enter a value from 0 to 100.")]
    [Display(Name = "Experience")]
    public int ExperienceWeight { get; set; }

    [Range(0, 100, ErrorMessage = "Enter a value from 0 to 100.")]
    [Display(Name = "Qualifications")]
    public int QualificationsWeight { get; set; }

    [Display(Name = "Also recalculate every existing match score with these weights")]
    public bool RecalculateExisting { get; set; }
    public DateTime? LastUpdatedAt { get; set; }
    public string? LastUpdatedBy { get; set; }
    public int ExistingMatchCount { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var total = TechnicalSkillsWeight + ExperienceWeight + QualificationsWeight;
        if (total != 100)
            yield return new ValidationResult($"The three weights must add up to 100 (they currently add up to {total}).");
    }
}
