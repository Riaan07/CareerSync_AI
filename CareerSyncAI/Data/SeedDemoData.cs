using CareerSyncAI.Models;

namespace CareerSyncAI.Data;

public static class SeedDemoData
{
    public static void Seed(IRepositoryWrapper repository)
    {
        var demoGraduate = repository.AppUser.FindByEmail(SeedIdentity.GraduateEmail);
        if (demoGraduate is null) return;
        if (repository.CV.GetByAppUserId(demoGraduate.Id).Any()) return;

        var fileStem = demoGraduate.FullName.Replace(" ", "_");

        var cvRecent = repository.CV.Add(new CV
        {
            AppUserId = demoGraduate.Id,
            FileName = $"{fileStem}_CV_v3.pdf",
            UploadedAt = DateTime.UtcNow.AddDays(-3),
            ExtractedSkills = new() { "C#", "ASP.NET", "REST API", "SQL", "Git" },
            ExtractedQualifications = new() { "BSc Computer Science" },
            RawText = "Recent computer science graduate with C#, ASP.NET, SQL and REST API experience from coursework and one internship.",
        });

        var cvOlder = repository.CV.Add(new CV
        {
            AppUserId = demoGraduate.Id,
            FileName = $"{fileStem}_CV_v2.pdf",
            UploadedAt = DateTime.UtcNow.AddDays(-16),
            ExtractedSkills = new() { "C#", "SQL", "networking" },
            ExtractedQualifications = new() { "BSc Computer Science" },
            RawText = "Graduate with C#, SQL and networking coursework, 1 year internship experience.",
        });

        var jdSoftwareDev = repository.JobDescription.Add(new JobDescription
        {
            AppUserId = demoGraduate.Id,
            Title = "Junior Software Developer",
            Company = "Example Tech Solutions",
            RawText = "Looking for a junior developer with C#, ASP.NET, SQL Server, Git and unit testing experience.",
            RequiredSkills = new() { "C#", "ASP.NET", "REST API", "SQL Server", "Unit Testing", "Git" },
            SubmittedAt = DateTime.UtcNow.AddDays(-3),
        });

        var jdDataAnalyst = repository.JobDescription.Add(new JobDescription
        {
            AppUserId = demoGraduate.Id,
            Title = "Graduate Data Analyst",
            Company = "Northgate Insurance",
            RawText = "Entry-level analyst role working with SQL, Excel and PowerBI.",
            RequiredSkills = new() { "SQL", "Excel", "PowerBI" },
            SubmittedAt = DateTime.UtcNow.AddDays(-9),
        });

        var jdItSupport = repository.JobDescription.Add(new JobDescription
        {
            AppUserId = demoGraduate.Id,
            Title = "IT Support Intern",
            Company = "Free State Health Dept.",
            RawText = "Internship supporting networking and Windows Server administration.",
            RequiredSkills = new() { "networking", "Windows Server" },
            SubmittedAt = DateTime.UtcNow.AddDays(-16),
        });

        repository.MatchResult.Add(new MatchResult
        {
            CVId = cvRecent.Id,
            JobDescriptionId = jdSoftwareDev.Id,
            OverallScore = 78,
            TechnicalSkillsScore = 82,
            ExperienceScore = 65,
            QualificationsScore = 100,
            MatchedSkills = new() { "C#", "ASP.NET", "REST API" },
            GapSkills = new() { "SQL Server", "Unit Testing", "Git" },
            GeneratedAt = DateTime.UtcNow.AddDays(-3),
        });

        repository.MatchResult.Add(new MatchResult
        {
            CVId = cvOlder.Id,
            JobDescriptionId = jdDataAnalyst.Id,
            OverallScore = 61,
            TechnicalSkillsScore = 33,
            ExperienceScore = 60,
            QualificationsScore = 100,
            MatchedSkills = new() { "SQL" },
            GapSkills = new() { "Excel", "PowerBI" },
            GeneratedAt = DateTime.UtcNow.AddDays(-9),
        });

        repository.MatchResult.Add(new MatchResult
        {
            CVId = cvOlder.Id,
            JobDescriptionId = jdItSupport.Id,
            OverallScore = 84,
            TechnicalSkillsScore = 50,
            ExperienceScore = 60,
            QualificationsScore = 100,
            MatchedSkills = new() { "networking" },
            GapSkills = new() { "Windows Server" },
            GeneratedAt = DateTime.UtcNow.AddDays(-16),
        });

    }
}
