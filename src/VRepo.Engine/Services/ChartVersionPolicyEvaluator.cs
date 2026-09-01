using Semver;
using VRepo.Common.Versioning;
using VRepo.Engine.Models.Policies;

namespace VRepo.Engine.Services;

public interface IChartVersionPolicyEvaluator
{
    bool VersionIsPermitted(ChartVersionPolicy policy, string version);
}

internal sealed class ChartVersionPolicyEvaluator : IChartVersionPolicyEvaluator
{
    public bool VersionIsPermitted(ChartVersionPolicy policy, string version) => !VersionIsExcluded(policy.ExclusionRules, version);

    private static bool VersionIsExcluded(ChartVersionExclusionRules rules, string versionStr)
    {
        var isSemVer = SemVersion.TryParse(SemVerHelper.Normalize(versionStr), out var version);

        if (rules.ExcludeInvalidSemVer)
        {
            if (!isSemVer)
            {
                // Version is not valid SemVer version 2
                return true;
            }
        }

        if (rules.ExcludePrereleaseVersions)
        {
            if (!isSemVer)
            {
                // Version is not valid SemVer version 2

                // Version does not pass the filter, since it is not
                // possible to detect whether it is a prerelease version
                return true;
            }

            if (version!.IsPrerelease)
            {
                // Version is identified as prerelease
                return true;
            }
        }

        if (rules.VersionExclusionPatterns is not null)
        {
            if (rules.VersionExclusionPatterns.Any(pattern => pattern.IsMatch(versionStr)))
            {
                // Version matched an exclusion pattern
                return true;
            }
        }

        return false;
    }
}
