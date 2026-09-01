using OrasProject.Oras.Registry;
using OrasProject.Oras.Registry.Remote;
using OrasProject.Oras.Registry.Remote.Auth;
using VRepo.Helm.Specification.Models.Charts.Oci;

namespace VRepo.Helm.Sources.Oci.Services;

internal interface IOciRepositoryClientFactory
{
    Repository Create(OciChartReference chart);
}

internal sealed class OciRepositoryClientFactory(HttpClient httpClient) : IOciRepositoryClientFactory
{
    public Repository Create(OciChartReference chart)
    {
        var repositoryOptions = new RepositoryOptions
        {
            Reference = Reference.Parse($"{chart.Repository.OciRepository}/{chart.Name}"),
            Client = new Client(httpClient)
        };

        return new Repository(repositoryOptions);
    }
}
