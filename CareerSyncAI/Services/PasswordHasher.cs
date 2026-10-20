using System.Security.Cryptography;
using System.Text;

namespace CareerSyncAI.Services;

public static class PasswordHasher
{
    private const string Prefix = "PBKDF2$";
    private const int SaltSize = 16;
    private const int KeySize = 32;
    private const int Iterations = 100_000;

    public static string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var key = Derive(password, salt, Iterations);
        return $"{Prefix}{Iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(key)}";
    }

    public static bool Verify(string password, string? storedHash)
    {
        if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(storedHash)) return false;

        if (storedHash.StartsWith(Prefix, StringComparison.Ordinal))
        {
            var parts = storedHash.Substring(Prefix.Length).Split('$');
            if (parts.Length != 3 || !int.TryParse(parts[0], out var iterations) || iterations <= 0) return false;
            try
            {
                var salt = Convert.FromBase64String(parts[1]);
                var expected = Convert.FromBase64String(parts[2]);
                return CryptographicOperations.FixedTimeEquals(Derive(password, salt, iterations), expected);
            }
            catch (FormatException)
            {
                return false;
            }
        }

        if (storedHash.Length == 64)
        {
            var legacy = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(password)));
            return CryptographicOperations.FixedTimeEquals(
                Encoding.ASCII.GetBytes(legacy),
                Encoding.ASCII.GetBytes(storedHash.ToUpperInvariant()));
        }

        return false;
    }

    public static bool NeedsRehash(string? storedHash) =>
        storedHash is null || !storedHash.StartsWith(Prefix, StringComparison.Ordinal);

    private static byte[] Derive(string password, byte[] salt, int iterations) =>
        Rfc2898DeriveBytes.Pbkdf2(Encoding.UTF8.GetBytes(password), salt, iterations, HashAlgorithmName.SHA256, KeySize);
}
