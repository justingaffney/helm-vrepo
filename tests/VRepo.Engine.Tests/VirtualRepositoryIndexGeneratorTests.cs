using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using VRepo.Engine.Models;
using VRepo.Engine.Models.Policies;
using VRepo.Engine.Services;
using VRepo.Helm.Sources.Http.Exceptions;
using VRepo.Helm.Sources.Http.Services;
using VRepo.Helm.Sources.Oci.Exceptions;
using VRepo.Helm.Sources.Oci.Models;
using VRepo.Helm.Sources.Oci.Services;
using VRepo.Helm.Specification.Models.Charts.Oci;
using VRepo.Helm.Specification.Models.Repositories;
using VRepo.Helm.Specification.Models.Repositories.References;

namespace VRepo.Engine.Tests;

public class VirtualRepositoryIndexGeneratorTests
{
    [Fact]
    public async Task Generate_MergesHttpOciAndManualCharts_AndSortsVersionsDescending()
    {
        var httpRepositoryIndexProvider = Substitute.For<IHttpRepositoryIndexProvider>();
        var ociChartProvider = Substitute.For<IOciChartProvider>();
        var chartVersionPolicyEvaluator = Substitute.For<IChartVersionPolicyEvaluator>();
        chartVersionPolicyEvaluator
            .VersionIsPermitted(Arg.Any<ChartVersionPolicy>(), Arg.Any<string>())
            .Returns(true);

        var httpRepository = new HttpUrlRepository("https://example.com/charts");
        var ociRepository = new OciUrlRepository("oci://example.com/charts");

        httpRepositoryIndexProvider
            .Get(httpRepository, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new RepositoryIndex
            {
                Entries = new Dictionary<string, IReadOnlyList<RepositoryChartMetadata>>
                {
                    ["alpha"] =
                    [
                        CreateChartVersion("alpha", "0.9.0", "v2", ["https://example.com/charts/alpha-0.9.0.tgz"]),
                        CreateChartVersion("alpha", "1.2.0", "v2", ["https://example.com/charts/alpha-1.2.0.tgz"]),
                        CreateChartVersion("alpha", "9.9.9", "not-valid", ["https://example.com/charts/alpha-9.9.9.tgz"])
                    ]
                },
                GenerationTime = TimeProvider.System.GetUtcNow()
            }));

        ociChartProvider
            .GetVersionTags(Arg.Any<OciChartReference>(), Arg.Any<OciRepositoryClientOptions?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IEnumerable<string>>(["0.5.0", "1.0.0"]));
        ociChartProvider
            .GetChart(Arg.Any<OciChartVersionReference>(), Arg.Any<OciRepositoryClientOptions?>(), Arg.Any<CancellationToken>())
            .Returns(ci => Task.FromResult(CreateChartVersion(
                ci.Arg<OciChartVersionReference>().Chart.Name,
                ci.Arg<OciChartVersionReference>().Version,
                "v2",
                [$"oci://example.com/charts/{ci.Arg<OciChartVersionReference>().Chart.Name}:{ci.Arg<OciChartVersionReference>().Version}"])));

        var repository = new VirtualRepository.Builder();
        repository.AddUpstreamHttpRepository(httpRepository, builder => builder.AddChart(new()
        {
            Name = "alpha",
            VersionPolicy = CreatePolicy()
        }));
        repository.AddUpstreamOciRepository(ociRepository, builder => builder.AddChart(new()
        {
            Name = "beta",
            VersionPolicy = CreatePolicy()
        }));
        repository.AddManualRepository(builder => builder.AddChart(new()
        {
            Name = "gamma",
            Versions = [CreateChartVersion("gamma", "2.0.0", "v2", ["https://example.com/charts/gamma-2.0.0.tgz"])]
        }));

        var generator = new VirtualRepositoryIndexGenerator(
            NullLogger<VirtualRepositoryIndexGenerator>.Instance,
            TimeProvider.System,
            httpRepositoryIndexProvider,
            ociChartProvider,
            chartVersionPolicyEvaluator);

        var index = await generator.Generate(repository.Build(), TestContext.Current.CancellationToken);

        Assert.Equal(["alpha", "beta", "gamma"], index.Entries.Keys.ToArray());
        Assert.Equal(["1.2.0", "0.9.0"], index.Entries["alpha"].Select(version => version.Version).ToArray());
        Assert.Equal(["1.0.0", "0.5.0"], index.Entries["beta"].Select(version => version.Version).ToArray());
        Assert.Equal(["2.0.0"], index.Entries["gamma"].Select(version => version.Version).ToArray());
        Assert.NotEqual(default, index.GenerationTime);
    }

    [Fact]
    public async Task Generate_SkipsFailedRepositoryRetrievals_AndMissingCharts()
    {
        var httpRepositoryIndexProvider = Substitute.For<IHttpRepositoryIndexProvider>();
        var ociChartProvider = Substitute.For<IOciChartProvider>();
        var chartVersionPolicyEvaluator = Substitute.For<IChartVersionPolicyEvaluator>();
        chartVersionPolicyEvaluator
            .VersionIsPermitted(Arg.Any<ChartVersionPolicy>(), Arg.Any<string>())
            .Returns(true);

        var failingHttpRepository = new HttpUrlRepository("https://example.com/failing-charts");
        var missingChartHttpRepository = new HttpUrlRepository("https://example.com/missing-charts");
        var ociRepository = new OciUrlRepository("oci://example.com/failing-oci");

        httpRepositoryIndexProvider
            .Get(failingHttpRepository, Arg.Any<CancellationToken>())
            .Returns(Task.FromException<RepositoryIndex>(new RepositoryIndexRetrievalException("HTTP failure")));
        httpRepositoryIndexProvider
            .Get(missingChartHttpRepository, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new RepositoryIndex
            {
                Entries = new Dictionary<string, IReadOnlyList<RepositoryChartMetadata>>
                {
                    ["other-chart"] = [CreateChartVersion("other-chart", "1.0.0", "v2", ["https://example.com/other-chart.tgz"])]
                },
                GenerationTime = TimeProvider.System.GetUtcNow()
            }));

        ociChartProvider
            .GetVersionTags(Arg.Any<OciChartReference>(), Arg.Any<OciRepositoryClientOptions?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<IEnumerable<string>>(new OciChartRetrievalException("OCI failure")));

        var repository = new VirtualRepository.Builder();
        repository.AddUpstreamHttpRepository(failingHttpRepository, builder => builder.AddChart(new()
        {
            Name = "ignored",
            VersionPolicy = CreatePolicy()
        }));
        repository.AddUpstreamHttpRepository(missingChartHttpRepository, builder => builder.AddChart(new()
        {
            Name = "missing",
            VersionPolicy = CreatePolicy()
        }));
        repository.AddUpstreamOciRepository(ociRepository, builder => builder.AddChart(new()
        {
            Name = "oci-chart",
            VersionPolicy = CreatePolicy()
        }));
        repository.AddManualRepository(builder => builder.AddChart(new()
        {
            Name = "manual-chart",
            Versions = [CreateChartVersion("manual-chart", "3.0.0", "v2", ["https://example.com/manual-chart-3.0.0.tgz"])]
        }));

        var generator = new VirtualRepositoryIndexGenerator(
            NullLogger<VirtualRepositoryIndexGenerator>.Instance,
            TimeProvider.System,
            httpRepositoryIndexProvider,
            ociChartProvider,
            chartVersionPolicyEvaluator);

        var index = await generator.Generate(repository.Build(), TestContext.Current.CancellationToken);

        Assert.Equal(["manual-chart"], index.Entries.Keys.ToArray());
    }

    [Fact]
    public async Task Generate_ConvertsRelativeHttpChartUrlsToAbsoluteUrls()
    {
        var httpRepositoryIndexProvider = Substitute.For<IHttpRepositoryIndexProvider>();
        var chartVersionPolicyEvaluator = Substitute.For<IChartVersionPolicyEvaluator>();
        chartVersionPolicyEvaluator
            .VersionIsPermitted(Arg.Any<ChartVersionPolicy>(), Arg.Any<string>())
            .Returns(true);

        var httpRepository = new HttpUrlRepository("https://example.com/charts/");

        httpRepositoryIndexProvider
            .Get(httpRepository, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new RepositoryIndex
            {
                Entries = new Dictionary<string, IReadOnlyList<RepositoryChartMetadata>>
                {
                    ["alpha"] =
                    [
                        CreateChartVersion("alpha", "1.0.0", "v2", ["alpha-1.0.0.tgz", "/mirror/alpha-1.0.0.tgz", "https://cdn.example.com/alpha-1.0.0.tgz"])
                    ]
                },
                GenerationTime = TimeProvider.System.GetUtcNow()
            }));

        var repository = new VirtualRepository.Builder();
        repository.AddUpstreamHttpRepository(httpRepository, builder => builder.AddChart(new()
        {
            Name = "alpha",
            VersionPolicy = CreatePolicy()
        }));

        var generator = new VirtualRepositoryIndexGenerator(
            NullLogger<VirtualRepositoryIndexGenerator>.Instance,
            TimeProvider.System,
            httpRepositoryIndexProvider,
            Substitute.For<IOciChartProvider>(),
            chartVersionPolicyEvaluator);

        var index = await generator.Generate(repository.Build(), TestContext.Current.CancellationToken);

        var chartVersion = Assert.Single(index.Entries["alpha"]);
        Assert.Equal(
            [
                "https://example.com/charts/alpha-1.0.0.tgz",
                "https://example.com/charts/mirror/alpha-1.0.0.tgz",
                "https://cdn.example.com/alpha-1.0.0.tgz"
            ],
            chartVersion.Urls);
    }

    [Fact]
    public async Task Generate_ExcludesVersionsRejectedByPolicy()
    {
        var httpRepositoryIndexProvider = Substitute.For<IHttpRepositoryIndexProvider>();
        var ociChartProvider = Substitute.For<IOciChartProvider>();
        var chartVersionPolicyEvaluator = Substitute.For<IChartVersionPolicyEvaluator>();
        chartVersionPolicyEvaluator
            .VersionIsPermitted(Arg.Any<ChartVersionPolicy>(), Arg.Any<string>())
            .Returns(ci => ci.Arg<string>() != "2.0.0-rc.1");

        var httpRepository = new HttpUrlRepository("https://example.com/charts");
        var ociRepository = new OciUrlRepository("oci://example.com/charts");

        httpRepositoryIndexProvider
            .Get(httpRepository, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new RepositoryIndex
            {
                Entries = new Dictionary<string, IReadOnlyList<RepositoryChartMetadata>>
                {
                    ["alpha"] =
                    [
                        CreateChartVersion("alpha", "1.0.0", "v2", ["https://example.com/charts/alpha-1.0.0.tgz"]),
                        CreateChartVersion("alpha", "2.0.0-rc.1", "v2", ["https://example.com/charts/alpha-2.0.0-rc.1.tgz"])
                    ]
                },
                GenerationTime = TimeProvider.System.GetUtcNow()
            }));

        ociChartProvider
            .GetVersionTags(Arg.Any<OciChartReference>(), Arg.Any<OciRepositoryClientOptions?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IEnumerable<string>>(["1.0.0", "2.0.0-rc.1"]));
        ociChartProvider
            .GetChart(Arg.Any<OciChartVersionReference>(), Arg.Any<OciRepositoryClientOptions?>(), Arg.Any<CancellationToken>())
            .Returns(ci => Task.FromResult(CreateChartVersion("beta", ci.Arg<OciChartVersionReference>().Version, "v2", ["oci://example.com/charts/beta"])));

        var repository = new VirtualRepository.Builder();
        repository.AddUpstreamHttpRepository(httpRepository, builder => builder.AddChart(new()
        {
            Name = "alpha",
            VersionPolicy = CreatePolicy()
        }));
        repository.AddUpstreamOciRepository(ociRepository, builder => builder.AddChart(new()
        {
            Name = "beta",
            VersionPolicy = CreatePolicy()
        }));

        var generator = new VirtualRepositoryIndexGenerator(
            NullLogger<VirtualRepositoryIndexGenerator>.Instance,
            TimeProvider.System,
            httpRepositoryIndexProvider,
            ociChartProvider,
            chartVersionPolicyEvaluator);

        var index = await generator.Generate(repository.Build(), TestContext.Current.CancellationToken);

        Assert.Equal(["1.0.0"], index.Entries["alpha"].Select(version => version.Version).ToArray());
        Assert.Equal(["1.0.0"], index.Entries["beta"].Select(version => version.Version).ToArray());

        // Excluded OCI versions should never be fetched
        await ociChartProvider
            .DidNotReceive()
            .GetChart(Arg.Is<OciChartVersionReference>(reference => reference.Version == "2.0.0-rc.1"), Arg.Any<OciRepositoryClientOptions?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Generate_SkipsFailedOciChartVersionRetrievals_AndKeepsRemainingVersions()
    {
        var ociChartProvider = Substitute.For<IOciChartProvider>();
        var chartVersionPolicyEvaluator = Substitute.For<IChartVersionPolicyEvaluator>();
        chartVersionPolicyEvaluator
            .VersionIsPermitted(Arg.Any<ChartVersionPolicy>(), Arg.Any<string>())
            .Returns(true);

        ociChartProvider
            .GetVersionTags(Arg.Any<OciChartReference>(), Arg.Any<OciRepositoryClientOptions?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IEnumerable<string>>(["1.0.0", "2.0.0", "3.0.0"]));
        ociChartProvider
            .GetChart(Arg.Any<OciChartVersionReference>(), Arg.Any<OciRepositoryClientOptions?>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.Arg<OciChartVersionReference>().Version == "2.0.0"
                ? Task.FromException<RepositoryChartMetadata>(new OciChartRetrievalException("Manifest failure"))
                : Task.FromResult(CreateChartVersion("beta", ci.Arg<OciChartVersionReference>().Version, "v2", ["oci://example.com/charts/beta"])));

        var repository = new VirtualRepository.Builder();
        repository.AddUpstreamOciRepository(new OciUrlRepository("oci://example.com/charts"), builder => builder.AddChart(new()
        {
            Name = "beta",
            VersionPolicy = CreatePolicy()
        }));

        var generator = new VirtualRepositoryIndexGenerator(
            NullLogger<VirtualRepositoryIndexGenerator>.Instance,
            TimeProvider.System,
            Substitute.For<IHttpRepositoryIndexProvider>(),
            ociChartProvider,
            chartVersionPolicyEvaluator);

        var index = await generator.Generate(repository.Build(), TestContext.Current.CancellationToken);

        Assert.Equal(["3.0.0", "1.0.0"], index.Entries["beta"].Select(version => version.Version).ToArray());
    }

    [Fact]
    public async Task Generate_PassesOciRepositoryClientOptionsToProvider()
    {
        var ociChartProvider = Substitute.For<IOciChartProvider>();
        var chartVersionPolicyEvaluator = Substitute.For<IChartVersionPolicyEvaluator>();
        chartVersionPolicyEvaluator
            .VersionIsPermitted(Arg.Any<ChartVersionPolicy>(), Arg.Any<string>())
            .Returns(true);

        ociChartProvider
            .GetVersionTags(Arg.Any<OciChartReference>(), Arg.Any<OciRepositoryClientOptions?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IEnumerable<string>>(["1.0.0"]));
        ociChartProvider
            .GetChart(Arg.Any<OciChartVersionReference>(), Arg.Any<OciRepositoryClientOptions?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(CreateChartVersion("beta", "1.0.0", "v2", ["oci://example.com/charts/beta"])));

        var repository = new VirtualRepository.Builder();
        repository.AddUpstreamOciRepository(new OciUrlRepository("oci://example.com/charts"), builder =>
        {
            builder.SetAuthenticationRealmHostname("auth.example.com");
            builder.AddChart(new()
            {
                Name = "beta",
                VersionPolicy = CreatePolicy()
            });
        });

        var generator = new VirtualRepositoryIndexGenerator(
            NullLogger<VirtualRepositoryIndexGenerator>.Instance,
            TimeProvider.System,
            Substitute.For<IHttpRepositoryIndexProvider>(),
            ociChartProvider,
            chartVersionPolicyEvaluator);

        await generator.Generate(repository.Build(), TestContext.Current.CancellationToken);

        await ociChartProvider
            .Received(1)
            .GetVersionTags(Arg.Any<OciChartReference>(), Arg.Is<OciRepositoryClientOptions?>(options => options!.AuthenticationRealmHostname == "auth.example.com"), Arg.Any<CancellationToken>());
        await ociChartProvider
            .Received(1)
            .GetChart(Arg.Any<OciChartVersionReference>(), Arg.Is<OciRepositoryClientOptions?>(options => options!.AuthenticationRealmHostname == "auth.example.com"), Arg.Any<CancellationToken>());
    }

    private static ChartVersionPolicy CreatePolicy() => new()
    {
        ExclusionRules = new ChartVersionExclusionRules
        {
            ExcludeInvalidSemVer = false,
            ExcludePrereleaseVersions = false,
            VersionExclusionPatterns = []
        }
    };

    private static RepositoryChartMetadata CreateChartVersion(string name, string version, string apiVersion, string[] urls) =>
        new()
        {
            ApiVersion = apiVersion,
            Name = name,
            Version = version,
            Urls = urls
        };
}
