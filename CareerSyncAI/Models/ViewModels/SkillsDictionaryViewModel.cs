using System.ComponentModel.DataAnnotations;

namespace CareerSyncAI.Models.ViewModels;

public class SkillsDictionaryViewModel
{
    public PagedResult<SkillDictionaryTerm> Terms { get; set; } = new();

    public string? Search { get; set; }
    public string Filter { get; set; } = "all";
    public int SkillCount { get; set; }
    public int QualificationCount { get; set; }

    public AddSkillTermViewModel NewTerm { get; set; } = new();
}

public class AddSkillTermViewModel
{
    [Required(ErrorMessage = "Enter a term.")]
    [StringLength(100, ErrorMessage = "Keep terms under 100 characters.")]
    [Display(Name = "New term")]
    public string Term { get; set; } = string.Empty;

    [Display(Name = "This is a qualification (not a skill)")]
    public bool IsQualification { get; set; }
}
