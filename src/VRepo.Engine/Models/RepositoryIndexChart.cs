using VRepo.Helm.Specification.Models.Repositories;

namespace VRepo.Engine.Models;

public sealed class RepositoryIndexChart
{
    public required string Name { get; init; }

    public required IReadOnlyList<RepositoryChartMetadata> Versions { get; init; }
}
