using Json.Schema.Generation;
using VRepo.Common.Versioning;
using VRepo.Configuration.Serialization.Yaml.Converters;
using YamlDotNet.Serialization;

namespace VRepo.Configuration.Versions.V1Alpha1.Models;

public sealed class ChartVersionPolicyConfig
{
    public static ChartVersionPolicyConfig Blank => new();

    [YamlMember(Alias = "exclude")]
    [Description("The rules for determining whether a chart version should be excluded.")]
    public ExclusionRulesConfig? ExclusionRules { get; init; }

    public sealed class ExclusionRulesConfig
    {
        [YamlMember(Alias = "invalidSemVer")]
        [Description("Whether chart versions that are not valid SemVer (version 2) are excluded.")]
        public bool? ExcludeInvalidSemVer { get; init; }

        [YamlMember(Alias = "prerelease")]
        [Description("Whether chart versions that have a SemVer prerelease tag (e.g. the \"-alpha\" part of \"1.2.3-alpha\") are excluded.")]
        public bool? ExcludePrereleaseVersions { get; init; }

        [YamlMember(Alias = "versions")]
        [YamlConverter(typeof(VersionPatternConverter))]
        [Description("The list of patterns that match versions that are excluded. Patterns are evaluated sequentially when determining whether to exclude a chart's version.\n\nCan be a single version (e.g. \"1.2.3\"), a SemVer range (e.g. \"<2.0.0\", \"1.0.0 - 2.3.4\", \"^1.2.3\"), or a regular expression (e.g. \"^(?!chartversionprefix-).*$\").\n\nRegular expression version patterns are useful for patterns that cannot be represented using a SemVer range, handling chart versions that are not valid SemVer (e.g. OCI charts where the image tag can be anything), or any other scenario that requires more flexibility to match versions.")]
        public IReadOnlyList<VersionPattern>? VersionExclusionPatterns { get; init; }
    }

    /// <summary>
    /// Merge the specified policy with this policy.
    /// </summary>
    /// <param name="overridingPolicy">The policy to merge over the top of this policy.</param>
    /// <returns>A new policy representing the combination of this policy and the specified policy, with the specified policy overriding this policy's rules when set.</returns>
    public ChartVersionPolicyConfig Merge(ChartVersionPolicyConfig overridingPolicy) =>
        new()
        {
            ExclusionRules = overridingPolicy.ExclusionRules is not null || ExclusionRules is not null
                ? new()
                {
                    ExcludeInvalidSemVer = overridingPolicy.ExclusionRules?.ExcludeInvalidSemVer ?? ExclusionRules?.ExcludeInvalidSemVer,
                    ExcludePrereleaseVersions = overridingPolicy.ExclusionRules?.ExcludePrereleaseVersions ?? ExclusionRules?.ExcludePrereleaseVersions,
                    VersionExclusionPatterns = [
                        .. overridingPolicy.ExclusionRules?.VersionExclusionPatterns ?? [],
                        .. ExclusionRules?.VersionExclusionPatterns ?? []
                    ]
                }
                : null
        };
}
