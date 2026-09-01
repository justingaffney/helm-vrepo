namespace VRepo.Engine.Models.Upstream;

public sealed class UpstreamRepositories
{
    public required IReadOnlyList<UpstreamHttpRepository> Http { get; init; }

    public required IReadOnlyList<UpstreamOciRepository> Oci { get; init; }
}
