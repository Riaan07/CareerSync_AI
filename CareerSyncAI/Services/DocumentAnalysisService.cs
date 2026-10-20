using System.Text.RegularExpressions;
using CareerSyncAI.Data;
using CareerSyncAI.Models;

namespace CareerSyncAI.Services;

public class DocumentAnalysisService : IDocumentAnalysisService
{
    private readonly IRepositoryWrapper _repository;

    public DocumentAnalysisService(IRepositoryWrapper repository)
    {
        _repository = repository;
    }

    public ExtractionResult ExtractTerms(string text)
    {
        var result = new ExtractionResult();
        if (string.IsNullOrWhiteSpace(text)) return result;

        foreach (var term in _repository.SkillDictionary.GetAll())
        {
            if (ContainsTerm(text, term.Term))
            {
                if (term.IsQualification) result.Qualifications.Add(term.Term);
                else result.Skills.Add(term.Term);
            }
        }
        return result;
    }

    public MatchCalculationResult CalculateMatch(CV cv, JobDescription jobDescription)
    {
        var matched = cv.ExtractedSkills
            .Where(s => jobDescription.RequiredSkills.Any(r => r.Equals(s, StringComparison.OrdinalIgnoreCase)))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var gaps = jobDescription.RequiredSkills
            .Where(r => !cv.ExtractedSkills.Any(s => s.Equals(r, StringComparison.OrdinalIgnoreCase)))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        int technicalScore = jobDescription.RequiredSkills.Count == 0
            ? 100
            : (int)Math.Round(100.0 * matched.Count / jobDescription.RequiredSkills.Count);

        int qualificationsScore = ScoreQualifications(cv, jobDescription);
        int experienceScore = ScoreExperience(cv.RawText);

        // Category weights are configured by Admin (Matching algorithm page)
        // and always total 100; defaults are 50 / 25 / 25.
        var weights = _repository.MatchingWeights.GetCurrent();
        int overall = (int)Math.Round(
            (technicalScore * weights.TechnicalSkillsWeight +
             experienceScore * weights.ExperienceWeight +
             qualificationsScore * weights.QualificationsWeight) / 100.0);

        return new MatchCalculationResult
        {
            OverallScore = overall,
            TechnicalSkillsScore = technicalScore,
            ExperienceScore = experienceScore,
            QualificationsScore = qualificationsScore,
            MatchedSkills = matched,
            GapSkills = gaps,
        };
    }

    private static int ScoreQualifications(CV cv, JobDescription jd)
    {
        if (jd.RequiredQualifications.Count == 0) return 100;
        var matched = jd.RequiredQualifications
            .Count(r => cv.ExtractedQualifications.Any(q => q.Equals(r, StringComparison.OrdinalIgnoreCase)));
        return (int)Math.Round(100.0 * matched / jd.RequiredQualifications.Count);
    }

    /// <summary>
    /// Simple heuristic pending real experience parsing: looks for a
    /// "N years" style pattern first, falls back to internship/graduate
    /// keywords, otherwise a neutral default.
    /// </summary>
    private static int ScoreExperience(string cvText)
    {
        if (string.IsNullOrWhiteSpace(cvText)) return 50;

        var yearsMatch = Regex.Match(cvText, @"(\d+)\+?\s*(?:years|yrs)", RegexOptions.IgnoreCase);
        if (yearsMatch.Success && int.TryParse(yearsMatch.Groups[1].Value, out var years))
        {
            return Math.Min(100, 50 + years * 15);
        }

        if (Regex.IsMatch(cvText, @"intern(ship)?", RegexOptions.IgnoreCase)) return 60;
        if (Regex.IsMatch(cvText, @"graduate|junior|entry.level", RegexOptions.IgnoreCase)) return 55;

        return 50;
    }

    /// <summary>
    /// Whole-term match. Uses look-arounds rather than \b because \b only
    /// works next to letters/digits: "\bC#\b" can never match "C# developer"
    /// (there's no word boundary between "#" and a space), so C#, C++ and
    /// similar terms were silently never detected.
    ///
    /// Very short purely-alphabetic terms ("R", "Go") are matched
    /// case-sensitively so ordinary words like "go" in "ready to go the
    /// extra mile" aren't counted as the Go language.
    /// </summary>
    private static bool ContainsTerm(string text, string term)
    {
        if (string.IsNullOrWhiteSpace(term)) return false;

        var options = term.Length <= 2 && term.All(char.IsLetter)
            ? RegexOptions.None
            : RegexOptions.IgnoreCase;

        return Regex.IsMatch(text, $@"(?<![\w]){Regex.Escape(term)}(?![\w])", options);
    }
}
