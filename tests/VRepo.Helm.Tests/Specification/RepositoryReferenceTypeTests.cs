using VRepo.Helm.Specification.Models.Repositories.References;

namespace VRepo.Helm.Tests.Specification;

public class RepositoryReferenceTypeTests
{
    [Theory]
    [InlineData("https://example.com/charts", "https://example.com/charts")]
    [InlineData("https://example.com/charts/", "https://example.com/charts")]
    [InlineData("https://example.com/charts/index.yaml", "https://example.com/charts")]
    [InlineData("https://example.com/charts?token=abc#fragment", "https://example.com/charts")]
    [InlineData("HTTPS://EXAMPLE.COM/charts", "https://example.com/charts")]
    [InlineData("http://example.com", "http://example.com/")]
    public void HttpUrlRepository_NormalizesUrl(string value, string expected)
    {
        var repository = new HttpUrlRepository(value);

        Assert.Equal(expected, repository.ToString());
    }

    [Theory]
    [InlineData("https://example.com/charts", "https://example.com/charts/index.yaml")]
    [InlineData("https://example.com/charts/index.yaml", "https://example.com/charts/index.yaml")]
    [InlineData("https://example.com", "https://example.com/index.yaml")]
    public void HttpUrlRepository_IndexFile_AppendsIndexFileName(string value, string expected)
    {
        var repository = new HttpUrlRepository(value);

        Assert.Equal(new Uri(expected), repository.IndexFile);
    }

    [Theory]
    [InlineData("oci://registry.example.com/charts")]
    [InlineData("file://../charts")]
    [InlineData("example.com/charts")]
    [InlineData("")]
    public void HttpUrlRepository_RejectsNonHttpUrls(string value)
    {
        Assert.False(HttpUrlRepository.Matches(value));
        Assert.Throws<ArgumentException>(() => new HttpUrlRepository(value));
    }

    [Fact]
    public void HttpUrlRepository_Equality_ComparesNormalizedUrls()
    {
        var repository = new HttpUrlRepository("https://example.com/charts");
        var equivalent = new HttpUrlRepository("https://EXAMPLE.com/charts/index.yaml");
        var different = new HttpUrlRepository("https://example.com/other");
        var copy = new HttpUrlRepository(repository);

        AssertEquality(repository, equivalent, different, copy);
        Assert.True(repository == equivalent);
        Assert.False(repository != equivalent);
        Assert.False(repository == different);
        Assert.True(repository != different);
        Assert.False(repository == null);
        Assert.True(repository != null);
        Assert.True((HttpUrlRepository?)null == null);
        Assert.False((HttpUrlRepository?)null != null);
    }

    [Theory]
    [InlineData("oci://registry.example.com/charts", "oci://registry.example.com/charts", "registry.example.com/charts")]
    [InlineData("oci://registry.example.com/charts/", "oci://registry.example.com/charts", "registry.example.com/charts")]
    [InlineData("oci://registry.example.com/charts?x=1#y", "oci://registry.example.com/charts", "registry.example.com/charts")]
    [InlineData("oci://localhost:5000/nested/charts", "oci://localhost:5000/nested/charts", "localhost:5000/nested/charts")]
    public void OciUrlRepository_NormalizesUrl_AndExposesOciRepository(string value, string expectedUrl, string expectedOciRepository)
    {
        var repository = new OciUrlRepository(value);

        Assert.Equal(expectedUrl, repository.ToString());
        Assert.Equal(expectedOciRepository, repository.OciRepository);
    }

    [Theory]
    [InlineData("https://example.com/charts")]
    [InlineData("registry.example.com/charts")]
    [InlineData("")]
    public void OciUrlRepository_RejectsNonOciUrls(string value)
    {
        Assert.False(OciUrlRepository.Matches(value));
        Assert.Throws<ArgumentException>(() => new OciUrlRepository(value));
    }

    [Fact]
    public void OciUrlRepository_Equality_ComparesNormalizedUrls()
    {
        var repository = new OciUrlRepository("oci://registry.example.com/charts");
        var equivalent = new OciUrlRepository("oci://registry.example.com/charts/");
        var different = new OciUrlRepository("oci://registry.example.com/other");
        var copy = new OciUrlRepository(repository);

        AssertEquality(repository, equivalent, different, copy);
        Assert.True(repository == equivalent);
        Assert.False(repository != equivalent);
        Assert.False(repository == different);
        Assert.True(repository != different);
        Assert.False(repository == null);
        Assert.True(repository != null);
    }

    [Theory]
    [InlineData("@stable", "stable")]
    [InlineData("alias:stable", "stable")]
    [InlineData("@", "")]
    public void AliasRepository_ParsesAliasValue_AndPreservesOriginal(string value, string expectedAlias)
    {
        var repository = new AliasRepository(value);

        Assert.Equal(expectedAlias, repository.Value);
        Assert.Equal(value, repository.ToString());
    }

    [Theory]
    [InlineData("stable")]
    [InlineData("  ")]
    [InlineData("")]
    public void AliasRepository_RejectsValuesWithoutAliasPrefix(string value)
    {
        Assert.False(AliasRepository.Matches(value));
        Assert.Throws<ArgumentException>(() => new AliasRepository(value));
    }

    [Fact]
    public void AliasRepository_Equality_ComparesOriginalValue()
    {
        var repository = new AliasRepository("@stable");
        var equivalent = new AliasRepository("@stable");
        var differentPrefix = new AliasRepository("alias:stable");
        var copy = new AliasRepository(repository);

        AssertEquality(repository, equivalent, differentPrefix, copy);
        Assert.True(repository == equivalent);
        Assert.False(repository != equivalent);
        Assert.False(repository == differentPrefix);
        Assert.True(repository != differentPrefix);
        Assert.False(repository == null);
        Assert.True(repository != null);
    }

    [Theory]
    [InlineData("file://../charts")]
    [InlineData("FILE:///absolute/path")]
    public void LocalFilesystemUrlRepository_AcceptsFileUrls_AndPreservesOriginal(string value)
    {
        var repository = new LocalFilesystemUrlRepository(value);

        Assert.True(LocalFilesystemUrlRepository.Matches(value));
        Assert.Equal(value, repository.ToString());
    }

    [Theory]
    [InlineData("https://example.com/charts")]
    [InlineData("../charts")]
    public void LocalFilesystemUrlRepository_RejectsNonFileUrls(string value)
    {
        Assert.False(LocalFilesystemUrlRepository.Matches(value));
        Assert.Throws<ArgumentException>(() => new LocalFilesystemUrlRepository(value));
    }

    [Fact]
    public void LocalFilesystemUrlRepository_Equality_ComparesOriginalValue()
    {
        var repository = new LocalFilesystemUrlRepository("file://../charts");
        var equivalent = new LocalFilesystemUrlRepository("file://../charts");
        var different = new LocalFilesystemUrlRepository("file://../other");
        var copy = new LocalFilesystemUrlRepository(repository);

        AssertEquality(repository, equivalent, different, copy);
        Assert.True(repository == equivalent);
        Assert.False(repository != equivalent);
        Assert.False(repository == different);
        Assert.True(repository != different);
        Assert.False(repository == null);
        Assert.True(repository != null);
    }

    [Fact]
    public void LocalChartsDirectoryRepository_ToString_IsEmpty()
    {
        Assert.Equal(string.Empty, new LocalChartsDirectoryRepository().ToString());
    }

    private static void AssertEquality<T>(T value, T equivalent, T different, T copy) where T : class
    {
        Assert.Equal(value, equivalent);
        Assert.Equal(value.GetHashCode(), equivalent.GetHashCode());
        Assert.Equal(value, copy);
        Assert.NotSame(value, copy);
        Assert.NotEqual(value, different);
        Assert.False(value.Equals(null));
        Assert.False(value.Equals("not a repository"));
    }
}
