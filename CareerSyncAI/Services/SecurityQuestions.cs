using System.Security.Cryptography;
using System.Text;

namespace CareerSyncAI.Services;

public static class SecurityQuestions
{
    public static readonly IReadOnlyList<string> All = new[]
    {
        "What was the name of your first pet?",
        "In what city or town were you born?",
        "What is your mother's maiden name?",
        "What was the name of your primary school?",
        "What was the make of your first car?",
        "What is the name of the street you grew up on?",
        "What was your childhood nickname?",
        "What is the first name of your oldest cousin?",
    };

    public static bool IsValid(string? question) =>
        question is not null && All.Contains(question);
}
public static class SecurityAnswerHasher
{
    private const int SaltSize = 16;
    private const int KeySize = 32;
    private const int Iterations = 100_000;

    public static string Normalize(string answer) =>
        string.Join(' ', (answer ?? string.Empty)
            .Trim()
            .ToLowerInvariant()
            .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

    public static string Hash(string answer)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var key = Derive(Normalize(answer), salt, Iterations);
        return $"{Iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(key)}";
    }

    public static bool Verify(string answer, string? storedHash)
    {
        if (string.IsNullOrWhiteSpace(storedHash)) return false;

        var parts = storedHash.Split('.');
        if (parts.Length != 3 || !int.TryParse(parts[0], out var iterations)) return false;

        byte[] salt, expected;
        try
        {
            salt = Convert.FromBase64String(parts[1]);
            expected = Convert.FromBase64String(parts[2]);
        }
        catch (FormatException)
        {
            return false;
        }

        var actual = Derive(Normalize(answer), salt, iterations);
        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }

    private static byte[] Derive(string normalizedAnswer, byte[] salt, int iterations) =>
        Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(normalizedAnswer),
            salt,
            iterations,
            HashAlgorithmName.SHA256,
            KeySize);
}
