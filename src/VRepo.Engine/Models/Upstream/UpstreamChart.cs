using VRepo.Engine.Models.Policies;

namespace VRepo.Engine.Models.Upstream;

public sealed class UpstreamChart
{
    public required string Name { get; init; }

    public required ChartVersionPolicy VersionPolicy { get; init; }
}
