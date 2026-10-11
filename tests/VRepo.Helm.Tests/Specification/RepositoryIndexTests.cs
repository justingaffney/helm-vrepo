using VRepo.Common.Serialization.Exceptions;
using VRepo.Helm.Specification.Models.Charts;
using VRepo.Helm.Specification.Models.Repositories;
using VRepo.Helm.Specification.Models.Repositories.References;

namespace VRepo.Helm.Tests.Specification;

public class RepositoryIndexTests
{
    private const string IndexYaml = """
        apiVersion: v1
        entries:
          alpha:
            - apiVersion: v2
              name: alpha
              version: "1.2.0"
              appVersion: "1.0"
              description: Alpha chart
              keywords:
                - first
                - second
              maintainers:
                - name: Jane
                  email: jane@example.com
              dependencies:
                - name: common
                  version: "2.0.0"
                  repository: "@stable"
              urls:
                - https://example.com/charts/alpha-1.2.0.tgz
              digest: sha256:abc
              created: 2026-01-02T03:04:05Z
              unknownProperty: ignored
        generated: 2026-02-03T04:05:06Z
        """;

    [Fact]
    public void Deserialize_ReadsRepositoryIndexYaml()
    {
        var index = RepositoryIndex.Deserialize(IndexYaml);

        AssertDeserializedIndex(index);
    }

    [Fact]
    public void Deserialize_FromTextReader_ReadsRepositoryIndexYaml()
    {
        var index = RepositoryIndex.Deserialize(new StringReader(IndexYaml));

        AssertDeserializedIndex(index);
    }

    [Fact]
    public void Deserialize_ThrowsSerializationException_ForInvalidYaml()
    {
        Assert.Throws<SerializationException>(() => RepositoryIndex.Deserialize("entries: [unterminated"));
        Assert.Throws<SerializationException>(() => RepositoryIndex.Deserialize(new StringReader("entries: [unterminated")));
    }

    [Fact]
    public void Serialize_RoundTripsRepositoryIndex()
    {
        var index = RepositoryIndex.Deserialize(IndexYaml);

        var yaml = index.Serialize();

        Assert.Contains("apiVersion: v1", yaml, StringComparison.Ordinal);
        Assert.Contains("version: \"1.2.0\"", yaml, StringComparison.Ordinal);
        Assert.Contains("repository: '@stable'", yaml, StringComparison.Ordinal);
        Assert.Contains("generated: 2026-02-03T04:05:06Z", yaml, StringComparison.Ordinal);
        Assert.DoesNotContain("unknownProperty", yaml, StringComparison.Ordinal);
        Assert.DoesNotContain("deprecated", yaml, StringComparison.Ordinal);

        AssertDeserializedIndex(RepositoryIndex.Deserialize(yaml));
    }

    [Fact]
    public void Serialize_ToTextWriter_MatchesStringSerialization()
    {
        var index = RepositoryIndex.Deserialize(IndexYaml);

        using var writer = new StringWriter();
        index.Serialize(writer);

        Assert.Equal(index.Serialize(), writer.ToString());
    }

    [Fact]
    public void Serialize_OrdersRepositoryChartPropertiesAfterChartMetadata()
    {
        var yaml = RepositoryIndex.Deserialize(IndexYaml).Serialize();

        var descriptionIndex = yaml.IndexOf("description:", StringComparison.Ordinal);
        var urlsIndex = yaml.IndexOf("urls:", StringComparison.Ordinal);
        var digestIndex = yaml.IndexOf("digest:", StringComparison.Ordinal);
        var createdIndex = yaml.IndexOf("created:", StringComparison.Ordinal);

        Assert.True(descriptionIndex < urlsIndex);
        Assert.True(urlsIndex < digestIndex);
        Assert.True(digestIndex < createdIndex);
    }

    private static void AssertDeserializedIndex(RepositoryIndex index)
    {
        Assert.Equal(RepositoryIndex.ValidApiVersion, index.ApiVersion);
        Assert.Equal(new DateTimeOffset(2026, 2, 3, 4, 5, 6, TimeSpan.Zero), index.GenerationTime);

        var chart = Assert.Single(Assert.Single(index.Entries).Value);
        Assert.Equal("v2", chart.ApiVersion);
        Assert.Equal("alpha", chart.Name);
        Assert.Equal("1.2.0", chart.Version);
        Assert.Equal("1.0", chart.AppVersion);
        Assert.Equal("Alpha chart", chart.Description);
        Assert.Equal(ChartMetadata.ChartTypes.Application, chart.Type);
        Assert.Equal(["first", "second"], chart.Keywords);
        Assert.False(chart.Deprecated);
        Assert.Equal(["https://example.com/charts/alpha-1.2.0.tgz"], chart.Urls);
        Assert.Equal("sha256:abc", chart.Digest);
        Assert.Equal(new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero), chart.CreationTime);

        var maintainer = Assert.Single(chart.Maintainers!);
        Assert.Equal("Jane", maintainer.Name);
        Assert.Equal("jane@example.com", maintainer.Email);

        var dependency = Assert.Single(chart.Dependencies!);
        Assert.Equal("common", dependency.Name);
        Assert.Equal("2.0.0", dependency.Version);
        Assert.True(dependency.Repository is AliasRepository);
        Assert.Equal("@stable", dependency.Repository.ToString());
    }
}
