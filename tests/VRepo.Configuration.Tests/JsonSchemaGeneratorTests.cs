using System.Text.Json;
using VRepo.Configuration.Services;
using VRepo.Configuration.Versions;

namespace VRepo.Configuration.Tests;

public class JsonSchemaGeneratorTests
{
    [Fact]
    public void Generate_ForAllVersions_ValidJsonIncludingTitle()
    {
        var generator = new JsonSchemaGenerator();

        var json = generator.Generate();

        using var doc = JsonDocument.Parse(json);

        Assert.Contains("helm-vrepo configuration YAML", json);
    }

    [Fact]
    public void Generate_ForSpecificApiVersion_ValidJsonIncludingTitleWithVersion()
    {
        var generator = new JsonSchemaGenerator();

        var json = generator.Generate(ApiVersion.Parse("v1alpha1"));

        using var doc = JsonDocument.Parse(json);

        Assert.Contains("helm-vrepo configuration YAML (v1alpha1)", json);
    }
}
