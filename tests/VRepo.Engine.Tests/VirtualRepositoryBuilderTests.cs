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
    public void UpstreamOciRepositoryBuilder_DefaultsToNoAuthenticationRealmHostname()
    {
        var builder = new UpstreamOciRepository.Builder(new OciUrlRepository("oci://example.com/charts"));

        var repository = builder.Build();

        Assert.Null(repository.ClientOptions.AuthenticationRealmHostname);
    }

    [Fact]
    public void UpstreamOciRepositoryBuilder_SetAuthenticationRealmHostname_SetsClientOptions()
    {
        var builder = new UpstreamOciRepository.Builder(new OciUrlRepository("oci://example.com/charts"));

        builder.SetAuthenticationRealmHostname("auth.example.com");

        Assert.Equal("auth.example.com", builder.Build().ClientOptions.AuthenticationRealmHostname);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void UpstreamOciRepositoryBuilder_SetAuthenticationRealmHostname_RejectsNullOrEmpty(string? hostname)
    {
        var builder = new UpstreamOciRepository.Builder(new OciUrlRepository("oci://example.com/charts"));

        Assert.ThrowsAny<ArgumentException>(() => builder.SetAuthenticationRealmHostname(hostname!));
    }

    [Fact]
    public void RepositoryIndexContentsBuilder_DuplicateCharts_ThrowsValidationException()
    {
        var builder = new RepositoryIndexContents.Builder();

        builder.AddChart(new RepositoryIndexChart { Name = "dup", Versions = [CreateChartVersion("dup", "1.0.0")] });
        builder.AddChart(new RepositoryIndexChart { Name = "dup", Versions = [CreateChartVersion("dup", "2.0.0")] });

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
    public void VirtualRepositoryBuilder_DuplicateUpstreamOciRepositories_ThrowsValidationException()
    {
        var builder = new VirtualRepository.Builder();

        var ociUrl = new OciUrlRepository("oci://example.com/charts");

        builder.AddUpstreamOciRepository(ociUrl, repo => repo.AddChart(new UpstreamChart { Name = "a", VersionPolicy = CreatePolicy() }));
        builder.AddUpstreamOciRepository(ociUrl, repo => repo.AddChart(new UpstreamChart { Name = "b", VersionPolicy = CreatePolicy() }));

        Assert.Throws<ValidationException>(builder.Build);
    }

    [Fact]
    public void VirtualRepositoryBuilder_ChartNameConflictsBetweenHttpRepositories_ThrowsValidationException()
    {
        var builder = new VirtualRepository.Builder();

        builder.AddUpstreamHttpRepository(new HttpUrlRepository("https://example.com/charts"), repo => repo.AddChart(new UpstreamChart { Name = "conflict", VersionPolicy = CreatePolicy() }));
        builder.AddUpstreamHttpRepository(new HttpUrlRepository("https://example.com/other-charts"), repo => repo.AddChart(new UpstreamChart { Name = "conflict", VersionPolicy = CreatePolicy() }));

        Assert.Throws<ValidationException>(builder.Build);
    }

    [Fact]
    public void VirtualRepositoryBuilder_ChartNameConflictsBetweenHttpAndOci_ThrowsValidationException()
    {
        var builder = new VirtualRepository.Builder();

        builder.AddUpstreamHttpRepository(new HttpUrlRepository("https://example.com/charts"), repo => repo.AddChart(new UpstreamChart { Name = "conflict", VersionPolicy = CreatePolicy() }));
        builder.AddUpstreamOciRepository(new OciUrlRepository("oci://example.com/charts"), repo => repo.AddChart(new UpstreamChart { Name = "conflict", VersionPolicy = CreatePolicy() }));

        Assert.Throws<ValidationException>(builder.Build);
    }

    [Fact]
    public void VirtualRepositoryBuilder_ChartNameConflictsBetweenOciRepositories_ThrowsValidationException()
    {
        var builder = new VirtualRepository.Builder();

        builder.AddUpstreamOciRepository(new OciUrlRepository("oci://example.com/charts"), repo => repo.AddChart(new UpstreamChart { Name = "conflict", VersionPolicy = CreatePolicy() }));
        builder.AddUpstreamOciRepository(new OciUrlRepository("oci://example.com/other-charts"), repo => repo.AddChart(new UpstreamChart { Name = "conflict", VersionPolicy = CreatePolicy() }));

        Assert.Throws<ValidationException>(builder.Build);
    }

    [Fact]
    public void VirtualRepositoryBuilder_AddManualRepositoryTwice_ThrowsInvalidOperationException()
    {
        var builder = new VirtualRepository.Builder();

        builder.AddManualRepository(_ => { });

        Assert.Throws<InvalidOperationException>(() => builder.AddManualRepository(_ => { }));
    }

    [Fact]
    public void VirtualRepositoryBuilder_WithoutManualRepository_BuildsEmptyManualRepository()
    {
        var builder = new VirtualRepository.Builder();

        var repository = builder.Build();

        Assert.Empty(repository.ManualRepository.Charts);
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
