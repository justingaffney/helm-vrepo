using VRepo.Common.Versioning;

namespace VRepo.Common.Tests.Versioning;

public class RegexHelperTests
{
    [Fact]
    public void TryParse_RecognizesValidPatternsAndRejectsInvalidOnes()
    {
        Assert.True(RegexHelper.TryParse("^v?\\d+\\.\\d+$", out var regex));
        Assert.NotNull(regex);
        Assert.Matches(regex!, "v1.2");
        Assert.DoesNotMatch(regex!, "1.2.3");

        Assert.False(RegexHelper.TryParse("(", out _));
    }

    [Fact]
    public void WildcardToRegex_TransformsWildcardPatterns()
    {
        var pattern = RegexHelper.WildcardToRegex("chart-*");

        Assert.Matches(pattern, "chart-1.2.3");
        Assert.DoesNotMatch(pattern, "chart");
    }
}
