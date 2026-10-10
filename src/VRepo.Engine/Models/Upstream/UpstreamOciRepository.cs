using System.ComponentModel.DataAnnotations;
using VRepo.Helm.Sources.Oci.Models;
using VRepo.Helm.Specification.Models.Repositories.References;

namespace VRepo.Engine.Models.Upstream;

public sealed class UpstreamOciRepository
{
    private UpstreamOciRepository(OciUrlRepository url, OciRepositoryClientOptions clientOptions, IReadOnlyList<UpstreamChart> charts)
    {
        Url = url;
        ClientOptions = clientOptions;
        Charts = charts;
    }

    public OciUrlRepository Url { get; }

    public OciRepositoryClientOptions ClientOptions { get; }

    public IReadOnlyList<UpstreamChart> Charts { get; }

    public sealed class Builder(OciUrlRepository url) : IBuilder<UpstreamOciRepository>
    {
        private readonly List<UpstreamChart> charts = new();
        private OciRepositoryClientOptions clientOptions = new();

        public void AddChart(UpstreamChart chart) => charts.Add(chart);

        public void SetAuthenticationRealmHostname(string authenticationRealmHostname)
        {
            ArgumentException.ThrowIfNullOrEmpty(authenticationRealmHostname);

            clientOptions = new OciRepositoryClientOptions
            {
                AuthenticationRealmHostname = authenticationRealmHostname
            };
        }

        public UpstreamOciRepository Build()
        {
            var duplicateCharts = charts
                .GroupBy(chart => chart.Name, StringComparer.Ordinal)
                .ToDictionary(charts => charts.Key, charts => charts.Count())
                .Where(charts => charts.Value > 1)
                .Select(charts => charts.Key)
                .ToArray();
            if (duplicateCharts.Length > 0)
            {
                throw new ValidationException($"The following charts have been added for repository \"{url}\" more than once: {string.Join(", ", duplicateCharts.Select(chart => $"\"{chart}\""))}.");
            }

            return new(url, clientOptions, charts.ToArray());
        }
    }
}
