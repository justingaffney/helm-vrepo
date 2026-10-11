using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging.Abstractions;
using VRepo.Helm.Sources.Oci.Exceptions;
using VRepo.Helm.Sources.Oci.Models;
using VRepo.Helm.Sources.Oci.Services;
using VRepo.Helm.Specification.Models.Charts;
using VRepo.Helm.Specification.Models.Charts.Oci;
using VRepo.Helm.Specification.Models.Repositories.References;

namespace VRepo.Helm.Tests.Sources;

public class OciChartProviderTests
{
    private const string AuthenticationRealmHost = "auth.unrelated.test";
    private const string ImageManifestMediaType = "application/vnd.oci.image.manifest.v1+json";
    private const string LayerDigest = "sha256:1111111111111111111111111111111111111111111111111111111111111111";

    [Fact]
    public async Task GetVersionTags_ReturnsTagsFromRegistry()
    {
        var registry = new FakeRegistry
        {
            Tags = ["1.0.0", "1.1.0", "2.0.0-beta.1"]
        };
        var provider = CreateProvider(registry);

        var tags = await provider.GetVersionTags(registry.ChartReference, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(["1.0.0", "1.1.0", "2.0.0-beta.1"], tags);
        Assert.Contains(registry.Requests, request => request.RequestUri!.AbsolutePath == "/v2/charts/alpha/tags/list");
    }

    [Fact]
    public async Task GetVersionTags_ThrowsOciChartRetrievalException_WhenAuthenticationRealmIsNotTrusted()
    {
        var registry = new FakeRegistry
        {
            Tags = ["1.0.0"],
            RequireToken = true
        };
        var provider = CreateProvider(registry);

        var exception = await Assert.ThrowsAsync<OciChartRetrievalException>(() =>
            provider.GetVersionTags(registry.ChartReference, cancellationToken: TestContext.Current.CancellationToken));

        Assert.Contains("authentication", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(registry.Requests, request => request.RequestUri!.Host == AuthenticationRealmHost);
    }

    [Fact]
    public async Task GetVersionTags_AuthenticatesWithTrustedAuthenticationRealmHostname()
    {
        var registry = new FakeRegistry
        {
            Tags = ["1.0.0"],
            RequireToken = true
        };
        var provider = CreateProvider(registry);
        var clientOptions = new OciRepositoryClientOptions
        {
            AuthenticationRealmHostname = AuthenticationRealmHost
        };

        var tags = await provider.GetVersionTags(registry.ChartReference, clientOptions, TestContext.Current.CancellationToken);

        Assert.Equal(["1.0.0"], tags);
        Assert.Contains(registry.Requests, request => request.RequestUri!.Host == AuthenticationRealmHost);
    }

    [Fact]
    public async Task GetChart_MapsManifestAnnotationsToChartMetadata()
    {
        var registry = new FakeRegistry
        {
            Manifest = CreateManifest(new Dictionary<string, string>
            {
                [OciChartManifest.Annotations.Name] = "alpha-title",
                [OciChartManifest.Annotations.CreationTime] = "2026-01-02T03:04:05Z",
                [OciChartManifest.Annotations.Description] = "Alpha chart",
                [OciChartManifest.Annotations.HomeUrl] = "https://example.com",
                [OciChartManifest.Annotations.SourceUrl] = "https://github.com/example/alpha",
                [OciChartManifest.Annotations.Maintainers] = "Jane, John"
            })
        };
        var provider = CreateProvider(registry);

        var chart = await provider.GetChart(new OciChartVersionReference(registry.ChartReference, "1.0.0"), cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(ChartMetadata.LatestStableApiVersion, chart.ApiVersion);
        Assert.Equal("alpha-title", chart.Name);
        Assert.Equal("1.0.0", chart.Version);
        Assert.Equal(new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero), chart.CreationTime);
        Assert.Equal("Alpha chart", chart.Description);
        Assert.Equal("https://example.com", chart.HomeUrl);
        Assert.Equal(["https://github.com/example/alpha"], chart.SourceUrls);
        Assert.Equal(["Jane", "John"], chart.Maintainers!.Select(maintainer => maintainer.Name));
        Assert.Equal([$"oci://{registry.Host}/charts/alpha:1.0.0"], chart.Urls);
        Assert.Equal(LayerDigest, chart.Digest);
        Assert.Contains(registry.Requests, request => request.RequestUri!.AbsolutePath == "/v2/charts/alpha/manifests/1.0.0");
    }

    [Fact]
    public async Task GetChart_FallsBackToReferenceValues_WhenAnnotationsAreBlankOrInvalid()
    {
        var registry = new FakeRegistry
        {
            Manifest = CreateManifest(new Dictionary<string, string>
            {
                [OciChartManifest.Annotations.Name] = " ",
                [OciChartManifest.Annotations.CreationTime] = "not-a-date",
                [OciChartManifest.Annotations.Description] = "",
                [OciChartManifest.Annotations.Maintainers] = " "
            })
        };
        var provider = CreateProvider(registry);

        var chart = await provider.GetChart(new OciChartVersionReference(registry.ChartReference, "1.0.0"), cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal("alpha", chart.Name);
        Assert.Null(chart.CreationTime);
        Assert.Null(chart.Description);
        Assert.Null(chart.HomeUrl);
        Assert.Null(chart.SourceUrls);
        Assert.Null(chart.Maintainers);
        Assert.Equal(LayerDigest, chart.Digest);
    }

    [Fact]
    public async Task GetChart_UsesReferenceValues_WhenManifestHasNoAnnotations()
    {
        var registry = new FakeRegistry
        {
            Manifest = CreateManifest(annotations: null)
        };
        var provider = CreateProvider(registry);

        var chart = await provider.GetChart(new OciChartVersionReference(registry.ChartReference, "1.0.0"), cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal("alpha", chart.Name);
        Assert.Equal("1.0.0", chart.Version);
        Assert.Equal([$"oci://{registry.Host}/charts/alpha:1.0.0"], chart.Urls);
        Assert.Equal(LayerDigest, chart.Digest);
        Assert.Null(chart.Description);
    }

    [Fact]
    public async Task GetChart_ThrowsOciChartRetrievalException_WhenConfigMediaTypeIsNotHelm()
    {
        var registry = new FakeRegistry
        {
            Manifest = CreateManifest(annotations: null, configMediaType: "application/vnd.oci.image.config.v1+json")
        };
        var provider = CreateProvider(registry);

        var exception = await Assert.ThrowsAsync<OciChartRetrievalException>(() =>
            provider.GetChart(new OciChartVersionReference(registry.ChartReference, "1.0.0"), cancellationToken: TestContext.Current.CancellationToken));

        Assert.Contains("config.mediaType", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetChart_ThrowsOciChartRetrievalException_WhenManifestIsNotAnImageManifest()
    {
        var registry = new FakeRegistry
        {
            Manifest = CreateManifest(annotations: null),
            ManifestMediaType = "application/vnd.oci.image.index.v1+json"
        };
        var provider = CreateProvider(registry);

        var exception = await Assert.ThrowsAsync<OciChartRetrievalException>(() =>
            provider.GetChart(new OciChartVersionReference(registry.ChartReference, "1.0.0"), cancellationToken: TestContext.Current.CancellationToken));

        Assert.Contains(ImageManifestMediaType, exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetChart_ThrowsOciChartRetrievalException_WhenManifestJsonIsInvalid()
    {
        var registry = new FakeRegistry
        {
            Manifest = "{ not json"
        };
        var provider = CreateProvider(registry);

        var exception = await Assert.ThrowsAsync<OciChartRetrievalException>(() =>
            provider.GetChart(new OciChartVersionReference(registry.ChartReference, "1.0.0"), cancellationToken: TestContext.Current.CancellationToken));

        Assert.IsAssignableFrom<JsonException>(exception.InnerException);
    }

    [Fact]
    public async Task GetChart_ThrowsOciChartRetrievalException_WhenAuthenticationRealmIsNotTrusted()
    {
        var registry = new FakeRegistry
        {
            Manifest = CreateManifest(annotations: null),
            RequireToken = true
        };
        var provider = CreateProvider(registry);

        await Assert.ThrowsAsync<OciChartRetrievalException>(() =>
            provider.GetChart(new OciChartVersionReference(registry.ChartReference, "1.0.0"), cancellationToken: TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task GetChart_AuthenticatesWithTrustedAuthenticationRealmHostname()
    {
        var registry = new FakeRegistry
        {
            Manifest = CreateManifest(annotations: null),
            RequireToken = true
        };
        var provider = CreateProvider(registry);
        var clientOptions = new OciRepositoryClientOptions
        {
            AuthenticationRealmHostname = AuthenticationRealmHost
        };

        var chart = await provider.GetChart(new OciChartVersionReference(registry.ChartReference, "1.0.0"), clientOptions, TestContext.Current.CancellationToken);

        Assert.Equal("alpha", chart.Name);
    }

    private static OciChartProvider CreateProvider(FakeRegistry registry) =>
        new(
            NullLogger<OciChartProvider>.Instance,
            new OciRepositoryClientFactory(new HttpClient(registry.Handler)));

    private static string CreateManifest(IReadOnlyDictionary<string, string>? annotations, string configMediaType = OciChartManifest.MediaTypes.HelmConfigV1Json)
    {
        var manifest = new JsonObject
        {
            ["schemaVersion"] = 2,
            ["mediaType"] = ImageManifestMediaType,
            ["config"] = new JsonObject
            {
                ["mediaType"] = configMediaType,
                ["digest"] = "sha256:2222222222222222222222222222222222222222222222222222222222222222",
                ["size"] = 100
            },
            ["layers"] = new JsonArray
            {
                new JsonObject
                {
                    ["mediaType"] = "application/vnd.cncf.helm.chart.content.v1.tar+gzip",
                    ["digest"] = LayerDigest,
                    ["size"] = 1000
                }
            }
        };

        if (annotations is not null)
        {
            var annotationsObject = new JsonObject();
            foreach (var (key, value) in annotations)
            {
                annotationsObject[key] = value;
            }

            manifest["annotations"] = annotationsObject;
        }

        return manifest.ToJsonString();
    }

    private sealed class FakeRegistry
    {
        private const string Token = "test-token";

        public FakeRegistry()
        {
            // ORAS caches bearer tokens per registry host across client instances, so each
            // fake registry uses a unique host to keep tests from sharing authentication state
            Host = $"registry-{Guid.NewGuid():N}.example.com";
            ChartReference = new(new OciUrlRepository($"oci://{Host}/charts"), "alpha");
            Handler = new StubHttpMessageHandler(Respond);
        }

        public string Host { get; }

        public OciChartReference ChartReference { get; }

        public StubHttpMessageHandler Handler { get; }

        public List<HttpRequestMessage> Requests => Handler.Requests;

        public IReadOnlyList<string> Tags { get; init; } = [];

        public string Manifest { get; init; } = string.Empty;

        public string ManifestMediaType { get; init; } = ImageManifestMediaType;

        public bool RequireToken { get; init; }

        private HttpResponseMessage Respond(HttpRequestMessage request)
        {
            if (request.RequestUri!.Host == AuthenticationRealmHost)
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(JsonSerializer.Serialize(new { token = Token, access_token = Token }), Encoding.UTF8, "application/json")
                };
            }

            if (request.RequestUri.Host != Host)
            {
                return new HttpResponseMessage(HttpStatusCode.NotFound);
            }

            if (RequireToken && request.Headers.Authorization is not { Scheme: "Bearer", Parameter: Token })
            {
                var response = new HttpResponseMessage(HttpStatusCode.Unauthorized);
                response.Headers.WwwAuthenticate.Add(new AuthenticationHeaderValue(
                    "Bearer",
                    $"realm=\"https://{AuthenticationRealmHost}/token\",service=\"{Host}\",scope=\"repository:charts/alpha:pull\""));
                return response;
            }

            var path = request.RequestUri.AbsolutePath;
            if (path == "/v2/charts/alpha/tags/list")
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(JsonSerializer.Serialize(new { name = "charts/alpha", tags = Tags }), Encoding.UTF8, "application/json")
                };
            }

            if (path.StartsWith("/v2/charts/alpha/manifests/", StringComparison.Ordinal))
            {
                var manifestBytes = Encoding.UTF8.GetBytes(Manifest);
                var digest = $"sha256:{Convert.ToHexStringLower(SHA256.HashData(manifestBytes))}";

                var response = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new ByteArrayContent(manifestBytes)
                };
                response.Content.Headers.ContentType = new MediaTypeHeaderValue(ManifestMediaType);
                response.Content.Headers.ContentLength = manifestBytes.Length;
                response.Headers.Add("Docker-Content-Digest", digest);
                return response;
            }

            return new HttpResponseMessage(HttpStatusCode.NotFound);
        }
    }
}
