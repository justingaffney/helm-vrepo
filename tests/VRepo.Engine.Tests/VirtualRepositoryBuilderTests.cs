using System.ComponentModel.DataAnnotations;
using VRepo.Engine.Models;
using VRepo.Engine.Models.Policies;
using VRepo.Engine.Models.Upstream;
using VRepo.Helm.Specification.Models.Repositories;
using VRepo.Helm.Specification.Models.Repositories.References;

namespace VRepo.Engine.Tests;

public class VirtualRepositoryBuilderTests
{
    private static ChartVersionPolicy CreatePolicy() => new()
    {
        ExclusionRules = new ChartVersionExclusionRules
        {
            ExcludeInvalidSemVer = false,
            ExcludePrereleaseVersions = false,
            VersionExclusionPatterns = []
        }
    };

    private static RepositoryChartMetadata CreateChartVersion(string name, string version) => new()
    {
        ApiVersion = "v2",
        Name = name,
        Version = version,
        Urls = ["https://example.com/chart.tgz"]
    };

    [Fact]
    public void UpstreamHttpRepositoryBuilder_DuplicateCharts_ThrowsValidationException()
    {
        var url = new HttpUrlRepository("https://example.com/charts");
        var builder = new UpstreamHttpRepository.Builder(url);

        builder.AddChart(new UpstreamChart { Name = "dup", VersionPolicy = CreatePolicy() });
        builder.AddChart(new UpstreamChart { Name = "dup", VersionPolicy = CreatePolicy() });

        Assert.Throws<ValidationException>(builder.Build);
    }

    [Fact]
    public void UpstreamOciRepositoryBuilder_DuplicateCharts_ThrowsValidationException()
    {
        var url = new OciUrlRepository("oci://example.com/charts");
        var builder = new UpstreamOciRepository.Builder(url);

        builder.AddChart(new UpstreamChart { Name = "dup", VersionPolicy = CreatePolicy() });
        builder.AddChart(new UpstreamChart { Name = "dup", VersionPolicy = CreatePolicy() });

        Assert.Throws<ValidationException>(builder.Build);
    }

    [Fact]
    public void RepositoryIndexContentsBuilder_FiltersOutChartsWithNoVersions()
    {
        var builder = new RepositoryIndexContents.Builder();

        builder.AddChart(new RepositoryIndexChart { Name = "empty", Versions = [] });
        builder.AddChart(new RepositoryIndexChart { Name = "has", Versions = [CreateChartVersion("has", "1.0.0")] });

        var contents = builder.Build();

        var chart = Assert.Single(contents.Charts);
        Assert.Equal("has", chart.Name);
    }

    [Fact]
    public void VirtualRepositoryBuilder_DuplicateUpstreamRepositories_ThrowsValidationException()
    {
        var builder = new VirtualRepository.Builder();

        var httpUrl = new HttpUrlRepository("https://example.com/charts");

        builder.AddUpstreamHttpRepository(httpUrl, repo => repo.AddChart(new UpstreamChart { Name = "a", VersionPolicy = CreatePolicy() }));
        builder.AddUpstreamHttpRepository(httpUrl, repo => repo.AddChart(new UpstreamChart { Name = "b", VersionPolicy = CreatePolicy() }));

        Assert.Throws<ValidationException>(builder.Build);
    }

    [Fact]
    public void VirtualRepositoryBuilder_ChartNameConflictsBetweenSources_ThrowsValidationException()
    {
        var builder = new VirtualRepository.Builder();

        var httpUrl = new HttpUrlRepository("https://example.com/charts");

        builder.AddUpstreamHttpRepository(httpUrl, repo => repo.AddChart(new UpstreamChart { Name = "conflict", VersionPolicy = CreatePolicy() }));

        builder.AddManualRepository(repo => repo.AddChart(new RepositoryIndexChart { Name = "conflict", Versions = [CreateChartVersion("conflict", "1.0.0")] }));

        Assert.Throws<ValidationException>(builder.Build);
    }
}
