namespace VRepo.Helm.Specification.Models.Repositories.References;

public sealed class AliasRepository
{
    private const string Prefix1 = "@";
    private const string Prefix2 = "alias:";

    private readonly string _original;

    public AliasRepository(AliasRepository value) : this(value._original)
    {
    }

    public AliasRepository(string value)
    {
        if (!Matches(value))
        {
            throw new ArgumentException("Value is not a valid repository alias.", nameof(value));
        }

        _original = value;

        Value = ParseAliasValue(value);
    }

    public string Value { get; }

    public static bool Matches(string alias) =>
        !string.IsNullOrWhiteSpace(alias) &&
        (
            alias.StartsWith(Prefix1, StringComparison.Ordinal) ||
            alias.StartsWith(Prefix2, StringComparison.Ordinal)
        );

    private static string ParseAliasValue(string alias)
    {
        if (alias.StartsWith(Prefix1, StringComparison.Ordinal))
        {
            return alias[Prefix1.Length..];
        }

        if (alias.StartsWith(Prefix2, StringComparison.Ordinal))
        {
            return alias[Prefix2.Length..];
        }

        throw new ArgumentException("Value is not a valid repository alias.", nameof(alias));
    }

    public static bool operator ==(AliasRepository? url1, AliasRepository? url2)
    {
        if (ReferenceEquals(url1, url2))
        {
            return true;
        }

        if (url1 is null || url2 is null)
        {
            return false;
        }

        return url1.Equals(url2);
    }

    public static bool operator !=(AliasRepository? url1, AliasRepository? url2)
    {
        if (ReferenceEquals(url1, url2))
        {
            return false;
        }

        if (url1 is null || url2 is null)
        {
            return true;
        }

        return !url1.Equals(url2);
    }

    public override bool Equals(object? other)
    {
        if (other is null || other is not AliasRepository otherAlias)
        {
            return false;
        }

        return string.Equals(_original, otherAlias._original, StringComparison.Ordinal);
    }

    public override int GetHashCode() => _original.GetHashCode();

    public override string ToString() => _original;
}
