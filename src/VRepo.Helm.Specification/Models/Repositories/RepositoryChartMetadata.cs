using Json.Schema.Generation;
using VRepo.Common.Serialization.Yaml.Converters;
using VRepo.Helm.Specification.Models.Charts;
using YamlDotNet.Serialization;

namespace VRepo.Helm.Specification.Models.Repositories;

public class RepositoryChartMetadata : ChartMetadata
{
    [YamlMember(Alias = "urls", Order = 101)]
    [Description("A list of URLs containing the locations of the chart package.")]
    public required string[] Urls { get; init; }

    [YamlMember(Alias = "digest", Order = 102)]
    [Description("The checksum of the chart package file.")]
    public string? Digest { get; init; }

    [YamlMember(Alias = "created", Order = 103)]
    [YamlConverter(typeof(DateTimeOffsetIsoWithoutMillisecondsConverter))]
    [Description("The ISO 8601 formatted timestamp of when the chart version was packaged or indexed.")]
    public DateTimeOffset? CreationTime { get; init; }
}
