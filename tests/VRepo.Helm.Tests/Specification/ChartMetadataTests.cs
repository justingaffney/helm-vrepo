using VRepo.Common.Serialization.Exceptions;
using VRepo.Helm.Specification.Models.Charts;
using VRepo.Helm.Specification.Models.Repositories.References;

namespace VRepo.Helm.Tests.Specification;

public class ChartMetadataTests
{
    private const string ChartYaml = """
        apiVersion: v2
        name: my-chart
        version: 1.0.0
        kubeVersion: ">=1.28.0"
        type: library
        home: https://example.com
        sources:
          - https://github.com/example/my-chart
        icon: https://example.com/icon.png
        appVersion: 1.16.0
        deprecated: true
        annotations:
          category: Infrastructure
        dependencies:
          - name: http-dependency
            version: ~1.0.0
            repository: https://example.com/charts
            condition: httpDependency.enabled
            tags:
              - backend
            alias: http-dep
          - name: oci-dependency
            version: 2.0.0
            repository: oci://registry.example.com/charts
          - name: local-dependency
            version: 3.0.0
            repository: file://../local-dependency
        """;

    [Fact]
    public void Deserialize_ReadsChartYaml()
    {
        AssertDeserializedChart(ChartMetadata.Deserialize(ChartYaml));
    }

    [Fact]
    public void Deserialize_FromTextReader_ReadsChartYaml()
    {
        AssertDeserializedChart(ChartMetadata.Deserialize(new StringReader(ChartYaml)));
    }

    [Fact]
    public void Deserialize_ThrowsSerializationException_ForInvalidYaml()
    {
        Assert.Throws<SerializationException>(() => ChartMetadata.Deserialize("name: [unterminated"));
        Assert.Throws<SerializationException>(() => ChartMetadata.Deserialize(new StringReader("name: [unterminated")));
    }

    [Fact]
    public void Serialize_QuotesVersionFields_AndRoundTrips()
    {
        var chart = ChartMetadata.Deserialize(ChartYaml);

        var yaml = chart.Serialize();

        Assert.Contains("version: \"1.0.0\"", yaml, StringComparison.Ordinal);
        Assert.Contains("appVersion: \"1.16.0\"", yaml, StringComparison.Ordinal);
        Assert.Contains("kubeVersion: \">=1.28.0\"", yaml, StringComparison.Ordinal);

        AssertDeserializedChart(ChartMetadata.Deserialize(yaml));
    }

    [Fact]
    public void Serialize_ToTextWriter_MatchesStringSerialization()
    {
        var chart = ChartMetadata.Deserialize(ChartYaml);

        using var writer = new StringWriter();
        chart.Serialize(writer);

        Assert.Equal(chart.Serialize(), writer.ToString());
    }

    [Fact]
    public void Serialize_OmitsDefaultAndUnsetValues()
    {
        var chart = new ChartMetadata
        {
            ApiVersion = ChartMetadata.LatestStableApiVersion,
            Name = "minimal",
            Version = "0.1.0"
        };

        var yaml = chart.Serialize();

        Assert.Equal(
            """
            apiVersion: v2
            name: minimal
            version: "0.1.0"
            type: application

            """.ReplaceLineEndings(),
            yaml.ReplaceLineEndings());
    }

    private static void AssertDeserializedChart(ChartMetadata chart)
    {
        Assert.Equal(ChartMetadata.ApiVersionV2, chart.ApiVersion);
        Assert.Equal("my-chart", chart.Name);
        Assert.Equal("1.0.0", chart.Version);
        Assert.Equal(">=1.28.0", chart.KubeVersion);
        Assert.Equal(ChartMetadata.ChartTypes.Library, chart.Type);
        Assert.Equal("https://example.com", chart.HomeUrl);
        Assert.Equal(["https://github.com/example/my-chart"], chart.SourceUrls);
        Assert.Equal("https://example.com/icon.png", chart.IconUrl);
        Assert.Equal("1.16.0", chart.AppVersion);
        Assert.True(chart.Deprecated);
        Assert.Equal("Infrastructure", Assert.Contains("category", chart.Annotations!));

        Assert.NotNull(chart.Dependencies);
        Assert.Equal(3, chart.Dependencies.Count);

        var httpDependency = chart.Dependencies[0];
        Assert.Equal("http-dependency", httpDependency.Name);
        Assert.Equal("~1.0.0", httpDependency.Version);
        Assert.True(httpDependency.Repository is HttpUrlRepository);
        Assert.Equal("httpDependency.enabled", httpDependency.Condition);
        Assert.Equal(["backend"], httpDependency.Tags);
        Assert.Equal("http-dep", httpDependency.Alias);

        Assert.True(chart.Dependencies[1].Repository is OciUrlRepository);
        Assert.True(chart.Dependencies[2].Repository is LocalFilesystemUrlRepository);
    }
}
