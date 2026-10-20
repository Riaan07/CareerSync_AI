using CareerSyncAI.Services;
using Xunit;

namespace CareerSyncAI.Tests;

public class SecurityAnswerHasherTests
{
    [Fact]
    public void Verify_ReturnsTrueForCorrectAnswer()
    {
        var hash = SecurityAnswerHasher.Hash("Bloemfontein");

        Assert.True(SecurityAnswerHasher.Verify("Bloemfontein", hash));
    }

    [Fact]
    public void Verify_ReturnsFalseForWrongAnswer()
    {
        var hash = SecurityAnswerHasher.Hash("Bloemfontein");

        Assert.False(SecurityAnswerHasher.Verify("Durban", hash));
    }

    [Theory]
    [InlineData("bloemfontein")]
    [InlineData("BLOEMFONTEIN")]
    [InlineData("  Bloemfontein  ")]
    public void Verify_IgnoresCaseAndSurroundingWhitespace(string attempt)
    {
        var hash = SecurityAnswerHasher.Hash("Bloemfontein");

        Assert.True(SecurityAnswerHasher.Verify(attempt, hash));
    }

    [Fact]
    public void Verify_CollapsesInnerWhitespace()
    {
        var hash = SecurityAnswerHasher.Hash("Grey College");

        Assert.True(SecurityAnswerHasher.Verify("grey    college", hash));
    }

    [Fact]
    public void Hash_IsSaltedSoSameAnswerGivesDifferentHashes()
    {
        var hash1 = SecurityAnswerHasher.Hash("Rex");
        var hash2 = SecurityAnswerHasher.Hash("Rex");

        Assert.NotEqual(hash1, hash2);
        Assert.True(SecurityAnswerHasher.Verify("Rex", hash1));
        Assert.True(SecurityAnswerHasher.Verify("Rex", hash2));
    }

    [Fact]
    public void Hash_NeverContainsThePlainTextAnswer()
    {
        var hash = SecurityAnswerHasher.Hash("Bloemfontein");

        Assert.DoesNotContain("bloemfontein", hash, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-a-valid-hash")]
    [InlineData("100000.%%%.###")]
    public void Verify_ReturnsFalseForMissingOrMalformedHash(string? storedHash)
    {
        Assert.False(SecurityAnswerHasher.Verify("anything", storedHash));
    }

    [Fact]
    public void SecurityQuestions_IsValidOnlyAcceptsListedQuestions()
    {
        Assert.True(SecurityQuestions.IsValid(SecurityQuestions.All[0]));
        Assert.False(SecurityQuestions.IsValid("What is your password?"));
        Assert.False(SecurityQuestions.IsValid(null));
    }
}
