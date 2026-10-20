using System.ComponentModel.DataAnnotations;

namespace CareerSyncAI.Models.ViewModels;

public enum PasswordRecoveryMethod
{
    Sms,

    SecurityQuestions,
}

public class ForgotPasswordViewModel
{
    [Required, EmailAddress]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Display(Name = "How would you like to reset it?")]
    public PasswordRecoveryMethod Method { get; set; } = PasswordRecoveryMethod.Sms;
}

public class VerifySmsCodeViewModel
{
    [Required]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Enter the 6-digit code.")]
    [RegularExpression(@"^\s*\d{6}\s*$", ErrorMessage = "The code is 6 digits.")]
    [Display(Name = "Verification code")]
    public string Code { get; set; } = string.Empty;
}
