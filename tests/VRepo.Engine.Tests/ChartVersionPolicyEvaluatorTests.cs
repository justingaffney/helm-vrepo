using VRepo.Common.Versioning;
using VRepo.Engine.Models.Policies;
using VRepo.Engine.Services;

namespace VRepo.Engine.Tests;

public class ChartVersionPolicyEvaluatorTests
{
    [Fact]
    public void VersionIsPermitted_ExcludesInvalidAndPrereleaseVersions()
    {
        var evaluator = new ChartVersionPolicyEvaluator();

        var policy = new ChartVersionPolicy
        {
            ExclusionRules = new ChartVersionExclusionRules
            {
                ExcludeInvalidSemVer = true,
                ExcludePrereleaseVersions = true,
                VersionExclusionPatterns = []
            }
        };

        Assert.False(evaluator.VersionIsPermitted(policy, "1.0.0-alpha.1"));
        Assert.False(evaluator.VersionIsPermitted(policy, "not-a-version"));
        Assert.True(evaluator.VersionIsPermitted(policy, "1.0.0"));
    }

    [Fact]
    public void VersionIsPermitted_PrereleaseExclusion_ExcludesInvalidSemVer_EvenWhenInvalidSemVerIsAllowed()
    {
        var evaluator = new ChartVersionPolicyEvaluator();

        var policy = new ChartVersionPolicy
        {
            ExclusionRules = new ChartVersionExclusionRules
            {
                ExcludeInvalidSemVer = false,
                ExcludePrereleaseVersions = true,
                VersionExclusionPatterns = []
            }
        };

        Assert.False(evaluator.VersionIsPermitted(policy, "not-a-version"));
        Assert.False(evaluator.VersionIsPermitted(policy, "1.0.0-rc.1"));
        Assert.True(evaluator.VersionIsPermitted(policy, "v1.0.0"));
    }

    [Fact]
    public void VersionIsPermitted_PermitsEverything_WhenNoRulesAreEnabled()
    {
        var evaluator = new ChartVersionPolicyEvaluator();

        var policy = new ChartVersionPolicy
        {
            ExclusionRules = new ChartVersionExclusionRules
            {
                ExcludeInvalidSemVer = false,
                ExcludePrereleaseVersions = false,
                VersionExclusionPatterns = []
            }
        };

        Assert.True(evaluator.VersionIsPermitted(policy, "not-a-version"));
        Assert.True(evaluator.VersionIsPermitted(policy, "1.0.0-rc.1"));
        Assert.True(evaluator.VersionIsPermitted(policy, "1.0.0"));
    }

    [Fact]
    public void VersionIsPermitted_RespectsExplicitExclusionPatterns()
    {
        var evaluator = new ChartVersionPolicyEvaluator();

        var policy = new ChartVersionPolicy
        {
            ExclusionRules = new ChartVersionExclusionRules
            {
                ExcludeInvalidSemVer = false,
                ExcludePrereleaseVersions = false,
                VersionExclusionPatterns = [VersionPattern.Parse("<2.0.0")]
            }
        };

        Assert.False(evaluator.VersionIsPermitted(policy, "1.5.0"));
        Assert.True(evaluator.VersionIsPermitted(policy, "2.0.0"));
    }
}
