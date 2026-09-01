namespace VRepo.Helm.Specification.Models.Repositories.References;

public sealed class LocalFilesystemUrlRepository
{
    private const string FileUriScheme = "file";

    // Relative file URIs (e.g. file://../myrepo) cannot be parsed to a `Uri` object,
    // so the original string representation of the URI is used instead.
    private readonly string _uri;

    public LocalFilesystemUrlRepository(LocalFilesystemUrlRepository value)
    {
        _uri = new(value._uri);
    }

    public LocalFilesystemUrlRepository(string value)
    {
        if (!Matches(value))
        {
            throw new ArgumentException("Value is not a valid local filesystem repository URL.", nameof(value));
        }

        _uri = value;
    }

    public static bool Matches(string uri) => uri.StartsWith($"{FileUriScheme}://", StringComparison.OrdinalIgnoreCase);

    public static bool operator ==(LocalFilesystemUrlRepository? url1, LocalFilesystemUrlRepository? url2)
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

    public static bool operator !=(LocalFilesystemUrlRepository? url1, LocalFilesystemUrlRepository? url2)
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
        if (other is null || other is not LocalFilesystemUrlRepository otherUrl)
        {
            return false;
        }

        return string.Equals(_uri, otherUrl._uri, StringComparison.Ordinal);
    }

    public override int GetHashCode() => _uri.GetHashCode();

    public override string ToString() => _uri;
}
