using System.ComponentModel.DataAnnotations;
using VRepo.Helm.Specification.Models.Repositories.References;

namespace VRepo.Engine.Models.Upstream;

public sealed class UpstreamOciRepository
{
    private UpstreamOciRepository(OciUrlRepository url, IReadOnlyList<UpstreamChart> charts)
    {
        Url = url;
        Charts = charts;
    }

    public OciUrlRepository Url { get; }

    public IReadOnlyList<UpstreamChart> Charts { get; }

    public sealed class Builder(OciUrlRepository url) : IBuilder<UpstreamOciRepository>
    {
        private readonly List<UpstreamChart> charts = new();

        public void AddChart(UpstreamChart chart) => charts.Add(chart);

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

            return new(url, charts.ToArray());
        }
    }
}
