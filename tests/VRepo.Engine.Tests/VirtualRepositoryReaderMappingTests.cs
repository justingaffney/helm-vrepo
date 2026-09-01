using Microsoft.Extensions.Options;
using NSubstitute;
using VRepo.Configuration.Versions.V1Alpha1.Models;
using VRepo.Engine.Services;

namespace VRepo.Engine.Tests;

public class VirtualRepositoryReaderMappingTests
{
    [Fact]
    public async Task Read_IncludesManualRepositoryEntries()
    {
        var validator = Substitute.For<IValidateOptions<VirtualRepositoryConfig>>();
        validator
            .Validate(Arg.Any<string?>(), Arg.Any<VirtualRepositoryConfig>())
            .Returns(ValidateOptionsResult.Success);

        var reader = new VirtualRepositoryReader(validator);

        var yaml = @"apiVersion: v1alpha1
manualRepository:
  entries:
    example-manual:
      - apiVersion: v2
        name: example-manual
        version: ""1.0.0""
        urls:
          - https://example.com/example-manual-1.0.0.tgz
";

        var repository = await reader.Read(new StringReader(yaml), TestContext.Current.CancellationToken);

        // Manual repository should be present and contain the expected chart
        Assert.NotNull(repository.ManualRepository);
        Assert.Contains(repository.ManualRepository.Charts, chart => chart.Name == "example-manual");

        // No upstream repositories should be present
        Assert.Empty(repository.UpstreamRepositories.Http);
        Assert.Empty(repository.UpstreamRepositories.Oci);
    }

    [Fact]
    public async Task Read_MergesVersionPolicy_GlobalRepoAndChartLevelOverrides()
    {
        var validator = Substitute.For<IValidateOptions<VirtualRepositoryConfig>>();
        validator
            .Validate(Arg.Any<string?>(), Arg.Any<VirtualRepositoryConfig>())
            .Returns(ValidateOptionsResult.Success);

        var reader = new VirtualRepositoryReader(validator);

        var yaml = @"apiVersion: v1alpha1
upstream:
  versionPolicy:
    exclude:
      invalidSemVer: true
      prerelease: false
  repositories:
    - url: https://example.com/charts
      versionPolicy:
        exclude:
          invalidSemVer: false
          prerelease: false
      charts:
        chart-a:
          # no chart-level overrides, should inherit repo-level
        chart-b:
          versionPolicy:
            exclude:
              prerelease: true
";

        var repository = await reader.Read(new StringReader(yaml), TestContext.Current.CancellationToken);

        var httpRepo = repository.UpstreamRepositories.Http.FirstOrDefault();
        Assert.NotNull(httpRepo);

        var chartA = httpRepo!.Charts.FirstOrDefault(c => c.Name == "chart-a");
        var chartB = httpRepo.Charts.FirstOrDefault(c => c.Name == "chart-b");

        Assert.NotNull(chartA);
        Assert.NotNull(chartB);

        // Expected precedence:
        // global: invalidSemVer=true, prerelease=false
        // repo:   invalidSemVer=false, prerelease=false (overrides invalidSemVer)
        // chart:  prerelease=true (overrides prerelease)

        // chart-a inherits repo-level values
        Assert.False(chartA!.VersionPolicy.ExclusionRules.ExcludeInvalidSemVer);
        Assert.False(chartA.VersionPolicy.ExclusionRules.ExcludePrereleaseVersions);

        // chart-b applies chart-level prerelease override, repo-level invalidSemVer remains
        Assert.False(chartB!.VersionPolicy.ExclusionRules.ExcludeInvalidSemVer);
        Assert.True(chartB.VersionPolicy.ExclusionRules.ExcludePrereleaseVersions);
    }
}
