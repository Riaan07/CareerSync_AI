using CareerSyncAI.Data;
using CareerSyncAI.Models;
using CareerSyncAI.Models.ViewModels;
using CareerSyncAI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text.Json;

namespace CareerSyncAI.Controllers;

[Authorize(Roles = "Graduate")]
public class AppUserController : Controller
{
    private readonly IRepositoryWrapper _repository;
    private readonly IDocumentAnalysisService _analysis;
    private readonly ICvTextExtractor _cvReader;

    public AppUserController(IRepositoryWrapper repository, IDocumentAnalysisService analysis, ICvTextExtractor cvReader)
    {
        _repository = repository;
        _analysis = analysis;
        _cvReader = cvReader;
    }

    private int CurrentUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    public IActionResult Index()
    {
        var userId = CurrentUserId();
        var name = User.FindFirstValue(ClaimTypes.Name) ?? "Graduate";
        var email = User.FindFirstValue(ClaimTypes.Email) ?? string.Empty;
        var appUser = _repository.AppUser.GetById(userId);

        if (TempData["StatusMessage"] is string msg) ViewData["StatusMessage"] = msg;

        var myCvs = _repository.CV.GetByAppUserId(userId).OrderByDescending(c => c.UploadedAt).ToList();
        var myJobDescriptions = _repository.JobDescription.GetByAppUserId(userId);
        var myCvIds = myCvs.Select(c => c.Id).ToHashSet();
        var myMatches = _repository.MatchResult.GetAll()
            .Where(m => myCvIds.Contains(m.CVId))
            .OrderByDescending(m => m.GeneratedAt)
            .ToList();

        var latestMatch = myMatches.FirstOrDefault();
        var previousMatch = myMatches.Skip(1).FirstOrDefault();

        string? latestCvFileName = null, latestJobTitle = null, latestCompany = null;
        if (latestMatch != null)
        {
            latestCvFileName = _repository.CV.GetById(latestMatch.CVId)?.FileName;
            var jd = _repository.JobDescription.GetById(latestMatch.JobDescriptionId);
            latestJobTitle = jd?.Title;
            latestCompany = jd?.Company;
        }

        var recentUploads = myMatches.Take(3).Select(m =>
        {
            var cv = _repository.CV.GetById(m.CVId);
            var jd = _repository.JobDescription.GetById(m.JobDescriptionId);
            return new RecentUploadRow
            {
                MatchId = m.Id,
                CvFileName = cv?.FileName ?? "—",
                JobTitle = jd?.Title ?? "—",
                Company = jd?.Company ?? string.Empty,
                Date = m.GeneratedAt,
                Score = m.OverallScore,
            };
        }).ToList();

        var recommendedResources = latestMatch?.GapSkills
            .SelectMany(skill => _repository.LearningResource.GetBySkillName(skill))
            .DistinctBy(r => r.Id)
            .ToList() ?? new List<LearningResource>();

        var profileCompletionPercent = 0;
        if (!string.IsNullOrWhiteSpace(appUser?.Qualification)) profileCompletionPercent += 25;
        if (myCvs.Count > 0) profileCompletionPercent += 25;
        if (myJobDescriptions.Count > 0) profileCompletionPercent += 25;
        if (myMatches.Count > 0) profileCompletionPercent += 25;

        var model = new AppUserDashboardViewModel
        {
            FullName = name,
            Email = email,
            ProfileCompletionPercent = profileCompletionPercent,
            LatestMatchScore = latestMatch?.OverallScore ?? 0,
            ScoreChangeSinceLastUpload = latestMatch != null && previousMatch != null
                ? latestMatch.OverallScore - previousMatch.OverallScore : 0,
            CvsAnalysedCount = myCvs.Count,
            JobPostingsComparedCount = myMatches.Select(m => m.JobDescriptionId).Distinct().Count(),
            OpenSkillGapsCount = latestMatch?.GapSkills.Count ?? 0,
            LatestMatch = latestMatch,
            LatestMatchJobTitle = latestJobTitle,
            LatestMatchCompany = latestCompany,
            LatestMatchCvFileName = latestCvFileName,
            RecommendedResources = recommendedResources,
            RecentUploads = recentUploads,
        };

        return View(model);
    }

    public IActionResult History(string? search, int page = 1)
    {
        var userId = CurrentUserId();
        var term = search?.Trim();
        var cvs = _repository.CV.GetByAppUserId(userId).ToDictionary(c => c.Id);

        var rows = _repository.MatchResult.GetAll()
            .Where(m => cvs.ContainsKey(m.CVId))
            .Select(m =>
            {
                var jd = _repository.JobDescription.GetById(m.JobDescriptionId);
                return (Match: m, Row: new MatchHistoryRow
                {
                    MatchId = m.Id,
                    JobTitle = jd?.Title ?? "—",
                    Company = jd?.Company,
                    CvFileName = cvs[m.CVId].FileName,
                    Date = m.GeneratedAt,
                    Score = m.OverallScore,
                    GapCount = m.GapSkills.Count,
                });
            })
            .Where(x => string.IsNullOrEmpty(term)
                || x.Row.JobTitle.Contains(term, StringComparison.OrdinalIgnoreCase)
                || (x.Row.Company ?? "").Contains(term, StringComparison.OrdinalIgnoreCase)
                || x.Row.CvFileName.Contains(term, StringComparison.OrdinalIgnoreCase)
                || x.Match.MatchedSkills.Any(s => s.Contains(term, StringComparison.OrdinalIgnoreCase))
                || x.Match.GapSkills.Any(s => s.Contains(term, StringComparison.OrdinalIgnoreCase)))
            .Select(x => x.Row)
            .OrderByDescending(r => r.Date)
            .ToList();

        var paged = PagedResult<MatchHistoryRow>.Create(rows, page, pageSize: 10);
        ViewData["Pagination"] = paged;
        ViewData["PaginationAction"] = nameof(History);
        ViewData["PaginationRouteValues"] = new Dictionary<string, string> { ["search"] = term ?? string.Empty };

        var courses = string.IsNullOrEmpty(term)
            ? new List<LearningResource>()
            : _repository.LearningResource.GetAll()
                .Where(r => r.SkillName.Contains(term, StringComparison.OrdinalIgnoreCase)
                         || r.Title.Contains(term, StringComparison.OrdinalIgnoreCase))
                .Take(6)
                .ToList();

        return View(new MatchHistoryViewModel { Matches = paged, Search = term, MatchingCourses = courses });
    }

    public IActionResult MatchDetail(int id)
    {
        var match = _repository.MatchResult.GetById(id);
        var cv = match != null ? _repository.CV.GetById(match.CVId) : null;

        if (match is null || cv is null || cv.AppUserId != CurrentUserId())
        {
            TempData["StatusMessage"] = "That analysis couldn't be found.";
            return RedirectToAction(nameof(History));
        }

        var jd = _repository.JobDescription.GetById(match.JobDescriptionId);
        var recommended = match.GapSkills
            .SelectMany(skill => _repository.LearningResource.GetBySkillName(skill))
            .DistinctBy(r => r.Id)
            .ToList();

        return View(new MatchDetailViewModel
        {
            Match = match,
            JobTitle = jd?.Title ?? "—",
            Company = jd?.Company,
            JobDescriptionText = jd?.RawText ?? string.Empty,
            CvFileName = cv.FileName,
            RecommendedResources = recommended,
            Weights = _repository.MatchingWeights.GetCurrent(),
        });
    }

    [HttpGet]
    public IActionResult Analyze() => View(new AnalyzeCvViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Analyze(AnalyzeCvViewModel model)
    {
        string cvText = model.PastedCvText ?? string.Empty;
        string cvFileName = "Pasted_CV_Text.txt";

                if (model.CvFile is { Length: > 0 })
        {
            var upload = await _cvReader.ExtractAsync(model.CvFile, HttpContext.RequestAborted);
            if (!upload.Success)
            {
                ModelState.AddModelError(nameof(model.CvFile), upload.Error!);
                return View(model);
            }
            cvText = upload.Text;
            cvFileName = Path.GetFileName(model.CvFile.FileName);
        }

        if (string.IsNullOrWhiteSpace(cvText))
        {
            ModelState.AddModelError(string.Empty, "Upload your CV (PDF or .txt), or paste your CV text below, so we can read it.");
            return View(model);
        }
        if (!ModelState.IsValid) return View(model);

        var userId = CurrentUserId();

        var cvExtraction = _analysis.ExtractTerms(cvText);
        var cv = _repository.CV.Add(new CV
        {
            AppUserId = userId,
            FileName = cvFileName,
            RawText = cvText,
            ExtractedSkills = cvExtraction.Skills,
            ExtractedQualifications = cvExtraction.Qualifications,
        });

        var jdExtraction = _analysis.ExtractTerms(model.JobDescriptionText);
        var jd = _repository.JobDescription.Add(new JobDescription
        {
            AppUserId = userId,
            Title = model.JobTitle,
            Company = model.Company,
            RawText = model.JobDescriptionText,
            RequiredSkills = jdExtraction.Skills,
            RequiredQualifications = jdExtraction.Qualifications,
        });

        var calc = _analysis.CalculateMatch(cv, jd);

        _repository.MatchResult.Add(new MatchResult
        {
            CVId = cv.Id,
            JobDescriptionId = jd.Id,
            OverallScore = calc.OverallScore,
            TechnicalSkillsScore = calc.TechnicalSkillsScore,
            ExperienceScore = calc.ExperienceScore,
            QualificationsScore = calc.QualificationsScore,
            MatchedSkills = calc.MatchedSkills,
            GapSkills = calc.GapSkills,
        });

        TempData["StatusMessage"] = $"Analysis complete — {calc.OverallScore}% match with {jd.Title}.";
        return RedirectToAction("Index");
    }

    [HttpGet]
    public IActionResult Profile()
    {
        var userId = CurrentUserId();
        var user = _repository.AppUser.GetById(userId);
        if (user is null) return RedirectToAction("Index");

        if (TempData["StatusMessage"] is string msg) ViewData["StatusMessage"] = msg;

        return View(new ProfileViewModel
        {
            FullName = user.FullName,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber ?? string.Empty,
            Qualification = user.Qualification,
            LinkedInUrl = user.LinkedInUrl,
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Profile(ProfileViewModel model)
    {
        var userId = CurrentUserId();
        if (!ModelState.IsValid) return View(model);

        var user = _repository.AppUser.GetById(userId);
        if (user is null) return RedirectToAction("Index");

        user.FullName = model.FullName;
        user.PhoneNumber = model.PhoneNumber;
        user.Qualification = model.Qualification;
        user.LinkedInUrl = model.LinkedInUrl;

        _repository.AppUser.Update(user);

        _repository.AuditLog.Add(new AuditLogEntry
        {
            UserId = userId,
            ActionType = "UpdateProfile",
            Description = $"{user.FullName} updated their profile.",
        });

        await UserSignIn.RefreshAsync(HttpContext, user);

        TempData["StatusMessage"] = "Profile updated.";
        return RedirectToAction("Profile");
    }

    public IActionResult MyCv()
    {
        var userId = CurrentUserId();
        if (TempData["StatusMessage"] is string msg) ViewData["StatusMessage"] = msg;

        var currentCv = _repository.CV.GetByAppUserId(userId).OrderByDescending(c => c.UploadedAt).FirstOrDefault();
        var matchesLinked = currentCv != null ? _repository.MatchResult.GetByCvId(currentCv.Id).Count : 0;

        return View(new MyCvViewModel
        {
            CurrentCv = currentCv,
            MatchesLinkedToCv = matchesLinked,
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult UpdateCv(string cvText)
    {
        var userId = CurrentUserId();

        if (string.IsNullOrWhiteSpace(cvText))
        {
            TempData["StatusMessage"] = "Paste your updated CV text before saving.";
            return RedirectToAction("MyCv");
        }

        var cv = _repository.CV.GetByAppUserId(userId).OrderByDescending(c => c.UploadedAt).FirstOrDefault();
        if (cv is null)
        {
            TempData["StatusMessage"] = "You don't have a CV on file yet — analyse one first.";
            return RedirectToAction("Analyze");
        }

        var extraction = _analysis.ExtractTerms(cvText);
        cv.RawText = cvText;
        cv.ExtractedSkills = extraction.Skills;
        cv.ExtractedQualifications = extraction.Qualifications;
        cv.UploadedAt = DateTime.UtcNow;
        _repository.CV.Update(cv);

        _repository.AuditLog.Add(new AuditLogEntry
        {
            UserId = userId,
            ActionType = "UpdateCv",
            Description = $"Updated CV \"{cv.FileName}\".",
        });

        var affectedMatches = _repository.MatchResult.GetByCvId(cv.Id);
        var refreshedCount = 0;
        foreach (var match in affectedMatches)
        {
            var jd = _repository.JobDescription.GetById(match.JobDescriptionId);
            if (jd is null) continue;

            var calc = _analysis.CalculateMatch(cv, jd);
            match.OverallScore = calc.OverallScore;
            match.TechnicalSkillsScore = calc.TechnicalSkillsScore;
            match.ExperienceScore = calc.ExperienceScore;
            match.QualificationsScore = calc.QualificationsScore;
            match.MatchedSkills = calc.MatchedSkills;
            match.GapSkills = calc.GapSkills;
            match.GeneratedAt = DateTime.UtcNow;
            _repository.MatchResult.Update(match);
            refreshedCount++;
        }

        TempData["StatusMessage"] = refreshedCount > 0
            ? $"CV updated — refreshed {refreshedCount} match score{(refreshedCount == 1 ? "" : "s")}."
            : "CV updated.";
        return RedirectToAction("MyCv");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult AnalyzeWithCurrentCv(string jobTitle, string company, string jobDescriptionText)
    {
        var userId = CurrentUserId();

        var cv = _repository.CV.GetByAppUserId(userId).OrderByDescending(c => c.UploadedAt).FirstOrDefault();
        if (cv is null)
        {
            TempData["StatusMessage"] = "You don't have a CV on file yet — analyse one first.";
            return RedirectToAction("Analyze");
        }

        if (string.IsNullOrWhiteSpace(jobTitle) || string.IsNullOrWhiteSpace(jobDescriptionText) || jobDescriptionText.Trim().Length < 30)
        {
            TempData["StatusMessage"] = "Add a job title and a fuller job description before analysing.";
            return RedirectToAction("MyCv");
        }

        var jdExtraction = _analysis.ExtractTerms(jobDescriptionText);
        var jd = _repository.JobDescription.Add(new JobDescription
        {
            AppUserId = userId,
            Title = jobTitle,
            Company = company,
            RawText = jobDescriptionText,
            RequiredSkills = jdExtraction.Skills,
            RequiredQualifications = jdExtraction.Qualifications,
        });

        var calc = _analysis.CalculateMatch(cv, jd);
        _repository.MatchResult.Add(new MatchResult
        {
            CVId = cv.Id,
            JobDescriptionId = jd.Id,
            OverallScore = calc.OverallScore,
            TechnicalSkillsScore = calc.TechnicalSkillsScore,
            ExperienceScore = calc.ExperienceScore,
            QualificationsScore = calc.QualificationsScore,
            MatchedSkills = calc.MatchedSkills,
            GapSkills = calc.GapSkills,
        });

        TempData["StatusMessage"] = $"Analysis complete — {calc.OverallScore}% match with {jd.Title}.";
        return RedirectToAction("Index");
    }


    public IActionResult Messages()
    {
        var messages = _repository.Message.GetByGraduateId(CurrentUserId()).ToList();

        foreach ( var m in messages.Where(m => !m.IsRead))
        {
            m.IsRead = true;
            _repository.Message.Update(m);
        }

        return View(new MessagesViewModel { Messages = messages });
    }

    public IActionResult Reviews()
    {
        var userId = CurrentUserId();
        var allReviews = _repository.Review.GetAll().OrderByDescending(r => r.CreatedAt).ToList();

        var rows = allReviews.Select(r =>
        {
            var grad = _repository.AppUser.GetById(r.GraduateId);
            return new ReviewRow
            {
                GraduateName = grad?.FullName ?? "—",
                Qualification = grad?.Qualification,
                Rating = r.Rating,
                Comment = r.Comment,
                CreatedAt = r.CreatedAt,
                UpdatedAt = r.UpdatedAt,
                IsCurrentUser = r.GraduateId == userId,
            };
        }).ToList();

        var myReview = _repository.Review.GetByGraduateId(userId);

        if (TempData["StatusMessage"] is string msg) ViewData["StatusMessage"] = msg;

        return View(new ReviewsListViewModel
        {
            Reviews = rows,
            AverageRating = allReviews.Count > 0 ? Math.Round(allReviews.Average(r => r.Rating), 1) : 0,
            TotalReviews = allReviews.Count,
            MyReview = myReview != null ? new ReviewViewModel { Rating = myReview.Rating, Comment = myReview.Comment } : null,
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult SubmitReview(ReviewViewModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["StatusMessage"] = "Choose a rating from 1 to 5 and add a comment before submitting.";
            return RedirectToAction("Reviews");
        }

        var userId = CurrentUserId();
        var existing = _repository.Review.GetByGraduateId(userId);

        if (existing != null)
        {
            existing.Rating = model.Rating;
            existing.Comment = model.Comment;
            existing.UpdatedAt = DateTime.UtcNow;
            _repository.Review.Update(existing);
            TempData["StatusMessage"] = "Your review has been updated.";
        }
        else
        {
            _repository.Review.Add(new Review
            {
                GraduateId = userId,
                Rating = model.Rating,
                Comment = model.Comment,
            });
            TempData["StatusMessage"] = "Thanks for your review!";
        }

        _repository.AuditLog.Add(new AuditLogEntry
        {
            UserId = userId,
            ActionType = existing != null ? "UpdateReview" : "SubmitReview",
            Description = "Submitted or updated a platform review.",
        });

        return RedirectToAction("Reviews");
    }
}
