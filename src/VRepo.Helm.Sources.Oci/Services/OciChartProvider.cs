using System.Security.Authentication;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using OrasProject.Oras.Content;
using OrasProject.Oras.Oci;
using VRepo.Helm.Sources.Oci.Exceptions;
using VRepo.Helm.Specification.Models.Charts;
using VRepo.Helm.Specification.Models.Charts.Oci;
using VRepo.Helm.Specification.Models.Repositories;

namespace VRepo.Helm.Sources.Oci.Services;

public interface IOciChartProvider
{
    Task<IEnumerable<string>> GetVersionTags(OciChartReference reference, CancellationToken cancellationToken = default);

    Task<RepositoryChartMetadata> GetChart(OciChartVersionReference reference, CancellationToken cancellationToken = default);
}

internal sealed class OciChartProvider(
        ILogger<OciChartProvider> logger,
        IOciRepositoryClientFactory clientFactory)
    : IOciChartProvider
{
    public async Task<IEnumerable<string>> GetVersionTags(OciChartReference reference, CancellationToken cancellationToken = default)
    {
        var client = clientFactory.Create(reference);

        try
        {
            var versions = new List<string>();
            await foreach (var tag in client.ListTagsAsync(cancellationToken: cancellationToken))
            {
                versions.Add(tag);
            }

            return versions;
        }
        catch (AuthenticationException exception)
        {
            throw new OciChartRetrievalException($"Version tag retrieval failed with authentication error: \"{exception.Message}\".", exception);
        }
    }

    public async Task<RepositoryChartMetadata> GetChart(OciChartVersionReference reference, CancellationToken cancellationToken = default)
    {
        var chartUrl = $"{reference.Chart.Repository}/{reference.Chart.Name}:{reference.Version}";

        var client = clientFactory.Create(reference.Chart);

        Descriptor manifestDescriptor;
        Stream manifestStream;
        try
        {
            var fetchedManifest = await client.FetchAsync(reference.Version.ToString(), cancellationToken);
            manifestDescriptor = fetchedManifest.Descriptor;
            manifestStream = fetchedManifest.Stream;
        }
        catch (AuthenticationException exception)
        {
            throw new OciChartRetrievalException($"Manifest retrieval failed with authentication error: \"{exception.Message}\".", exception);
        }

        if (!string.Equals(manifestDescriptor.MediaType, MediaType.ImageManifest, StringComparison.OrdinalIgnoreCase))
        {
            throw new OciChartRetrievalException($"Manifest descriptor media type value \"{manifestDescriptor.MediaType}\" is not \"{MediaType.ImageManifest}\".");
        }

        string manifestJson;
        using (manifestStream)
        {
            var manifestBytes = await manifestStream.ReadAllAsync(manifestDescriptor, cancellationToken);

            manifestJson = Encoding.UTF8.GetString(manifestBytes);
        }

        Manifest manifest;
        try
        {
            logger.LogDebug("Chart (URL: {ChartUrl} image manifest JSON: \"{ManifestJson}\"", chartUrl, manifestJson);
            manifest = JsonSerializer.Deserialize<Manifest>(manifestJson) ?? throw new JsonException("Manifest could not be deserialized to `Manifest`");
        }
        catch (JsonException exception)
        {
            throw new OciChartRetrievalException($"Image manifest JSON deserialization failed with error: \"{exception.Message}\".", exception);
        }

        if (!string.Equals(manifest.Config.MediaType, OciChartManifest.MediaTypes.HelmConfigV1Json, StringComparison.OrdinalIgnoreCase))
        {
            throw new OciChartRetrievalException($"Manifest \"config.mediaType\" value \"{manifest.Config.MediaType}\" is not \"{OciChartManifest.MediaTypes.HelmConfigV1Json}\"");
        }

        if (manifest.Annotations is null)
        {
            return new RepositoryChartMetadata
            {
                ApiVersion = ChartMetadata.LatestStableApiVersion,
                Name = reference.Chart.Name,
                Version = reference.Version,
                Urls = [chartUrl],
                Digest = manifest.Layers.First().Digest // TODO There are a few digest values in the manifest, is this the right one?
            };
        }

        return new RepositoryChartMetadata
        {
            ApiVersion = ChartMetadata.LatestStableApiVersion,
            Name = manifest.Annotations.TryGetValue(OciChartManifest.Annotations.Name, out var chartName) && !string.IsNullOrWhiteSpace(chartName)
                ? chartName
                : reference.Chart.Name,
            Version = reference.Version,
            CreationTime = manifest.Annotations.TryGetValue(OciChartManifest.Annotations.CreationTime, out var creationTimeStr) && DateTimeOffset.TryParse(creationTimeStr, out var creationTime)
                ? creationTime
                : null,
            Description = manifest.Annotations.TryGetValue(OciChartManifest.Annotations.Description, out var description) && !string.IsNullOrWhiteSpace(description)
                ? description
                : null,
            Urls = [chartUrl],
            HomeUrl = manifest.Annotations.TryGetValue(OciChartManifest.Annotations.HomeUrl, out var homeUrl) && !string.IsNullOrWhiteSpace(homeUrl)
                ? homeUrl
                : null,
            SourceUrls = manifest.Annotations.TryGetValue(OciChartManifest.Annotations.SourceUrl, out var sourceUrl) && !string.IsNullOrWhiteSpace(sourceUrl)
                ? [sourceUrl]
                : null,
            Maintainers = manifest.Annotations.TryGetValue(OciChartManifest.Annotations.Maintainers, out var maintainers) && !string.IsNullOrWhiteSpace(maintainers)
                ? maintainers
                    .Split(',')
                    .Select(maintainer => new ChartMetadata.Maintainer
                    {
                        Name = maintainer.Trim()
                    })
                    .ToArray()
                : null,
            Digest = manifest.Layers.First().Digest // TODO There are a few digest values in the manifest, is this the right one?
        };
    }
}
