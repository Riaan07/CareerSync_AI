using CareerSyncAI.Data;
using CareerSyncAI.Models;
using CareerSyncAI.Models.Enums;

namespace CareerSyncAI.Services;

    public interface IAdminReportBuilder
    {
        /// <summary>Builds the admin report as an Excel workbook. Dates are local, inclusive; null = no limit.</summary>
        byte[] Build(DateTime? from, DateTime? to, string generatedBy);
    }

    /// <summary>
    /// The Excel report Admin downloads from Admin → Reports. Five sheets:
    /// Summary, Graduates, Analyses, Skill gaps and Reviews.
    /// The Graduates sheet is all-time; the other sheets cover the chosen period.
    /// </summary>
    public class AdminReportBuilder : IAdminReportBuilder
    {
        public const int ReadyToApplyScore = 65;   // same threshold as the Admin dashboard

        private readonly IRepositoryWrapper _repository;

        public AdminReportBuilder(IRepositoryWrapper repository)
        {
            _repository = repository;
        }

        public byte[] Build(DateTime? from, DateTime? to, string generatedBy)
        {
            // Stored times are UTC; the dates the admin picks are local calendar days.
            DateTime? fromUtc = from?.Date.ToUniversalTime();
            DateTime? toUtc = to?.Date.AddDays(1).ToUniversalTime();     // inclusive of the "to" day
            bool InPeriod(DateTime utc) => (fromUtc is null || utc >= fromUtc) && (toUtc is null || utc < toUtc);
            static DateTime Local(DateTime utc) => DateTime.SpecifyKind(utc, DateTimeKind.Utc).ToLocalTime();

            var users = _repository.AppUser.GetAll();
            var graduates = users.Where(u => u.Role == UserRole.Graduate).ToList();
            var userById = users.ToDictionary(u => u.Id);
            var cvs = _repository.CV.GetAll();
            var cvById = cvs.ToDictionary(c => c.Id);
            var jobById = _repository.JobDescription.GetAll().ToDictionary(j => j.Id);
            var allMatches = _repository.MatchResult.GetAll();
            var reviews = _repository.Review.GetAll();
            var resourcesBySkill = _repository.LearningResource.GetAll()
                .GroupBy(r => r.SkillName, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.Count(), StringComparer.OrdinalIgnoreCase);
            var weights = _repository.MatchingWeights.GetCurrent();

            int? OwnerOf(MatchResult m) => cvById.TryGetValue(m.CVId, out var cv) ? cv.AppUserId : null;

            var periodMatches = allMatches.Where(m => InPeriod(m.GeneratedAt)).OrderByDescending(m => m.GeneratedAt).ToList();
            var periodReviews = reviews.Where(r => InPeriod(r.CreatedAt)).OrderByDescending(r => r.CreatedAt).ToList();
            var periodLabel = (from, to) switch
            {
                (null, null) => "All time",
                (DateTime f, null) => $"From {f:d MMM yyyy}",
                (null, DateTime t) => $"Up to {t:d MMM yyyy}",
                (DateTime f, DateTime t) => $"{f:d MMM yyyy} – {t:d MMM yyyy}",
            };

            var xlsx = new XlsxWriter { Title = "CareerSync AI platform report", Author = generatedBy };

            // ---------------------------------------------------------------- Summary
            var avgScore = periodMatches.Count > 0 ? periodMatches.Average(m => m.OverallScore) : (double?)null;
            var ready = periodMatches.Count(m => m.OverallScore >= ReadyToApplyScore);
            var avgRating = periodReviews.Count > 0 ? periodReviews.Average(r => r.Rating) : (double?)null;

            xlsx.AddSheet("Summary")
                .SetColumnWidths(44, 18)
                .AddTitle("CareerSync AI — platform report")
                .AddSubtitle($"Period: {periodLabel}")
                .AddSubtitle($"Generated {DateTime.Now:d MMM yyyy, HH:mm} by {generatedBy}")
                .AddBlankRow()
                .AddTable(
                    new[] { "Measure", "Value" },
                    new[] { XlsxStyle.Default, XlsxStyle.Integer },
                    new List<object?[]>
                    {
                    new object?[] { "Registered graduates (all time)", graduates.Count },
                    new object?[] { "Active graduate accounts", graduates.Count(g => g.IsActive) },
                    new object?[] { "New graduates in period", graduates.Count(g => InPeriod(g.CreatedAt)) },
                    new object?[] { "CVs uploaded in period", cvs.Count(c => InPeriod(c.UploadedAt)) },
                    new object?[] { "CV / job analyses in period", periodMatches.Count },
                    new object?[] { "Graduates who ran an analysis in period", periodMatches.Select(OwnerOf).Where(id => id != null).Distinct().Count() },
                    new object?[] { "Average match score", new XlsxCell(Pct(avgScore), XlsxStyle.Percent) },
                    new object?[] { $"Analyses scoring {ReadyToApplyScore}% or more (ready to apply)", ready },
                    new object?[] { "Share of analyses ready to apply", new XlsxCell(periodMatches.Count > 0 ? (double)ready / periodMatches.Count : null, XlsxStyle.Percent) },
                    new object?[] { "Reviews submitted in period", periodReviews.Count },
                    new object?[] { "Average review rating (out of 5)", new XlsxCell(avgRating is double ar ? Math.Round(ar, 1) : null, XlsxStyle.Decimal) },
                    new object?[] { "Matching weights (skills / experience / qualifications)", new XlsxCell($"{weights.TechnicalSkillsWeight}% / {weights.ExperienceWeight}% / {weights.QualificationsWeight}%") },
                    });

            // -------------------------------------------------------------- Graduates (all time)
            var graduateRows = graduates
                .OrderBy(g => g.FullName)
                .Select(g =>
                {
                    var myCvIds = cvs.Where(c => c.AppUserId == g.Id).Select(c => c.Id).ToHashSet();
                    var mine = allMatches.Where(m => myCvIds.Contains(m.CVId)).OrderByDescending(m => m.GeneratedAt).ToList();
                    return new object?[]
                    {
                    g.FullName, g.Email, g.StudentNumber, g.Qualification, g.PhoneNumber,
                    g.IsActive ? "Active" : "Deactivated",
                    Local(g.CreatedAt),
                    myCvIds.Count,
                    mine.Count,
                    mine.Count > 0 ? mine[0].OverallScore / 100.0 : null,
                    mine.Count > 0 ? mine.Max(m => m.OverallScore) / 100.0 : null,
                    mine.Count > 0 ? Local(mine[0].GeneratedAt) : null,
                    };
                });
            xlsx.AddSheet("Graduates")
                .SetColumnWidths(24, 32, 16, 26, 16, 13, 13, 8, 10, 11, 10, 17)
                .AddTitle("Graduates")
                .AddSubtitle("All registered graduates, all time")
                .AddBlankRow()
                .AddTable(
                    new[] { "Name", "Email", "Student number", "Qualification", "Mobile", "Status", "Joined", "CVs", "Analyses", "Latest score", "Best score", "Last analysis" },
                    new[] { XlsxStyle.Bold, XlsxStyle.Default, XlsxStyle.Default, XlsxStyle.Default, XlsxStyle.Default, XlsxStyle.Default, XlsxStyle.Date,
                        XlsxStyle.Integer, XlsxStyle.Integer, XlsxStyle.Percent, XlsxStyle.Percent, XlsxStyle.DateTime },
                    graduateRows);

            // ---------------------------------------------------------------- Analyses
            var analysisRows = periodMatches.Select(m =>
            {
                var owner = OwnerOf(m) is int uid && userById.TryGetValue(uid, out var u) ? u : null;
                jobById.TryGetValue(m.JobDescriptionId, out var job);
                return new object?[]
                {
                Local(m.GeneratedAt), owner?.FullName ?? "—", owner?.Email, job?.Title ?? "—", job?.Company,
                m.OverallScore / 100.0, m.TechnicalSkillsScore / 100.0, m.ExperienceScore / 100.0, m.QualificationsScore / 100.0,
                string.Join(", ", m.MatchedSkills), string.Join(", ", m.GapSkills),
                };
            });
            xlsx.AddSheet("Analyses")
                .SetColumnWidths(17, 22, 30, 26, 22, 9, 10, 11, 14, 40, 34)
                .AddTitle("CV / job analyses")
                .AddSubtitle($"Period: {periodLabel} · newest first")
                .AddBlankRow()
                .AddTable(
                    new[] { "Date", "Graduate", "Email", "Job title", "Company", "Overall", "Technical", "Experience", "Qualifications", "Matched skills", "Skill gaps" },
                    new[] { XlsxStyle.DateTime, XlsxStyle.Bold, XlsxStyle.Default, XlsxStyle.Default, XlsxStyle.Default,
                        XlsxStyle.Percent, XlsxStyle.Percent, XlsxStyle.Percent, XlsxStyle.Percent, XlsxStyle.Wrap, XlsxStyle.Wrap },
                    analysisRows);

            // -------------------------------------------------------------- Skill gaps
            var gapRows = periodMatches
                .SelectMany(m => m.GapSkills.Distinct(StringComparer.OrdinalIgnoreCase).Select(skill => (skill, m)))
                .GroupBy(x => x.skill, StringComparer.OrdinalIgnoreCase)
                .Select(g => new
                {
                    Skill = g.Key,
                    Times = g.Count(),
                    Graduates = g.Select(x => OwnerOf(x.m)).Where(id => id != null).Distinct().Count(),
                })
                .OrderByDescending(x => x.Times).ThenBy(x => x.Skill)
                .Select(x => new object?[]
                {
                x.Skill, x.Times,
                periodMatches.Count > 0 ? (double)x.Times / periodMatches.Count : null,
                x.Graduates,
                resourcesBySkill.TryGetValue(x.Skill, out var n) ? n : 0,
                });
            xlsx.AddSheet("Skill gaps")
                .SetColumnWidths(28, 14, 16, 18, 20)
                .AddTitle("Most common skill gaps")
                .AddSubtitle($"Period: {periodLabel} · skills the job asked for that the CV didn't show")
                .AddBlankRow()
                .AddTable(
                    new[] { "Skill", "Times missing", "% of analyses", "Graduates affected", "Courses available" },
                    new[] { XlsxStyle.Bold, XlsxStyle.Integer, XlsxStyle.Percent, XlsxStyle.Integer, XlsxStyle.Integer },
                    gapRows);

            // ---------------------------------------------------------------- Reviews
            var reviewRows = periodReviews.Select(r =>
            {
                userById.TryGetValue(r.GraduateId, out var u);
                return new object?[] { Local(r.CreatedAt), u?.FullName ?? "(deleted account)", r.Rating, r.Comment, r.UpdatedAt is null ? "No" : "Yes" };
            });
            xlsx.AddSheet("Reviews")
                .SetColumnWidths(17, 24, 8, 70, 8)
                .AddTitle("Platform reviews")
                .AddSubtitle($"Period: {periodLabel}")
                .AddBlankRow()
                .AddTable(
                    new[] { "Date", "Graduate", "Rating", "Comment", "Edited" },
                    new[] { XlsxStyle.DateTime, XlsxStyle.Bold, XlsxStyle.Integer, XlsxStyle.Wrap, XlsxStyle.Default },
                    reviewRows);

            return xlsx.ToBytes();
        }

        private static double? Pct(double? score) => score is double s ? s / 100.0 : null;
    }



