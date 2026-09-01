using VRepo.Common.Serialization.Exceptions;
using VRepo.Helm.Sources.Http.Exceptions;
using VRepo.Helm.Specification.Models.Repositories;
using VRepo.Helm.Specification.Models.Repositories.References;

namespace VRepo.Helm.Sources.Http.Services;

public interface IHttpRepositoryIndexProvider
{
    Task<RepositoryIndex> Get(HttpUrlRepository repository, CancellationToken cancellationToken = default);
}

internal sealed class HttpRepositoryIndexProvider(HttpClient client) : IHttpRepositoryIndexProvider
{
    public async Task<RepositoryIndex> Get(HttpUrlRepository repository, CancellationToken cancellationToken = default)
    {
        var indexFileUri = repository.IndexFile;

        var response = await client.GetAsync(indexFileUri, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new RepositoryIndexRetrievalException($"GET request to \"{indexFileUri}\" failed with HTTP status code {(int)response.StatusCode}");
        }

        var repositoryIndexYaml = await response.Content.ReadAsStringAsync(cancellationToken);

        try
        {
            return RepositoryIndex.Deserialize(repositoryIndexYaml);
        }
        catch (SerializationException exception)
        {
            throw new RepositoryIndexRetrievalException($"Failed to deserialize YAML retrieved from \"{indexFileUri}\" to repository index with error: \"{exception.GetBaseException().Message}\".", exception);
        }
    }
}
