using CareerSyncAI.Services;
using Xunit;

namespace CareerSyncAI.Tests;

public class PasswordHasherTests
{
    [Fact]
    public void Hash_IsSaltedSoSamePasswordGivesDifferentHashes()
    {
        var hash1 = PasswordHasher.Hash("MyPassword123");
        var hash2 = PasswordHasher.Hash("MyPassword123");

        Assert.NotEqual(hash1, hash2);
        Assert.True(PasswordHasher.Verify("MyPassword123", hash1));
        Assert.True(PasswordHasher.Verify("MyPassword123", hash2));
    }

    [Fact]
    public void Verify_AcceptsLegacySha256HashesAndFlagsThemForRehash()
    {
        // Exactly what the old hasher stored: unsalted SHA-256, upper-case hex.
        var legacy = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes("Graduate123!")));

        Assert.True(PasswordHasher.Verify("Graduate123!", legacy));
        Assert.False(PasswordHasher.Verify("graduate123!", legacy));
        Assert.True(PasswordHasher.NeedsRehash(legacy));
        Assert.False(PasswordHasher.NeedsRehash(PasswordHasher.Hash("Graduate123!")));
    }

    [Fact]
    public void Verify_ReturnsFalseForMissingOrMalformedHash()
    {
        Assert.False(PasswordHasher.Verify("x", null));
        Assert.False(PasswordHasher.Verify("x", ""));
        Assert.False(PasswordHasher.Verify("x", "PBKDF2$abc$%%%$###"));
    }

    [Fact]
    public void Hash_ProducesDifferentOutputForDifferentInput()
    {
        var hash1 = PasswordHasher.Hash("MyPassword123");
        var hash2 = PasswordHasher.Hash("DifferentPassword456");

        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void Hash_NeverReturnsThePlainTextPassword()
    {
        var hash = PasswordHasher.Hash("CorrectHorseBatteryStaple");

        Assert.DoesNotContain("CorrectHorseBatteryStaple", hash);
    }

    [Fact]
    public void Verify_ReturnsTrueForCorrectPassword()
    {
        var hash = PasswordHasher.Hash("CorrectHorseBatteryStaple");

        Assert.True(PasswordHasher.Verify("CorrectHorseBatteryStaple", hash));
    }

    [Fact]
    public void Verify_ReturnsFalseForIncorrectPassword()
    {
        var hash = PasswordHasher.Hash("CorrectHorseBatteryStaple");

        Assert.False(PasswordHasher.Verify("WrongPassword", hash));
    }

    [Fact]
    public void Verify_IsCaseSensitive()
    {
        var hash = PasswordHasher.Hash("CorrectHorseBatteryStaple");

        Assert.False(PasswordHasher.Verify("correcthorsebatterystaple", hash));
    }
}
