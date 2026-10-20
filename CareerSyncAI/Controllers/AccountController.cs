using System.Security.Claims;
using System.Security.Cryptography;
using CareerSyncAI.Data;
using CareerSyncAI.Models;
using CareerSyncAI.Models.Enums;
using CareerSyncAI.Models.ViewModels;
using CareerSyncAI.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace CareerSyncAI.Controllers;

public class AccountController : Controller
{
    private readonly IRepositoryWrapper _repository;
    private readonly ISmsVerificationService _sms;
    private readonly IMemoryCache _cache;
    private readonly ILogger<AccountController> _logger;

    private static readonly TimeSpan SmsResendCooldown = TimeSpan.FromSeconds(60);
    private const int MaxSmsCodeAttempts = 5;
    private static readonly TimeSpan SmsAttemptWindow = TimeSpan.FromMinutes(10);

    private const int MaxSecurityAnswerAttempts = 5;
    private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    private const string InvalidResetMessage = "That reset session is invalid or has expired — please start again.";

    public AccountController(IRepositoryWrapper repository, ISmsVerificationService sms,
        IMemoryCache cache, ILogger<AccountController> logger)
    {
        _repository = repository;
        _sms = sms;
        _cache = cache;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult Login()
    {
        if (TempData["StatusMessage"] is string msg) ViewData["StatusMessage"] = msg;
        return View(new LoginViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var user = _repository.AppUser.FindByEmail(model.Email);
        if (user is null || user.PasswordHash is null || !PasswordHasher.Verify(model.Password, user.PasswordHash))
        {
            ModelState.AddModelError(string.Empty, "Incorrect email or password.");
            return View(model);
        }

        if (!user.IsActive)
        {
            ModelState.AddModelError(string.Empty, "This account has been deactivated. Contact the Careers & Employability Office.");
            return View(model);
        }

        if (PasswordHasher.NeedsRehash(user.PasswordHash))
        {
            user.PasswordHash = PasswordHasher.Hash(model.Password);
            _repository.AppUser.Update(user);
        }

        await SignInUserAsync(user, model.RememberMe);

        _repository.AuditLog.Add(new AuditLogEntry
        {
            UserId = user.Id,
            ActionType = "Login",
            Description = $"{user.FullName} signed in.",
        });

        if (!user.HasSecurityQuestions)
        {
            TempData["StatusMessage"] = "Please set up your security questions — you'll need them if you ever forget your password.";
            return RedirectToAction(nameof(SecurityQuestions));
        }

        return user.Role == UserRole.Admin
            ? RedirectToAction("Index", "Admin")
            : RedirectToAction("Index", "AppUser");
    }

    [HttpGet]
    public IActionResult Register() => View(new RegisterViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        if (_repository.AppUser.EmailExists(model.Email))
        {
            ModelState.AddModelError(nameof(model.Email), "An account with this email already exists.");
            return View(model);
        }

        _repository.AppUser.Add(new AppUser
        {
            FullName = model.FullName,
            Email = model.Email,
            PhoneNumber = model.PhoneNumber,
            Qualification = model.Qualification,
            PasswordHash = PasswordHasher.Hash(model.Password),
            Role = UserRole.Graduate,
            SecurityQuestion1 = model.SecurityQuestion1,
            SecurityAnswer1Hash = SecurityAnswerHasher.Hash(model.SecurityAnswer1),
            SecurityQuestion2 = model.SecurityQuestion2,
            SecurityAnswer2Hash = SecurityAnswerHasher.Hash(model.SecurityAnswer2),
        });

        var newUser = _repository.AppUser.FindByEmail(model.Email);
        _repository.AuditLog.Add(new AuditLogEntry
        {
            UserId = newUser?.Id,
            ActionType = "Register",
            Description = $"{model.FullName} created an account.",
        });

        TempData["StatusMessage"] = "Account created — please sign in.";
        return RedirectToAction("Login");
    }

    [HttpGet]
    public IActionResult ForgotPassword()
    {
        if (TempData["StatusMessage"] is string msg) ViewData["StatusMessage"] = msg;
        return View(new ForgotPasswordViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        model.Email = model.Email.Trim();

        if (model.Method == PasswordRecoveryMethod.Sms)
        {
            var outcome = await SendSmsCodeAsync(model.Email);
            if (SmsSendError(outcome) is string error)
            {
                ModelState.AddModelError(string.Empty, error);
                return View(model);
            }

            return RedirectToAction(nameof(EnterSmsCode), new { email = model.Email });
        }

      
        var user = _repository.AppUser.FindByEmail(model.Email);
        if (user is null || !user.IsActive || !user.HasSecurityQuestions)
        {
            ModelState.AddModelError(string.Empty,
                "We couldn't find an account with security questions for that email. " +
                "Choose \"Text me a code\" instead.");
            return View(model);
        }

        return RedirectToAction(nameof(AnswerSecurityQuestions), new { email = user.Email });
    }

    [HttpGet]
    public IActionResult EnterSmsCode(string email)
    {
        if (string.IsNullOrWhiteSpace(email)) return RedirectToAction(nameof(ForgotPassword));
        if (TempData["StatusMessage"] is string msg) ViewData["StatusMessage"] = msg;
        return View(new VerifySmsCodeViewModel { Email = email.Trim() });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EnterSmsCode(VerifySmsCodeViewModel model)
    {
        if (string.IsNullOrWhiteSpace(model.Email)) return RedirectToAction(nameof(ForgotPassword));
        if (!ModelState.IsValid) return View(model);

        var attemptsKey = "sms-attempts:" + model.Email.Trim().ToLowerInvariant();
        var attempts = _cache.Get<int>(attemptsKey);
        if (attempts >= MaxSmsCodeAttempts)
        {
            ModelState.AddModelError(string.Empty, "Too many incorrect codes. Send yourself a new code and try again.");
            model.Code = string.Empty;
            return View(model);
        }

        var user = _repository.AppUser.FindByEmail(model.Email.Trim());
        var phone = user is { IsActive: true } ? PhoneNumbers.ToE164(user.PhoneNumber) : null;

        var result = phone is null ? SmsCheckStatus.Incorrect : await _sms.CheckCodeAsync(phone, model.Code.Trim());

        if (result == SmsCheckStatus.Approved)
        {
            _cache.Remove(attemptsKey);
            var (rawToken, tokenHash) = PasswordResetTokens.Create();
            user!.ResetToken = tokenHash;
            user.ResetTokenExpiresAt = DateTime.UtcNow.Add(PasswordResetTokens.VerifiedResetLifetime);
            _repository.AppUser.Update(user);

            _repository.AuditLog.Add(new AuditLogEntry
            {
                UserId = user.Id,
                ActionType = "ResetCodeVerified",
                Description = $"{user.FullName} confirmed the SMS code sent to {PhoneNumbers.Mask(phone!)} to reset their password.",
            });
            return RedirectToAction(nameof(ResetPassword), new { email = user.Email, token = rawToken });
        }

        model.Code = string.Empty;
        switch (result)
        {
            case SmsCheckStatus.Incorrect:
                attempts++;
                _cache.Set(attemptsKey, attempts, SmsAttemptWindow);
                var left = MaxSmsCodeAttempts - attempts;
                ModelState.AddModelError(string.Empty, left > 0
                    ? $"That code isn't right. {left} attempt{(left == 1 ? "" : "s")} left."
                    : "Too many incorrect codes. Send yourself a new code and try again.");
                if (user != null)
                    _repository.AuditLog.Add(new AuditLogEntry
                    {
                        UserId = user.Id,
                        ActionType = "ResetCodeFailed",
                        Description = $"Incorrect SMS reset code entered for {user.FullName}.",
                    });
                break;
            case SmsCheckStatus.ExpiredOrMissing:
                ModelState.AddModelError(string.Empty, "That code has expired or has already been used. Send yourself a new code.");
                break;
            default:
                ModelState.AddModelError(string.Empty, "We couldn't check the code right now. Please try again in a moment.");
                break;
        }
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResendSmsCode(string email)
    {
        if (string.IsNullOrWhiteSpace(email)) return RedirectToAction(nameof(ForgotPassword));

        var outcome = await SendSmsCodeAsync(email.Trim());
        TempData["StatusMessage"] = outcome == SmsSendOutcome.CoolingDown
            ? "Please wait a minute before asking for another code."
            : SmsSendError(outcome) ?? "If that email belongs to an account with a mobile number, a new code is on its way.";
        if (outcome != SmsSendOutcome.CoolingDown && SmsSendError(outcome) is null)
            _cache.Remove("sms-attempts:" + email.Trim().ToLowerInvariant());   

        return RedirectToAction(nameof(EnterSmsCode), new { email = email.Trim() });
    }

    [HttpGet]
    public IActionResult AnswerSecurityQuestions(string email)
    {
        var user = string.IsNullOrWhiteSpace(email) ? null : _repository.AppUser.FindByEmail(email);
        if (user is null || !user.IsActive || !user.HasSecurityQuestions)
            return RedirectToAction(nameof(ForgotPassword));

        if (IsLockedOut(user, out var lockedMessage))
            ModelState.AddModelError(string.Empty, lockedMessage);

        return View(new AnswerSecurityQuestionsViewModel
        {
            Email = user.Email,
            Question1 = user.SecurityQuestion1!,
            Question2 = user.SecurityQuestion2!,
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult AnswerSecurityQuestions(AnswerSecurityQuestionsViewModel model)
    {
        var user = _repository.AppUser.FindByEmail(model.Email);
        if (user is null || !user.IsActive || !user.HasSecurityQuestions)
            return RedirectToAction(nameof(ForgotPassword));

        model.Question1 = user.SecurityQuestion1!;
        model.Question2 = user.SecurityQuestion2!;

        if (!ModelState.IsValid) return View(model);

        if (IsLockedOut(user, out var lockedMessage))
        {
            ModelState.AddModelError(string.Empty, lockedMessage);
            return View(model);
        }

        var answer1Ok = SecurityAnswerHasher.Verify(model.Answer1, user.SecurityAnswer1Hash);
        var answer2Ok = SecurityAnswerHasher.Verify(model.Answer2, user.SecurityAnswer2Hash);

        if (!(answer1Ok && answer2Ok))
        {
            user.SecurityAnswerFailedAttempts++;
            var remaining = MaxSecurityAnswerAttempts - user.SecurityAnswerFailedAttempts;

            if (remaining <= 0)
            {
                user.SecurityAnswerLockoutEndsAt = DateTime.UtcNow.Add(LockoutDuration);
                user.SecurityAnswerFailedAttempts = 0;
                ModelState.AddModelError(string.Empty,
                    $"Too many incorrect attempts. Password recovery for this account is locked for {LockoutDuration.TotalMinutes:0} minutes.");
            }
            else
            {
                ModelState.AddModelError(string.Empty,
                    $"Those answers don't match our records. {remaining} attempt{(remaining == 1 ? "" : "s")} left.");
            }

            _repository.AppUser.Update(user);
            _repository.AuditLog.Add(new AuditLogEntry
            {
                UserId = user.Id,
                ActionType = "SecurityQuestionsFailed",
                Description = $"Incorrect security answers entered for {user.FullName}.",
            });

            model.Answer1 = model.Answer2 = string.Empty;
            return View(model);
        }

        user.SecurityAnswerFailedAttempts = 0;
        user.SecurityAnswerLockoutEndsAt = null;
        var (rawToken, tokenHash) = PasswordResetTokens.Create();
        user.ResetToken = tokenHash;
        user.ResetTokenExpiresAt = DateTime.UtcNow.Add(PasswordResetTokens.VerifiedResetLifetime);
        _repository.AppUser.Update(user);

        _repository.AuditLog.Add(new AuditLogEntry
        {
            UserId = user.Id,
            ActionType = "SecurityQuestionsVerified",
            Description = $"{user.FullName} answered their security questions to reset their password.",
        });

        return RedirectToAction(nameof(ResetPassword), new { email = user.Email, token = rawToken });
    }

    [HttpGet]
    public IActionResult ResetPassword(string email, string token)
    {
        var user = string.IsNullOrWhiteSpace(email) ? null : _repository.AppUser.FindByEmail(email);
        if (!IsResetTokenValid(user, token))
        {
            TempData["StatusMessage"] = InvalidResetMessage;
            return RedirectToAction(nameof(ForgotPassword));
        }

        return View(new ResetPasswordViewModel { Email = email, Token = token });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ResetPassword(ResetPasswordViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var user = _repository.AppUser.FindByEmail(model.Email);
        if (!IsResetTokenValid(user, model.Token))
        {
            TempData["StatusMessage"] = InvalidResetMessage;
            return RedirectToAction(nameof(ForgotPassword));
        }

        user!.PasswordHash = PasswordHasher.Hash(model.NewPassword);
        user.ResetToken = null;              
        user.ResetTokenExpiresAt = null;
        user.SecurityAnswerFailedAttempts = 0;
        user.SecurityAnswerLockoutEndsAt = null;
        _repository.AppUser.Update(user);

        _repository.AuditLog.Add(new AuditLogEntry
        {
            UserId = user.Id,
            ActionType = "ResetPassword",
            Description = $"{user.FullName} reset their password (via SMS code or security questions).",
        });

        TempData["StatusMessage"] = "Password reset — sign in with your new password.";
        return RedirectToAction("Login");
    }

    public IActionResult GoogleLogin()
    {
        TempData["StatusMessage"] = "Google sign-in isn't wired up yet — sign in with email for now.";
        return RedirectToAction("Login");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Index", "Home");
    }


    [Authorize]
    [HttpGet]
    public IActionResult ChangePassword()
    {
        if (TempData["StatusMessage"] is string msg) ViewData["StatusMessage"] = msg;
        return View(new ChangePasswordViewModel());
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ChangePassword(ChangePasswordViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var user = _repository.AppUser.GetById(userId);
        if (user is null) return RedirectToAction("Login");

        if (user.PasswordHash is null || !PasswordHasher.Verify(model.CurrentPassword, user.PasswordHash))
        {
            ModelState.AddModelError(nameof(model.CurrentPassword), "Current password is incorrect.");
            return View(model);
        }

        user.PasswordHash = PasswordHasher.Hash(model.NewPassword);
        _repository.AppUser.Update(user);

        _repository.AuditLog.Add(new AuditLogEntry
        {
            UserId = user.Id,
            ActionType = "ChangePassword",
            Description = $"{user.FullName} changed their password.",
        });

        TempData["StatusMessage"] = "Password updated.";
        return RedirectToAction("ChangePassword");
    }

    [Authorize]
    [HttpGet]
    public IActionResult SecurityQuestions()
    {
        if (TempData["StatusMessage"] is string msg) ViewData["StatusMessage"] = msg;

        var user = GetSignedInUser();
        if (user is null) return RedirectToAction("Login");

        return View(new ManageSecurityQuestionsViewModel
        {
            SecurityQuestion1 = user.SecurityQuestion1 ?? string.Empty,
            SecurityQuestion2 = user.SecurityQuestion2 ?? string.Empty,
        });
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult SecurityQuestions(ManageSecurityQuestionsViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var user = GetSignedInUser();
        if (user is null) return RedirectToAction("Login");

        if (user.PasswordHash is null || !PasswordHasher.Verify(model.CurrentPassword, user.PasswordHash))
        {
            ModelState.AddModelError(nameof(model.CurrentPassword), "Current password is incorrect.");
            return View(model);
        }

        user.SecurityQuestion1 = model.SecurityQuestion1;
        user.SecurityAnswer1Hash = SecurityAnswerHasher.Hash(model.SecurityAnswer1);
        user.SecurityQuestion2 = model.SecurityQuestion2;
        user.SecurityAnswer2Hash = SecurityAnswerHasher.Hash(model.SecurityAnswer2);
        user.SecurityAnswerFailedAttempts = 0;
        user.SecurityAnswerLockoutEndsAt = null;
        _repository.AppUser.Update(user);

        _repository.AuditLog.Add(new AuditLogEntry
        {
            UserId = user.Id,
            ActionType = "UpdateSecurityQuestions",
            Description = $"{user.FullName} updated their security questions.",
        });

        TempData["StatusMessage"] = "Security questions saved.";
        return RedirectToAction(nameof(SecurityQuestions));
    }

    private enum SmsSendOutcome { Sent, NothingToSend, CoolingDown, InvalidNumber, TooManyRequests, Failed }

    private async Task<SmsSendOutcome> SendSmsCodeAsync(string email)
    {
        var user = _repository.AppUser.FindByEmail(email);
        if (user is null || !user.IsActive)
        {
            _logger.LogInformation("SMS reset requested for unknown or inactive address {Email}", email);
            return SmsSendOutcome.NothingToSend;
        }

        var phone = PhoneNumbers.ToE164(user.PhoneNumber);
        if (phone is null)
        {
            _logger.LogWarning("SMS reset requested for {Email}, but their phone number '{Phone}' can't be used for SMS",
                email, user.PhoneNumber);
            return SmsSendOutcome.NothingToSend;
        }

        var cooldownKey = "sms-sent:" + user.Email.ToLowerInvariant();
        if (_cache.TryGetValue(cooldownKey, out _)) return SmsSendOutcome.CoolingDown;

        var status = await _sms.SendCodeAsync(phone);
        if (status != SmsSendStatus.Sent)
        {
            return status switch
            {
                SmsSendStatus.InvalidNumber => SmsSendOutcome.InvalidNumber,
                SmsSendStatus.TooManyRequests => SmsSendOutcome.TooManyRequests,
                _ => SmsSendOutcome.Failed,
            };
        }

        _cache.Set(cooldownKey, true, SmsResendCooldown);
        _repository.AuditLog.Add(new AuditLogEntry
        {
            UserId = user.Id,
            ActionType = "ResetCodeSent",
            Description = $"Password reset code texted to {PhoneNumbers.Mask(phone)}.",
        });
        return SmsSendOutcome.Sent;
    }

    private static string? SmsSendError(SmsSendOutcome outcome) => outcome switch
    {
        SmsSendOutcome.InvalidNumber =>
            "The phone number on this account can't receive text messages. Choose \"Answer my security questions\" instead.",
        SmsSendOutcome.TooManyRequests =>
            "Too many codes have been sent to this number recently. Try again later, or answer your security questions instead.",
        SmsSendOutcome.Failed =>
            "We couldn't send a text right now. Please try again in a few minutes, or answer your security questions instead.",
        _ => null,   
    };

    private AppUser? GetSignedInUser()
    {
        var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(idClaim, out var userId) ? _repository.AppUser.GetById(userId) : null;
    }

    private static bool IsLockedOut(AppUser user, out string message)
    {
        if (user.SecurityAnswerLockoutEndsAt is DateTime endsAt && endsAt > DateTime.UtcNow)
        {
            var minutes = Math.Max(1, (int)Math.Ceiling((endsAt - DateTime.UtcNow).TotalMinutes));
            message = $"Too many incorrect attempts. Try again in {minutes} minute{(minutes == 1 ? "" : "s")}.";
            return true;
        }

        message = string.Empty;
        return false;
    }

    private static bool IsResetTokenValid(AppUser? user, string? token) =>
        user is not null
        && !string.IsNullOrEmpty(token)
        && user.IsActive
        && PasswordResetTokens.Matches(token, user.ResetToken)
        && user.ResetTokenExpiresAt is DateTime expires
        && expires >= DateTime.UtcNow;

    private Task SignInUserAsync(AppUser user, bool rememberMe) =>
        UserSignIn.SignInAsync(HttpContext, user, rememberMe);
}
