using System.ComponentModel.DataAnnotations;
using VRepo.Helm.Specification.Models.Repositories.References;

namespace VRepo.Engine.Models.Upstream;

public sealed class UpstreamHttpRepository
{
    private UpstreamHttpRepository(HttpUrlRepository url, IReadOnlyList<UpstreamChart> charts)
    {
        Url = url;
        Charts = charts;
    }

    public HttpUrlRepository Url { get; }

    public IReadOnlyList<UpstreamChart> Charts { get; }

    public sealed class Builder(HttpUrlRepository url) : IBuilder<UpstreamHttpRepository>
    {
        private readonly List<UpstreamChart> charts = new();

        public void AddChart(UpstreamChart chart) => charts.Add(chart);

        public UpstreamHttpRepository Build()
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

            return new(url, charts.ToArray());
        }
    }
}
