namespace VRepo.Engine.Models.Policies;

public sealed class ChartVersionPolicy
{
    public required ChartVersionExclusionRules ExclusionRules { get; init; }
}
