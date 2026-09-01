using VRepo.Common.Serialization.Exceptions;
using VRepo.Configuration.Versions.V1Alpha1.Models;

namespace VRepo.Configuration.Tests;

public class VirtualRepositoryConfigTests
{
    [Fact]
    public void CreateSerializedExample_DeserializesSuccessfully()
    {
        var yaml = VirtualRepositoryConfig.CreateSerializedExample();

        var config = VirtualRepositoryConfig.Deserialize(yaml);

        Assert.Equal("v1alpha1", config.ApiVersion);
        Assert.NotNull(config.Upstream);
        Assert.NotEmpty(config.Upstream.Repositories);
    }

    [Fact]
    public void Deserialize_InvalidYaml_ThrowsSerializationException()
    {
        var invalid = "not: [balanced";

        Assert.Throws<SerializationException>(() => VirtualRepositoryConfig.Deserialize(invalid));
    }

    [Fact]
    public void Deserialize_FromTextReader_Works()
    {
        var yaml = "apiVersion: v1alpha1\n";

        using var reader = new StringReader(yaml);
        var config = VirtualRepositoryConfig.Deserialize(reader);

        Assert.Equal("v1alpha1", config.ApiVersion);
    }

    [Fact]
    public void Serialize_Then_Deserialize_RoundTrip_MinimalConfig()
    {
        var config = new VirtualRepositoryConfig();

        var yaml = config.Serialize();

        var deserialized = VirtualRepositoryConfig.Deserialize(yaml);

        Assert.Equal(config.ApiVersion, deserialized.ApiVersion);
        Assert.Null(deserialized.Upstream);
        Assert.Null(deserialized.ManualRepository);
    }

    [Fact]
    public void VersionPatternConverter_ParsesPatterns_InYaml()
    {
        var yaml = @"
apiVersion: v1alpha1
upstream:
  repositories:
    - url: https://example.com/charts
      charts:
        mychart:
          versionPolicy:
            exclude:
              versions:
                - '<15.0.0'
";

        var config = VirtualRepositoryConfig.Deserialize(yaml);

        Assert.NotNull(config.Upstream);

        var repo = config.Upstream.Repositories[0];
        var chart = repo.Charts["mychart"];
        var patterns = chart.VersionPolicy?.ExclusionRules?.VersionExclusionPatterns;

        Assert.NotNull(patterns);
        var pattern = Assert.Single(patterns);
        Assert.Equal("<15.0.0", pattern.ToString());
    }
}
