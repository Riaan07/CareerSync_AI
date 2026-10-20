using System.Security.Claims;
using CareerSyncAI.Data;
using CareerSyncAI.Hubs;
using CareerSyncAI.Models;
using CareerSyncAI.Models.Enums;
using CareerSyncAI.Models.ViewModels;
using CareerSyncAI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace CareerSyncAI.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly IRepositoryWrapper _repository;
    private readonly IHubContext<MessagingHub> _hub;

    public AdminController(IRepositoryWrapper repository, IHubContext<MessagingHub> hub)
    {
        _repository = repository;
        _hub = hub;
    }

    public IActionResult Index()
    {
        var adminName = User.FindFirstValue(ClaimTypes.Name) ?? "Admin";
        var allUsers = _repository.AppUser.GetAll();
        var graduateCount = allUsers.Count(u => u.Role == UserRole.Graduate);

        var allMatches = _repository.MatchResult.GetAll();
        var dictionary = _repository.SkillDictionary.GetAll();
        var avgScore = allMatches.Count > 0 ? (int)Math.Round(allMatches.Average(m => m.OverallScore)) : 0;
        var readyCount = allMatches.Count(m => m.OverallScore >= 65);

        var gapCounts = allMatches
            .SelectMany(m => m.GapSkills)
            .GroupBy(s => s, StringComparer.OrdinalIgnoreCase)
            .Select(g => new SkillGapStat
            {
                SkillName = g.Key,
                PercentOfGraduates = allMatches.Count > 0 ? (int)Math.Round(100.0 * g.Count() / allMatches.Count) : 0,
            })
            .OrderByDescending(s => s.PercentOfGraduates)
            .Take(5)
            .ToList();

        var recentActivity = allMatches
            .OrderByDescending(m => m.GeneratedAt)
            .Take(5)
            .Select(m =>
            {
                var cv = _repository.CV.GetById(m.CVId);
                var jd = _repository.JobDescription.GetById(m.JobDescriptionId);
                var grad = cv != null ? _repository.AppUser.GetById(cv.AppUserId) : null;
                return new AdminActivityRow
                {
                    GraduateName = grad?.FullName ?? "—",
                    Qualification = grad?.Qualification ?? string.Empty,
                    JobTitle = jd?.Title ?? "—",
                    Company = jd?.Company ?? string.Empty,
                    Date = m.GeneratedAt,
                    Score = m.OverallScore,
                };
            }).ToList();

        var model = new AdminOverviewViewModel
        {
            AdminName = adminName,
            TotalGraduates = graduateCount,
            NewGraduatesThisWeek = allUsers.Count(u => u.Role == UserRole.Graduate && u.CreatedAt >= DateTime.UtcNow.AddDays(-7)),
            CvsAnalysedLast30Days = _repository.CV.GetAll().Count(c => c.UploadedAt >= DateTime.UtcNow.AddDays(-30)),
            AverageMatchScore = avgScore,
            GraduatesReadyToApplyCount = readyCount,
            GraduatesReadyToApplyPercent = allMatches.Count > 0 ? (int)Math.Round(100.0 * readyCount / allMatches.Count) : 0,
            TopSkillGaps = gapCounts,
            RecentActivity = recentActivity,
            Graduates = allUsers.Select(u => new GraduateSummaryRow
            {
                GraduateId = u.Id,
                FullName = u.FullName,
                Email = u.Email,
                Role = u.Role == UserRole.Admin ? "ADMIN" : "GRADUATE",
                IsActive = u.IsActive,
            }).ToList(),
            TechnicalSkillTermCount = dictionary.Count(t => !t.IsQualification),
            QualificationTermCount = dictionary.Count(t => t.IsQualification),
        };

        return View(model);
    }

    // Profile
    private int CurrentUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public IActionResult Profile()
    {
        var user = _repository.AppUser.GetById(CurrentUserId());
        if (user is null) return RedirectToAction("Index");

        if (TempData["StatusMessage"] is string msg) ViewData["StatusMessage"] = msg;

        return View(new AdminProfileViewModel
        {
            FullName = user.FullName,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber ?? string.Empty,
            Department = user.Department,
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Profile(AdminProfileViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var user = _repository.AppUser.GetById(CurrentUserId());
        if (user is null) return RedirectToAction("Index");

        user.FullName = model.FullName;
        user.PhoneNumber = model.PhoneNumber;
        user.Department = model.Department;

        _repository.AppUser.Update(user);
        await UserSignIn.RefreshAsync(HttpContext, user);  

        TempData["StatusMessage"] = "Profile updated.";
        return RedirectToAction("Profile");
    }

    // Graduates — list, view detail, edit, delete

    public IActionResult Graduates(string? search, int page = 1)
    {
        if (TempData["StatusMessage"] is string msg) ViewData["StatusMessage"] = msg;
        var term = search?.Trim();

        var rows = _repository.AppUser.GetAll()
            .Where(u => u.Role == UserRole.Graduate)
            .Where(u => string.IsNullOrEmpty(term)
                || u.FullName.Contains(term, StringComparison.OrdinalIgnoreCase)
                || u.Email.Contains(term, StringComparison.OrdinalIgnoreCase)
                || (u.Qualification ?? "").Contains(term, StringComparison.OrdinalIgnoreCase)
                || (u.StudentNumber ?? "").Contains(term, StringComparison.OrdinalIgnoreCase))
            .Select(u => new GraduateListRow
            {
                GraduateId = u.Id,
                FullName = u.FullName,
                Email = u.Email,
                Qualification = u.Qualification,
                IsActive = u.IsActive,
                CvCount = _repository.CV.GetByAppUserId(u.Id).Count,
                MatchCount = _repository.CV.GetByAppUserId(u.Id)
                    .SelectMany(c => _repository.MatchResult.GetByCvId(c.Id)).Count(),
                CreatedAt = u.CreatedAt,
            })
            .OrderByDescending(r => r.CreatedAt)
            .ToList();

        var paged = PagedResult<GraduateListRow>.Create(rows, page, pageSize: 10);
        ViewData["Pagination"] = paged;
        ViewData["PaginationAction"] = "Graduates";
        ViewData["PaginationRouteValues"] = new Dictionary<string, string> { ["search"] = term ?? string.Empty };
        return View(new GraduatesListViewModel { Graduates = paged, Search = term });
    }

    public IActionResult GraduateDetail(int id)
    {
        var grad = _repository.AppUser.GetById(id);
        if (grad is null || grad.Role != UserRole.Graduate)
        {
            TempData["StatusMessage"] = "That graduate account couldn't be found.";
            return RedirectToAction("Graduates");
        }

        if (TempData["StatusMessage"] is string msg) ViewData["StatusMessage"] = msg;

        var currentCv = _repository.CV.GetByAppUserId(id).OrderByDescending(c => c.UploadedAt).FirstOrDefault();

        var matchHistory = _repository.CV.GetByAppUserId(id)
            .SelectMany(c => _repository.MatchResult.GetByCvId(c.Id))
            .OrderByDescending(m => m.GeneratedAt)
            .Select(m =>
            {
                var jd = _repository.JobDescription.GetById(m.JobDescriptionId);
                return new GraduateMatchRow
                {
                    JobTitle = jd?.Title ?? "—",
                    Company = jd?.Company ?? string.Empty,
                    Date = m.GeneratedAt,
                    Score = m.OverallScore,
                };
            }).ToList();

        var model = new GraduateDetailViewModel
        {
            GraduateId = grad.Id,
            FullName = grad.FullName,
            Email = grad.Email,
            PhoneNumber = grad.PhoneNumber,
            StudentNumber = grad.StudentNumber,
            Qualification = grad.Qualification,
            LinkedInUrl = grad.LinkedInUrl,
            IsActive = grad.IsActive,
            CreatedAt = grad.CreatedAt,
            CurrentCv = currentCv,
            MatchHistory = matchHistory,
            Messages = _repository.Message.GetByGraduateId(id).ToList(),
        };

        return View(model);
    }

    [HttpGet]
    public IActionResult EditGraduate(int id)
    {
        var grad = _repository.AppUser.GetById(id);
        if (grad is null || grad.Role != UserRole.Graduate) return RedirectToAction("Graduates");

        return View(new EditGraduateViewModel
        {
            GraduateId = grad.Id,
            FullName = grad.FullName,
            PhoneNumber = grad.PhoneNumber,
            StudentNumber = grad.StudentNumber,
            Qualification = grad.Qualification,
            IsActive = grad.IsActive,
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult EditGraduate(EditGraduateViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var grad = _repository.AppUser.GetById(model.GraduateId);
        if (grad is null || grad.Role != UserRole.Graduate) return RedirectToAction("Graduates");

        grad.FullName = model.FullName;
        grad.PhoneNumber = model.PhoneNumber;
        grad.StudentNumber = model.StudentNumber;
        grad.Qualification = model.Qualification;
        grad.IsActive = model.IsActive;
        _repository.AppUser.Update(grad);

        _repository.AuditLog.Add(new AuditLogEntry
        {
            UserId = CurrentUserId(),
            ActionType = "EditGraduate",
            Description = $"Updated details for {grad.FullName} ({grad.Email}).",
        });

        TempData["StatusMessage"] = $"Updated {grad.FullName}'s account.";
        return RedirectToAction("GraduateDetail", new { id = grad.Id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult DeleteGraduate(int id)
    {
        var grad = _repository.AppUser.GetById(id);
        if (grad is null || grad.Role != UserRole.Graduate)
        {
            TempData["StatusMessage"] = "That graduate account couldn't be found.";
            return RedirectToAction("Graduates");
        }

        foreach (var cv in _repository.CV.GetByAppUserId(id))
        {
            foreach (var match in _repository.MatchResult.GetByCvId(cv.Id))
                _repository.MatchResult.Delete(match.Id);
            _repository.CV.Delete(cv.Id);
        }
        foreach (var jd in _repository.JobDescription.GetByAppUserId(id))
            _repository.JobDescription.Delete(jd.Id);
        foreach (var msg in _repository.Message.GetByGraduateId(id))
            _repository.Message.Delete(msg.Id);
        var review = _repository.Review.GetByGraduateId(id);
        if (review != null)
            _repository.Review.Delete(review.Id);

        _repository.AppUser.Delete(id);

        _repository.AuditLog.Add(new AuditLogEntry
        {
            UserId = CurrentUserId(),
            ActionType = "DeleteGraduate",
            Description = $"Deleted account for {grad.FullName} ({grad.Email}).",
        });

        TempData["StatusMessage"] = $"Deleted {grad.FullName}'s account.";
        return RedirectToAction("Graduates");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendMessage(int graduateId, string content)
    {
        var grad = _repository.AppUser.GetById(graduateId);
        if (grad is null || grad.Role != UserRole.Graduate || string.IsNullOrWhiteSpace(content))
        {
            TempData["StatusMessage"] = "Couldn't send that message.";
            return RedirectToAction("GraduateDetail", new { id = graduateId });
        }

        var adminId = CurrentUserId();
        var admin = _repository.AppUser.GetById(adminId);

        var message = _repository.Message.Add(new Message
        {
            SentByAdminId = adminId,
            RecipientGraduateId = graduateId,
            Content = content.Trim(),
        });

        await _hub.Clients.Group($"user-{graduateId}").SendAsync("ReceiveMessage", new
        {
            from = admin?.FullName ?? "CareerSync AI Admin",
            content = message.Content,
            sentAt = message.SentAt.ToString("HH:mm"),
        });

        _repository.AuditLog.Add(new AuditLogEntry
        {
            UserId = adminId,
            ActionType = "SendMessage",
            Description = $"Sent a message to {grad.FullName} ({grad.Email}).",
        });

        TempData["StatusMessage"] = "Message sent.";
        return RedirectToAction("GraduateDetail", new { id = graduateId });
    }

    public IActionResult AuditLogs(int page = 1)
    {
        var rows = _repository.AuditLog.GetAll()
            .OrderByDescending(a => a.Timestamp)
            .Select(a => new AuditLogRow
            {
                UserName = a.UserId != null ? _repository.AppUser.GetById(a.UserId.Value)?.FullName ?? "—" : "System",
                ActionType = a.ActionType,
                Description = a.Description,
                Timestamp = a.Timestamp,
            })
            .ToList();

        var paged = PagedResult<AuditLogRow>.Create(rows, page, pageSize: 20);
        ViewData["Pagination"] = paged;
        ViewData["PaginationAction"] = "AuditLogs";
        return View(new AuditLogViewModel { Entries = paged });
    }

    public IActionResult Reviews()
    {
        if (TempData["StatusMessage"] is string msg) ViewData["StatusMessage"] = msg;
        var allReviews = _repository.Review.GetAll().OrderByDescending(r => r.CreatedAt).ToList();

        var rows = allReviews.Select(r =>
        {
            var grad = _repository.AppUser.GetById(r.GraduateId);
            return new ReviewRow
            {
                ReviewId = r.Id,
                GraduateName = grad?.FullName ?? "—",
                Qualification = grad?.Qualification,
                Rating = r.Rating,
                Comment = r.Comment,
                CreatedAt = r.CreatedAt,
                UpdatedAt = r.UpdatedAt,
            };
        }).ToList();

        return View(new ReviewsListViewModel
        {
            Reviews = rows,
            AverageRating = allReviews.Count > 0 ? Math.Round(allReviews.Average(r => r.Rating), 1) : 0,
            TotalReviews = allReviews.Count,
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult DeleteReview(int id)
    {
        var review = _repository.Review.GetById(id);
        if (review is null)
        {
            TempData["StatusMessage"] = "That review couldn't be found.";
            return RedirectToAction(nameof(Reviews));
        }

        var author = _repository.AppUser.GetById(review.GraduateId);
        _repository.Review.Delete(id);

        _repository.AuditLog.Add(new AuditLogEntry
        {
            UserId = CurrentUserId(),
            ActionType = "DeleteReview",
            Description = $"Removed a {review.Rating}-star review by {author?.FullName ?? "a deleted user"}.",
        });

        TempData["StatusMessage"] = "Review removed.";
        return RedirectToAction(nameof(Reviews));
    }

    public IActionResult SkillsDictionary(string? search, string filter = "all", int page = 1)
    {
        if (TempData["StatusMessage"] is string msg) ViewData["StatusMessage"] = msg;
        return View(BuildSkillsDictionaryModel(search, filter, page));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult AddSkillTerm([Bind(Prefix = "NewTerm")] AddSkillTermViewModel model)
    {
        model.Term = (model.Term ?? string.Empty).Trim();

        if (ModelState.IsValid && _repository.SkillDictionary.TermExists(model.Term))
            ModelState.AddModelError("NewTerm.Term", $"\"{model.Term}\" is already in the dictionary.");

        if (!ModelState.IsValid)
        {
            var vm = BuildSkillsDictionaryModel(null, "all", 1);
            vm.NewTerm = model;
            return View(nameof(SkillsDictionary), vm);
        }

        _repository.SkillDictionary.Add(new SkillDictionaryTerm
        {
            Term = model.Term,
            IsQualification = model.IsQualification,
        });

        _repository.AuditLog.Add(new AuditLogEntry
        {
            UserId = CurrentUserId(),
            ActionType = "AddSkillTerm",
            Description = $"Added {(model.IsQualification ? "qualification" : "skill")} \"{model.Term}\" to the skills dictionary.",
        });

        TempData["StatusMessage"] = $"Added \"{model.Term}\". It will be detected in new analyses from now on.";
        return RedirectToAction(nameof(SkillsDictionary), new { search = model.Term });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult DeleteSkillTerm(int id, string? search, string filter = "all", int page = 1)
    {
        var term = _repository.SkillDictionary.GetById(id);
        if (term is null)
        {
            TempData["StatusMessage"] = "That term couldn't be found — it may already have been removed.";
            return RedirectToAction(nameof(SkillsDictionary), new { search, filter, page });
        }

        _repository.SkillDictionary.Delete(id);

        _repository.AuditLog.Add(new AuditLogEntry
        {
            UserId = CurrentUserId(),
            ActionType = "DeleteSkillTerm",
            Description = $"Removed \"{term.Term}\" from the skills dictionary.",
        });

        TempData["StatusMessage"] = $"Removed \"{term.Term}\".";
        return RedirectToAction(nameof(SkillsDictionary), new { search, filter, page });
    }

    private SkillsDictionaryViewModel BuildSkillsDictionaryModel(string? search, string filter, int page)
    {
        var all = _repository.SkillDictionary.GetAll();
        filter = filter is "skills" or "qualifications" ? filter : "all";

        IEnumerable<SkillDictionaryTerm> query = all;
        if (filter == "skills") query = query.Where(t => !t.IsQualification);
        if (filter == "qualifications") query = query.Where(t => t.IsQualification);
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(t => t.Term.Contains(search.Trim(), StringComparison.OrdinalIgnoreCase));

        var paged = PagedResult<SkillDictionaryTerm>.Create(
            query.OrderBy(t => t.Term, StringComparer.OrdinalIgnoreCase), page, pageSize: 25);

        ViewData["Pagination"] = paged;
        ViewData["PaginationAction"] = nameof(SkillsDictionary);
        ViewData["PaginationRouteValues"] = new Dictionary<string, string>
        {
            ["search"] = search ?? string.Empty,
            ["filter"] = filter,
        };

        return new SkillsDictionaryViewModel
        {
            Terms = paged,
            Search = search,
            Filter = filter,
            SkillCount = all.Count(t => !t.IsQualification),
            QualificationCount = all.Count(t => t.IsQualification),
        };
    }

    [HttpGet]
    public IActionResult MatchingAlgorithm()
    {
        if (TempData["StatusMessage"] is string msg) ViewData["StatusMessage"] = msg;

        var current = _repository.MatchingWeights.GetCurrent();
        var model = new MatchingAlgorithmViewModel
        {
            TechnicalSkillsWeight = current.TechnicalSkillsWeight,
            ExperienceWeight = current.ExperienceWeight,
            QualificationsWeight = current.QualificationsWeight,
        };
        FillMatchingAlgorithmDisplay(model, current);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult MatchingAlgorithm(MatchingAlgorithmViewModel model,
        [FromServices] Services.IDocumentAnalysisService analysis)
    {
        var current = _repository.MatchingWeights.GetCurrent();
        if (!ModelState.IsValid)
        {
            FillMatchingAlgorithmDisplay(model, current);
            return View(model);
        }

        current.TechnicalSkillsWeight = model.TechnicalSkillsWeight;
        current.ExperienceWeight = model.ExperienceWeight;
        current.QualificationsWeight = model.QualificationsWeight;
        current.UpdatedAt = DateTime.UtcNow;
        current.UpdatedByAdminId = CurrentUserId();

        if (current.Id == 0) _repository.MatchingWeights.Add(current);
        else _repository.MatchingWeights.Update(current);

        var recalculated = 0;
        if (model.RecalculateExisting)
        {
            foreach (var match in _repository.MatchResult.GetAll())
            {
                var cv = _repository.CV.GetById(match.CVId);
                var jd = _repository.JobDescription.GetById(match.JobDescriptionId);
                if (cv is null || jd is null) continue;

                var calc = analysis.CalculateMatch(cv, jd);
                match.OverallScore = calc.OverallScore;
                match.TechnicalSkillsScore = calc.TechnicalSkillsScore;
                match.ExperienceScore = calc.ExperienceScore;
                match.QualificationsScore = calc.QualificationsScore;
                match.MatchedSkills = calc.MatchedSkills;
                match.GapSkills = calc.GapSkills;
                _repository.MatchResult.Update(match);
                recalculated++;
            }
        }

        _repository.AuditLog.Add(new AuditLogEntry
        {
            UserId = CurrentUserId(),
            ActionType = "UpdateMatchingWeights",
            Description = $"Set matching weights to technical {model.TechnicalSkillsWeight}%, experience {model.ExperienceWeight}%, qualifications {model.QualificationsWeight}%"
                          + (model.RecalculateExisting ? $" and recalculated {recalculated} existing match result(s)." : "."),
        });

        TempData["StatusMessage"] = model.RecalculateExisting
            ? $"Weights saved and {recalculated} existing match score{(recalculated == 1 ? "" : "s")} recalculated."
            : "Weights saved. They apply to every new analysis from now on.";
        return RedirectToAction(nameof(MatchingAlgorithm));
    }

    private void FillMatchingAlgorithmDisplay(MatchingAlgorithmViewModel model, MatchingWeights current)
    {
        model.ExistingMatchCount = _repository.MatchResult.GetAll().Count;
        if (current.Id != 0)
        {
            model.LastUpdatedAt = current.UpdatedAt;
            model.LastUpdatedBy = current.UpdatedByAdminId is int id ? _repository.AppUser.GetById(id)?.FullName : null;
        }
    }

    // -------------------------------------------------------------
    // Reports — downloadable Excel workbook (Summary, Graduates,
    // Analyses, Skill gaps, Reviews) for an optional date range.
    // -------------------------------------------------------------
    [HttpGet]
    public IActionResult Reports()
    {
        if (TempData["StatusMessage"] is string msg) ViewData["StatusMessage"] = msg;
        return View();
    }

    [HttpGet]
    public IActionResult DownloadReport(DateTime? from, DateTime? to, [FromServices] IAdminReportBuilder reports)
    {
        if (from is DateTime f && to is DateTime t && f.Date > t.Date)
        {
            TempData["StatusMessage"] = "The \"From\" date must be on or before the \"To\" date.";
            return RedirectToAction(nameof(Reports));
        }

        var adminName = User.FindFirstValue(ClaimTypes.Name) ?? "Admin";
        var bytes = reports.Build(from, to, adminName);

        _repository.AuditLog.Add(new AuditLogEntry
        {
            UserId = CurrentUserId(),
            ActionType = "DownloadReport",
            Description = $"Downloaded the Excel report ({(from is null && to is null ? "all time" : $"{from:yyyy-MM-dd} to {to:yyyy-MM-dd}")}).",
        });

        var fileName = $"CareerSyncAI_Report_{DateTime.Now:yyyy-MM-dd_HHmm}.xlsx";
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }
}

