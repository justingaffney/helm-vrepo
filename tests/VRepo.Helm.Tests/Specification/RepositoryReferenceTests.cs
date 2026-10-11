using VRepo.Helm.Specification.Models.Repositories;
using VRepo.Helm.Specification.Models.Repositories.References;

namespace VRepo.Helm.Tests.Specification;

public class RepositoryReferenceTests
{
    [Theory]
    [InlineData("", typeof(LocalChartsDirectoryRepository))]
    [InlineData("   ", typeof(LocalChartsDirectoryRepository))]
    [InlineData("file://../charts/my-chart", typeof(LocalFilesystemUrlRepository))]
    [InlineData("https://example.com/charts", typeof(HttpUrlRepository))]
    [InlineData("http://example.com/charts", typeof(HttpUrlRepository))]
    [InlineData("oci://registry.example.com/charts", typeof(OciUrlRepository))]
    [InlineData("@stable", typeof(AliasRepository))]
    [InlineData("alias:stable", typeof(AliasRepository))]
    public void RepositoryReference_Parse_ReturnsExpectedReferenceType(string value, Type expectedType)
    {
        var reference = RepositoryReference.Parse(value);

        Assert.Equal(expectedType, GetReferenceType(reference));
    }

    [Theory]
    [InlineData("")]
    [InlineData("file://../charts/my-chart")]
    [InlineData("https://example.com/charts")]
    [InlineData("oci://registry.example.com/charts")]
    [InlineData("@stable")]
    [InlineData("alias:stable")]
    public void RepositoryReference_ToString_RoundTripsParsedValue(string value)
    {
        Assert.Equal(value, RepositoryReference.Parse(value).ToString());
    }

    [Theory]
    [InlineData("stable")]
    [InlineData("ftp://example.com/charts")]
    public void RepositoryReference_ParseAndTryParse_RejectInvalidValues(string value)
    {
        Assert.Throws<FormatException>(() => RepositoryReference.Parse(value));

        Assert.False(RepositoryReference.TryParse(value, out var result));
        Assert.Null(result);
    }

    [Fact]
    public void RepositoryReference_TryParse_AcceptsValidValues()
    {
        Assert.True(RepositoryReference.TryParse("@stable", out var result));
        Assert.Equal("@stable", result.Value.ToString());
    }

    [Theory]
    [InlineData("https://example.com/charts", typeof(HttpUrlRepository))]
    [InlineData("oci://registry.example.com/charts", typeof(OciUrlRepository))]
    public void RemoteRepositoryReference_Parse_ReturnsExpectedReferenceType(string value, Type expectedType)
    {
        var reference = RemoteRepositoryReference.Parse(value);

        Assert.Equal(expectedType, GetReferenceType(reference));
        Assert.Equal(value, reference.ToString());
    }

    [Theory]
    [InlineData("")]
    [InlineData("file://../charts")]
    [InlineData("@stable")]
    public void RemoteRepositoryReference_ParseAndTryParse_RejectNonRemoteValues(string value)
    {
        Assert.Throws<FormatException>(() => RemoteRepositoryReference.Parse(value));

        Assert.False(RemoteRepositoryReference.TryParse(value, out var result));
        Assert.Null(result);
    }

    [Fact]
    public void RemoteRepositoryReference_TryParse_AcceptsValidValues()
    {
        Assert.True(RemoteRepositoryReference.TryParse("oci://registry.example.com/charts", out var result));
        Assert.Equal("oci://registry.example.com/charts", result.Value.ToString());
    }

    [Theory]
    [InlineData("", typeof(LocalChartsDirectoryRepository))]
    [InlineData("file://../charts", typeof(LocalFilesystemUrlRepository))]
    public void LocalRepositoryReference_Parse_ReturnsExpectedReferenceType(string value, Type expectedType)
    {
        var reference = LocalRepositoryReference.Parse(value);

        Assert.Equal(expectedType, GetReferenceType(reference));
        Assert.Equal(value, reference.ToString());
    }

    [Theory]
    [InlineData("https://example.com/charts")]
    [InlineData("oci://registry.example.com/charts")]
    [InlineData("@stable")]
    public void LocalRepositoryReference_ParseAndTryParse_RejectNonLocalValues(string value)
    {
        Assert.Throws<FormatException>(() => LocalRepositoryReference.Parse(value));

        Assert.False(LocalRepositoryReference.TryParse(value, out var result));
        Assert.Null(result);
    }

    [Fact]
    public void LocalRepositoryReference_TryParse_AcceptsValidValues()
    {
        Assert.True(LocalRepositoryReference.TryParse("file://../charts", out var result));
        Assert.Equal("file://../charts", result.Value.ToString());
    }

    private static Type? GetReferenceType(RepositoryReference reference) => reference switch
    {
        LocalChartsDirectoryRepository => typeof(LocalChartsDirectoryRepository),
        LocalFilesystemUrlRepository => typeof(LocalFilesystemUrlRepository),
        HttpUrlRepository => typeof(HttpUrlRepository),
        OciUrlRepository => typeof(OciUrlRepository),
        AliasRepository => typeof(AliasRepository),
        _ => null
    };

    private static Type? GetReferenceType(RemoteRepositoryReference reference) => reference switch
    {
        HttpUrlRepository => typeof(HttpUrlRepository),
        OciUrlRepository => typeof(OciUrlRepository),
        _ => null
    };

    private static Type? GetReferenceType(LocalRepositoryReference reference) => reference switch
    {
        LocalChartsDirectoryRepository => typeof(LocalChartsDirectoryRepository),
        LocalFilesystemUrlRepository => typeof(LocalFilesystemUrlRepository),
        _ => null
    };
}
