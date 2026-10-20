using CareerSyncAI.Services;
using Xunit;

namespace CareerSyncAI.Tests;

public class PasswordResetTests
{
    [Fact]
    public void Create_ReturnsUrlSafeTokenAndMatchingHash()
    {
        var (raw, hash) = PasswordResetTokens.Create();

        Assert.True(raw.Length >= 40);
        Assert.DoesNotContain("+", raw);
        Assert.DoesNotContain("/", raw);
        Assert.DoesNotContain("=", raw);
        Assert.True(PasswordResetTokens.Matches(raw, hash));
    }

    [Fact]
    public void Create_NeverStoresTheRawToken()
    {
        var (raw, hash) = PasswordResetTokens.Create();

        Assert.NotEqual(raw, hash);
        Assert.DoesNotContain(raw, hash);
    }

    [Fact]
    public void Create_GivesDifferentTokensEachTime()
    {
        Assert.NotEqual(PasswordResetTokens.Create().Raw, PasswordResetTokens.Create().Raw);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("wrong-token")]
    public void Matches_RejectsMissingOrWrongTokens(string? attempt)
    {
        var (_, hash) = PasswordResetTokens.Create();

        Assert.False(PasswordResetTokens.Matches(attempt, hash));
    }

    [Fact]
    public void Matches_RejectsWhenNothingIsStored()
    {
        var (raw, _) = PasswordResetTokens.Create();

        Assert.False(PasswordResetTokens.Matches(raw, null));
    }
}
