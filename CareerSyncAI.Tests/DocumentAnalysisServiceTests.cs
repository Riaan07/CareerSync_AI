using CareerSyncAI.Models;
using CareerSyncAI.Services;
using CareerSyncAI.Tests.Fakes;
using Xunit;

namespace CareerSyncAI.Tests;

public class DocumentAnalysisServiceTests
{
    private static IDocumentAnalysisService CreateService(List<SkillDictionaryTerm>? terms = null)
    {
        terms ??= new List<SkillDictionaryTerm>
        {
            new() { Id = 1, Term = "C#", IsQualification = false },
            new() { Id = 2, Term = "SQL Server", IsQualification = false },
            new() { Id = 3, Term = "Git", IsQualification = false },
            new() { Id = 4, Term = "BSc Computer Science", IsQualification = true },
        };
        return new DocumentAnalysisService(new FakeRepositoryWrapper(terms));
    }

    // -------------------------------------------------------------
    // ExtractTerms
    // -------------------------------------------------------------

    [Fact]
    public void ExtractTerms_FindsSkillsCaseInsensitively()
    {
        var service = CreateService();
        var result = service.ExtractTerms("Experienced with c# and sql server development.");

        Assert.Contains("C#", result.Skills);
        Assert.Contains("SQL Server", result.Skills);
        Assert.DoesNotContain("Git", result.Skills);
    }

    [Fact]
    public void ExtractTerms_SeparatesQualificationsFromSkills()
    {
        var service = CreateService();
        var result = service.ExtractTerms("Graduate with a BSc Computer Science degree, skilled in Git.");

        Assert.Contains("BSc Computer Science", result.Qualifications);
        Assert.Contains("Git", result.Skills);
        Assert.DoesNotContain("BSc Computer Science", result.Skills);
    }

    [Fact]
    public void ExtractTerms_DoesNotMatchPartialWords()
    {
        // "Git" should not match inside unrelated words like "digital" —
        // this is exactly the kind of false positive naive substring
        // matching would produce, and the \b word-boundary regex exists
        // specifically to prevent it.
        var service = CreateService();
        var result = service.ExtractTerms("Digital marketing experience only.");

        Assert.DoesNotContain("Git", result.Skills);
    }

    [Fact]
    public void ExtractTerms_FindsTermsEndingInSymbols()
    {
        // Regression: the old \b...\b pattern could never match a term that
        // ends in a non-letter (C#, C++) when followed by a space or comma.
        var service = CreateService(new List<SkillDictionaryTerm>
        {
            new() { Id = 1, Term = "C#" },
            new() { Id = 2, Term = "C++" },
        });
        var result = service.ExtractTerms("Projects in C#, plus some C++ coursework.");

        Assert.Contains("C#", result.Skills);
        Assert.Contains("C++", result.Skills);
    }

    [Fact]
    public void ExtractTerms_ShortAlphabeticTermsAreCaseSensitive()
    {
        var service = CreateService(new List<SkillDictionaryTerm>
        {
            new() { Id = 1, Term = "Go" },
        });

        Assert.DoesNotContain("Go", service.ExtractTerms("Ready to go the extra mile.").Skills);
        Assert.Contains("Go", service.ExtractTerms("Built microservices in Go.").Skills);
    }

    [Fact]
    public void ExtractTerms_EmptyText_ReturnsEmptyResult()
    {
        var service = CreateService();
        var result = service.ExtractTerms("");

        Assert.Empty(result.Skills);
        Assert.Empty(result.Qualifications);
    }

    // -------------------------------------------------------------
    // CalculateMatch — technical skills scoring
    // -------------------------------------------------------------

    [Fact]
    public void CalculateMatch_FullSkillOverlap_Gives100PercentTechnicalScore()
    {
        var service = CreateService();
        var cv = new CV { ExtractedSkills = new() { "C#", "SQL Server" }, ExtractedQualifications = new(), RawText = "" };
        var jd = new JobDescription { RequiredSkills = new() { "C#", "SQL Server" }, RequiredQualifications = new() };

        var result = service.CalculateMatch(cv, jd);

        Assert.Equal(100, result.TechnicalSkillsScore);
        Assert.Empty(result.GapSkills);
        Assert.Equal(2, result.MatchedSkills.Count);
    }

    [Fact]
    public void CalculateMatch_NoSkillOverlap_GivesZeroTechnicalScore()
    {
        var service = CreateService();
        var cv = new CV { ExtractedSkills = new() { "Python" }, ExtractedQualifications = new(), RawText = "" };
        var jd = new JobDescription { RequiredSkills = new() { "C#", "SQL Server" }, RequiredQualifications = new() };

        var result = service.CalculateMatch(cv, jd);

        Assert.Equal(0, result.TechnicalSkillsScore);
        Assert.Equal(2, result.GapSkills.Count);
    }

    [Fact]
    public void CalculateMatch_PartialOverlap_GivesProportionalScore()
    {
        var service = CreateService();
        var cv = new CV { ExtractedSkills = new() { "C#" }, ExtractedQualifications = new(), RawText = "" };
        var jd = new JobDescription { RequiredSkills = new() { "C#", "SQL Server", "Git", "Docker" }, RequiredQualifications = new() };

        var result = service.CalculateMatch(cv, jd);

        // 1 of 4 required skills matched = 25%
        Assert.Equal(25, result.TechnicalSkillsScore);
    }

    [Fact]
    public void CalculateMatch_NoRequiredSkills_DefaultsTo100Percent()
    {
        var service = CreateService();
        var cv = new CV { ExtractedSkills = new(), ExtractedQualifications = new(), RawText = "" };
        var jd = new JobDescription { RequiredSkills = new(), RequiredQualifications = new() };

        var result = service.CalculateMatch(cv, jd);

        Assert.Equal(100, result.TechnicalSkillsScore);
    }

    // -------------------------------------------------------------
    // CalculateMatch — experience heuristic
    // -------------------------------------------------------------

    [Fact]
    public void CalculateMatch_ExperienceScore_DetectsYearsPattern()
    {
        var service = CreateService();
        var cv = new CV { ExtractedSkills = new(), ExtractedQualifications = new(), RawText = "I have 3 years of experience in software development." };
        var jd = new JobDescription { RequiredSkills = new(), RequiredQualifications = new() };

        var result = service.CalculateMatch(cv, jd);

        // 50 + 3*15 = 95
        Assert.Equal(95, result.ExperienceScore);
    }

    [Fact]
    public void CalculateMatch_ExperienceScore_CapsAt100()
    {
        var service = CreateService();
        var cv = new CV { ExtractedSkills = new(), ExtractedQualifications = new(), RawText = "10 years experience." };
        var jd = new JobDescription { RequiredSkills = new(), RequiredQualifications = new() };

        var result = service.CalculateMatch(cv, jd);

        Assert.Equal(100, result.ExperienceScore);
    }

    [Fact]
    public void CalculateMatch_ExperienceScore_NoPattern_DefaultsTo50()
    {
        var service = CreateService();
        var cv = new CV { ExtractedSkills = new(), ExtractedQualifications = new(), RawText = "Enthusiastic and hardworking." };
        var jd = new JobDescription { RequiredSkills = new(), RequiredQualifications = new() };

        var result = service.CalculateMatch(cv, jd);

        Assert.Equal(50, result.ExperienceScore);
    }

    // -------------------------------------------------------------
    // CalculateMatch — overall weighted score
    // -------------------------------------------------------------

    [Fact]
    public void CalculateMatch_OverallScore_IsWeightedAverage()
    {
        var service = CreateService();
        var cv = new CV { ExtractedSkills = new() { "C#" }, ExtractedQualifications = new(), RawText = "" };
        var jd = new JobDescription { RequiredSkills = new() { "C#" }, RequiredQualifications = new() };

        var result = service.CalculateMatch(cv, jd);

        // technical=100 (1/1), qualifications=100 (none required), experience=50 (default)
        // overall = 100*0.50 + 50*0.25 + 100*0.25 = 87.5 -> 88 (banker's rounding to even)
        Assert.Equal(88, result.OverallScore);
    }

    // -------------------------------------------------------------
    // CalculateMatch — Admin-configured weights
    // -------------------------------------------------------------

    [Fact]
    public void CalculateMatch_UsesConfiguredWeights()
    {
        var fake = new FakeRepositoryWrapper(new List<SkillDictionaryTerm>());
        var service = new DocumentAnalysisService(fake);
        var cv = new CV { ExtractedSkills = new() { "C#" }, RawText = "" };          // experience -> 50
        var jd = new JobDescription { RequiredSkills = new() { "C#", "SQL" } };      // technical -> 50, qualifications -> 100

        // Default 50/25/25: 50*0.5 + 50*0.25 + 100*0.25 = 62.5 -> 62 (banker's rounding)
        Assert.Equal(62, service.CalculateMatch(cv, jd).OverallScore);

        // Qualifications only
        fake.Weights.Current = new MatchingWeights { TechnicalSkillsWeight = 0, ExperienceWeight = 0, QualificationsWeight = 100 };
        Assert.Equal(100, service.CalculateMatch(cv, jd).OverallScore);

        // Technical only
        fake.Weights.Current = new MatchingWeights { TechnicalSkillsWeight = 100, ExperienceWeight = 0, QualificationsWeight = 0 };
        Assert.Equal(50, service.CalculateMatch(cv, jd).OverallScore);
    }
}
