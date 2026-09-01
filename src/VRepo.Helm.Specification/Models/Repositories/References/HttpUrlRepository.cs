namespace VRepo.Helm.Specification.Models.Repositories.References;

public sealed class HttpUrlRepository
{
    private const string HttpUriScheme = "http";
    private const string HttpsUriScheme = "https";

    public const string IndexFileName = "index.yaml";

    public HttpUrlRepository(HttpUrlRepository value) : this(value.Uri.ToString())
    {
    }

    public HttpUrlRepository(string value)
    {
        if (!Matches(value))
        {
            throw new ArgumentException("Value is not a valid HTTP repository URL.", nameof(value));
        }

        Uri = NormalizeUri(new(value, UriKind.Absolute));
    }

    public Uri Uri { get; }

    public Uri IndexFile => new($"{Uri.ToString().TrimEnd('/')}/{IndexFileName}", UriKind.Absolute);

    public static bool Matches(string uri) =>
        Uri.TryCreate(uri, UriKind.Absolute, out var parsedUri) &&
        (
            string.Equals(parsedUri.Scheme, HttpUriScheme, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(parsedUri.Scheme, HttpsUriScheme, StringComparison.OrdinalIgnoreCase)
        );

    private static Uri NormalizeUri(Uri uri)
    {
        // Remove query and fragment
        var url = uri.GetLeftPart(UriPartial.Path).TrimEnd('/');

        // Remove index file from the URL if specified
        const string indexFileNameUrlSuffix = $"/{IndexFileName}";
        if (url.EndsWith(indexFileNameUrlSuffix, StringComparison.Ordinal))
        {
            url = url[..^indexFileNameUrlSuffix.Length];
        }

        return new Uri(url, UriKind.Absolute);
    }

    public static bool operator ==(HttpUrlRepository? url1, HttpUrlRepository? url2)
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

    public static bool operator !=(HttpUrlRepository? url1, HttpUrlRepository? url2)
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
        if (other is null || other is not HttpUrlRepository otherUrl)
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
