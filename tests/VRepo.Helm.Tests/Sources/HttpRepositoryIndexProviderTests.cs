using System.Net;
using VRepo.Helm.Sources.Http.Exceptions;
using VRepo.Helm.Sources.Http.Services;
using VRepo.Helm.Specification.Models.Repositories.References;

namespace VRepo.Helm.Tests.Sources;

public class HttpRepositoryIndexProviderTests
{
    private const string IndexYaml = """
        apiVersion: v1
        entries:
          alpha:
            - apiVersion: v2
              name: alpha
              version: "1.0.0"
              urls:
                - alpha-1.0.0.tgz
        generated: 2026-01-01T00:00:00Z
        """;

    [Fact]
    public async Task Get_RequestsIndexFile_AndDeserializesResponse()
    {
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(IndexYaml)
        });
        var provider = new HttpRepositoryIndexProvider(new HttpClient(handler));

        var index = await provider.Get(new HttpUrlRepository("https://example.com/charts/"), TestContext.Current.CancellationToken);

        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Get, request.Method);
        Assert.Equal(new Uri("https://example.com/charts/index.yaml"), request.RequestUri);

        var chart = Assert.Single(index.Entries["alpha"]);
        Assert.Equal("1.0.0", chart.Version);
        Assert.Equal(["alpha-1.0.0.tgz"], chart.Urls);
    }

    [Theory]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.InternalServerError)]
    public async Task Get_ThrowsRepositoryIndexRetrievalException_ForUnsuccessfulStatusCode(HttpStatusCode statusCode)
    {
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(statusCode));
        var provider = new HttpRepositoryIndexProvider(new HttpClient(handler));

        var exception = await Assert.ThrowsAsync<RepositoryIndexRetrievalException>(() =>
            provider.Get(new HttpUrlRepository("https://example.com/charts"), TestContext.Current.CancellationToken));

        Assert.Contains("https://example.com/charts/index.yaml", exception.Message, StringComparison.Ordinal);
        Assert.Contains(((int)statusCode).ToString(), exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Get_ThrowsRepositoryIndexRetrievalException_ForInvalidYaml()
    {
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("entries: [unterminated")
        });
        var provider = new HttpRepositoryIndexProvider(new HttpClient(handler));

        var exception = await Assert.ThrowsAsync<RepositoryIndexRetrievalException>(() =>
            provider.Get(new HttpUrlRepository("https://example.com/charts"), TestContext.Current.CancellationToken));

        Assert.NotNull(exception.InnerException);
    }
}
