using System.Text.RegularExpressions;

namespace CareerSyncAI.Services;

public static class PhoneNumbers
{
    public const string DefaultCountryCode = "27"; 

    private static readonly Regex E164 = new(@"^\+[1-9]\d{7,14}$", RegexOptions.Compiled);

    public static string? ToE164(string? raw, string defaultCountryCode = DefaultCountryCode)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;

        var trimmed = raw.Trim();
        var hasPlus = trimmed.StartsWith('+');
        var digits = new string(trimmed.Where(char.IsDigit).ToArray());
        if (digits.Length == 0) return null;

        string candidate;
        if (hasPlus)
            candidate = "+" + digits;                                       
        else if (digits.StartsWith("00"))
            candidate = "+" + digits[2..];                                  
        else if (digits.StartsWith('0'))
            candidate = "+" + defaultCountryCode + digits[1..];             
        else if (digits.StartsWith(defaultCountryCode) && digits.Length == defaultCountryCode.Length + 9)
            candidate = "+" + digits;                                      
        else
            return null;                                                    

        if (candidate.StartsWith("+27") && candidate.Length != 12) return null;

        return E164.IsMatch(candidate) ? candidate : null;
    }
    public static string Mask(string e164) =>
        e164.Length < 8 ? "••••" : $"{e164[..3]} •• ••• {e164[^4..]}";
}
