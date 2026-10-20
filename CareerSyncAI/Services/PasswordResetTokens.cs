using System.Security.Cryptography;
using System.Text;

namespace CareerSyncAI.Services;

public static class PasswordResetTokens
{
    public static readonly TimeSpan VerifiedResetLifetime = TimeSpan.FromMinutes(15);

    public static (string Raw, string Hash) Create()
    {
        var raw = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');
        return (raw, Hash(raw));
    }

    public static string Hash(string rawToken) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));

    public static bool Matches(string? rawToken, string? storedHash)
    {
        if (string.IsNullOrEmpty(rawToken) || string.IsNullOrEmpty(storedHash)) return false;
        return CryptographicOperations.FixedTimeEquals(
            Encoding.ASCII.GetBytes(Hash(rawToken)),
            Encoding.ASCII.GetBytes(storedHash));
    }
}
