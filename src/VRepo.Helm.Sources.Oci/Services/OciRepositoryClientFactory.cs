using System.Collections.Frozen;
using OrasProject.Oras.Registry;
using OrasProject.Oras.Registry.Remote;
using OrasProject.Oras.Registry.Remote.Auth;
using VRepo.Helm.Sources.Oci.Models;
using VRepo.Helm.Specification.Models.Charts.Oci;

namespace VRepo.Helm.Sources.Oci.Services;

internal interface IOciRepositoryClientFactory
{
    Repository Create(OciChartReference chart, OciRepositoryClientOptions? clientOptions = null);
}

internal sealed class OciRepositoryClientFactory(HttpClient httpClient) : IOciRepositoryClientFactory
{
    public Repository Create(OciChartReference chart, OciRepositoryClientOptions? clientOptions = null)
    {
        var innerClient = new Client(httpClient)
        {
            RealmValidator = new DefaultRealmValidator
            {
                TrustedRealmHosts = !string.IsNullOrWhiteSpace(clientOptions?.AuthenticationRealmHostname)
                    ? FrozenSet.Create(clientOptions.AuthenticationRealmHostname)
                    : []
            }
        };

        var repositoryOptions = new RepositoryOptions
        {
            Reference = Reference.Parse($"{chart.Repository.OciRepository}/{chart.Name}"),
            Client = innerClient
        };

        return new Repository(repositoryOptions);
    }
}
