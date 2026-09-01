using VRepo.Common.Versioning;

namespace VRepo.Engine.Models.Policies;

public sealed class ChartVersionExclusionRules
{
    public required bool ExcludeInvalidSemVer { get; init; }

    public required bool ExcludePrereleaseVersions { get; init; }

    public required IReadOnlyList<VersionPattern> VersionExclusionPatterns { get; init; }
}
