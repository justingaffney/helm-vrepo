using VRepo.Common.Versioning;

namespace VRepo.Common.Tests.Versioning;

public class SemVerHelperTests
{
    [Fact]
    public void Normalize_RemovesVersionPrefixAndLeadingZeroes()
    {
        Assert.Equal("1.2.3", SemVerHelper.Normalize("v01.02.03"));
        Assert.Equal("1.2.3-alpha.1", SemVerHelper.Normalize("v1.2.3-alpha.1"));
    }

    [Fact]
    public void SortVersionedElementsDescending_OrdersHigherVersionsFirst()
    {
        var versions = new[] { "foo", "v1.2.3", "2.0.0", "1.9.9" };

        var result = SemVerHelper.SortVersionedElements(versions, version => version, ascending: false).ToArray();

        Assert.Equal(new[] { "2.0.0", "1.9.9", "v1.2.3", "foo" }, result);
    }
}
