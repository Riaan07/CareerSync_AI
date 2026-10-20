using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace CareerSyncAI.Models.ViewModels;

public class AnalyzeCvViewModel
{
    [Display(Name = "CV file (PDF or .txt)")]
    public IFormFile? CvFile { get; set; }

    [Display(Name = "Or paste your CV text")]
    [DataType(DataType.MultilineText)]
    public string? PastedCvText { get; set; }

    [Required]
    [Display(Name = "Job title")]
    public string JobTitle { get; set; } = string.Empty;

    [Display(Name = "Company (optional)")]
    public string? Company { get; set; }

    [Required, MinLength(30, ErrorMessage = "Paste a more complete job description.")]
    [DataType(DataType.MultilineText)]
    [Display(Name = "Job description")]
    public string JobDescriptionText { get; set; } = string.Empty;
}
