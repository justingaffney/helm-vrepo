using VRepo.Configuration.Exceptions;

namespace VRepo.Configuration.Versions;

public readonly union ApiVersion(V1Alpha1.ApiVersion)
{
    public const string YamlMemberName = "apiVersion";

    public static readonly IReadOnlyList<string> AllValues = [
        V1Alpha1.ApiVersion.Value
    ];

    public static ApiVersion Parse(string value) => value.ToLowerInvariant() switch
    {
        V1Alpha1.ApiVersion.Value => new V1Alpha1.ApiVersion(),
        _ => throw new InvalidApiVersionException(value.ToLowerInvariant())
    };

    public override readonly string ToString() => this switch
    {
        V1Alpha1.ApiVersion => V1Alpha1.ApiVersion.Value,
        _ => throw new InvalidOperationException($"Unknown `{nameof(ApiVersion)}` value.")
    };
}
