using Microsoft.Extensions.Logging;
using VRepo.Common.Versioning;
using VRepo.Engine.Models;
using VRepo.Engine.Models.Policies;
using VRepo.Engine.Models.Upstream;
using VRepo.Helm.Sources.Http.Exceptions;
using VRepo.Helm.Sources.Http.Services;
using VRepo.Helm.Sources.Oci.Exceptions;
using VRepo.Helm.Sources.Oci.Services;
using VRepo.Helm.Specification.Models.Charts;
using VRepo.Helm.Specification.Models.Charts.Oci;
using VRepo.Helm.Specification.Models.Repositories;

namespace VRepo.Engine.Services;

public interface IVirtualRepositoryIndexGenerator
{
    Task<RepositoryIndex> Generate(VirtualRepository repository, CancellationToken cancellationToken = default);
}

internal sealed class VirtualRepositoryIndexGenerator(
        ILogger<VirtualRepositoryIndexGenerator> logger,
        TimeProvider timeProvider,
        IHttpRepositoryIndexProvider httpRepositoryIndexProvider,
        IOciChartProvider ociChartProvider,
        IChartVersionPolicyEvaluator chartVersionPolicyEvaluator)
    : IVirtualRepositoryIndexGenerator
{
    public async Task<RepositoryIndex> Generate(VirtualRepository repository, CancellationToken cancellationToken = default)
    {
        var allCharts = new List<RepositoryIndexChart>();

        foreach (var httpRepository in repository.UpstreamRepositories.Http
            .OrderBy(repository => repository.Url.ToString(), StringComparer.Ordinal))
        {
            var contents = await GetUpstreamHttpRepositoryIndexContents(httpRepository, cancellationToken);
            allCharts.AddRange(contents.Charts);
        }

        foreach (var ociRepository in repository.UpstreamRepositories.Oci
            .OrderBy(repository => repository.Url.ToString(), StringComparer.Ordinal))
        {
            var contents = await GetUpstreamOciRepositoryIndexContents(ociRepository, cancellationToken);
            allCharts.AddRange(contents.Charts);
        }

        allCharts.AddRange(repository.ManualRepository.Charts);

        return new()
        {
            Entries = allCharts
                .OrderBy(chart => chart.Name, StringComparer.Ordinal)
                .ToDictionary(chart => chart.Name, chart => chart.Versions, StringComparer.Ordinal),
            GenerationTime = timeProvider.GetUtcNow()
        };
    }

    private async Task<RepositoryIndexContents> GetUpstreamHttpRepositoryIndexContents(UpstreamHttpRepository repository, CancellationToken cancellationToken)
    {
        var repositoryIndexContentsBuilder = new RepositoryIndexContents.Builder();

        RepositoryIndex fullUpstreamRepositoryIndex;
        try
        {
            logger.LogInformation("Retrieving index YAML file for repository (URL: \"{RepositoryUrl}\").", repository.Url);
            fullUpstreamRepositoryIndex = await httpRepositoryIndexProvider.Get(repository.Url, cancellationToken);
        }
        catch (RepositoryIndexRetrievalException exception)
        {
            logger.LogWarning("Retrieving index YAML file for repository (URL: \"{RepositoryUrl}\") failed with error: \"{ErrorMessage}\". It will be skipped.", repository.Url, exception.Message);
            return repositoryIndexContentsBuilder.Build();
        }

        foreach (var chart in repository.Charts)
        {
            if (!fullUpstreamRepositoryIndex.Entries.TryGetValue(chart.Name, out var chartVersions))
            {
                logger.LogWarning("Chart \"{ChartName}\" cannot be found in repository (URL: \"{RepositoryUrl}\") index. It will be skipped.", chart.Name, repository.Url);
                continue;
            }

            // Filter out versions with invalid `apiVersion` values
            chartVersions = chartVersions
                .Where((chartVersion, index) =>
                {
                    if (!ChartMetadata.ValidApiVersions.Any(validApiVersion => string.Equals(validApiVersion, chartVersion.ApiVersion, StringComparison.OrdinalIgnoreCase)))
                    {
                        logger.LogWarning("Chart \"{ChartName}\" version[{ChartVersionListIndex}] (version: \"{ChartVersion}\") in repository (URL: \"{RepositoryUrl}\") index has an invalid \"apiVersion\" value, value is \"{ChartVersionApiVersion}\" but only \"{ValidChartApiVersions}\" is supported. It will be skipped.", chart.Name, index, chartVersion.Version, repository.Url, chartVersion.ApiVersion, string.Join(", ", ChartMetadata.ValidApiVersions));
                        return false;
                    }

                    return true;
                })
                .ToArray();

            chartVersions = FilterVersions(chartVersions, version => version.Version, chart.VersionPolicy).ToArray();

            if (chartVersions.Count > 0)
            {
                // Sort versions
                chartVersions = SortVersions(chartVersions, chart => chart.Version).ToList();

                repositoryIndexContentsBuilder.AddChart(new()
                {
                    Name = chart.Name,
                    Versions = chartVersions
                });
            }
        }

        var repositoryIndexContents = repositoryIndexContentsBuilder.Build();

        // Convert relative URLs to absolute, otherwise URLs will
        // be broken when relative to the virtual repository's URL
        foreach (var chartVersion in repositoryIndexContents.Charts.SelectMany(chart => chart.Versions))
        {
            for (var i = 0; i < chartVersion.Urls.Length; i++)
            {
                var url = chartVersion.Urls[i];
                if (Uri.TryCreate(url, UriKind.Absolute, out _))
                {
                    // URL is already absolute
                    continue;
                }

                // Most likely a relative URL, relative to the original repository URL.
                // Convert to an absolute URL using the repository URL as the base.
                const char pathSeparator = '/';
                var repositoryUrl = repository.Url.Uri.ToString().TrimEnd(pathSeparator);
                var relativeUrl = url.TrimStart(pathSeparator);
                var absoluteUrl = $"{repositoryUrl}{pathSeparator}{relativeUrl}";

                chartVersion.Urls[i] = absoluteUrl;
            }
        }

        return repositoryIndexContents;
    }

    private async Task<RepositoryIndexContents> GetUpstreamOciRepositoryIndexContents(UpstreamOciRepository repository, CancellationToken cancellationToken)
    {
        var repositoryIndexContentsBuilder = new RepositoryIndexContents.Builder();

        foreach (var chart in repository.Charts)
        {
            var chartReference = new OciChartReference(repository.Url, chart.Name);

            IEnumerable<string> chartVersionTags;
            try
            {
                logger.LogInformation("Retrieving versions for OCI chart \"{ChartName}\" in repository (URL: \"{RepositoryUrl}\").", chart.Name, repository.Url);
                chartVersionTags = await ociChartProvider.GetVersionTags(chartReference, repository.ClientOptions, cancellationToken);
            }
            catch (OciChartRetrievalException exception)
            {
                logger.LogWarning("Retrieving versions for OCI chart \"{ChartName}\" in repository (URL: \"{RepositoryUrl}\") failed with error: \"{ErrorMessage}\". It will be skipped.", chart.Name, repository.Url, exception.Message);
                continue;
            }

            chartVersionTags = FilterVersions(chartVersionTags, version => version, chart.VersionPolicy);

            var chartVersions = new List<RepositoryChartMetadata>();
            foreach (var chartVersionTag in chartVersionTags)
            {
                RepositoryChartMetadata chartVersion;
                try
                {
                    logger.LogInformation("Retrieving OCI chart \"{ChartName}\" version \"{ChartVersion}\" in repository (URL: \"{RepositoryUrl}\").", chart.Name, chartVersionTag, repository.Url);
                    chartVersion = await ociChartProvider.GetChart(new OciChartVersionReference(chartReference, chartVersionTag), repository.ClientOptions, cancellationToken);
                }
                catch (OciChartRetrievalException exception)
                {
                    logger.LogWarning("Chart \"{ChartName}\" version \"{ChartVersion}\" retrieval from repository (URL: \"{RepositoryUrl}\") failed with error: \"{ErrorMessage}\".", chart.Name, chartVersionTag, repository.Url, exception.Message);
                    continue;
                }

                chartVersions.Add(chartVersion);
            }

            if (chartVersions.Count > 0)
            {
                // Sort versions
                chartVersions = SortVersions(chartVersions, chart => chart.Version).ToList();

                repositoryIndexContentsBuilder.AddChart(new()
                {
                    Name = chart.Name,
                    Versions = chartVersions
                });
            }
        }

        var repositoryIndexContents = repositoryIndexContentsBuilder.Build();

        return repositoryIndexContents;
    }

    private IEnumerable<TChartVersion> FilterVersions<TChartVersion>(
        IEnumerable<TChartVersion> chartVersions,
        Func<TChartVersion, string> getRawVersion,
        ChartVersionPolicy policy) =>
        chartVersions.Where(chartVersion => chartVersionPolicyEvaluator.VersionIsPermitted(policy, getRawVersion(chartVersion)));

    private static IEnumerable<TChartVersion> SortVersions<TChartVersion>(
        IEnumerable<TChartVersion> chartVersions,
        Func<TChartVersion, string> getRawVersion) =>
        SemVerHelper.SortVersionedElements(chartVersions, getRawVersion, ascending: false);
}
