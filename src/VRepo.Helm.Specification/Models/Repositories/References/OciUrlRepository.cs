namespace VRepo.Helm.Specification.Models.Repositories.References;

public sealed class OciUrlRepository
{
    private const string OciUriScheme = "oci";

    public OciUrlRepository(OciUrlRepository value) : this(value.Uri.ToString())
    {
    }

    public OciUrlRepository(string value)
    {
        if (!Matches(value))
        {
            throw new ArgumentException("Value is not a valid OCI repository URL.", nameof(value));
        }

        Uri = NormalizeUri(new(value, UriKind.Absolute));
    }

    public Uri Uri { get; }

    /// <summary>
    /// Gets the OCI repository.
    /// </summary>
    /// <remarks>
    /// Note that the OCI repository concept is different to the <i>Helm</i> repository. The Helm repository
    /// value is used for Helm operations, while the OCI repository value is used for OCI registry operations.
    /// The two values differ in format and usage.
    /// </remarks>
    public string OciRepository => $"{Uri.Authority}{Uri.AbsolutePath}";

    public static bool Matches(string uri) =>
        Uri.TryCreate(uri, UriKind.Absolute, out var parsedUri) &&
        string.Equals(parsedUri.Scheme, OciUriScheme, StringComparison.Ordinal);

    private static Uri NormalizeUri(Uri uri)
    {
        // Remove query and fragment
        var url = uri.GetLeftPart(UriPartial.Path).TrimEnd('/');

        return new Uri(url, UriKind.Absolute);
    }

    public static bool operator ==(OciUrlRepository? url1, OciUrlRepository? url2)
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

    public static bool operator !=(OciUrlRepository? url1, OciUrlRepository? url2)
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
        if (other is null || other is not OciUrlRepository otherUrl)
        {
            return false;
        }

        var urlString = Uri.ToString();
        var otherUrlString = otherUrl.Uri.ToString();

        return string.Equals(urlString, otherUrlString, StringComparison.Ordinal);
    }

    public override int GetHashCode() => Uri.GetHashCode();

    public override string ToString() => Uri.ToString();
}
