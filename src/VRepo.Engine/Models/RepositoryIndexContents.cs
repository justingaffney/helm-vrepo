using System.ComponentModel.DataAnnotations;

namespace VRepo.Engine.Models;

public sealed class RepositoryIndexContents
{
    private RepositoryIndexContents(IReadOnlyList<RepositoryIndexChart> charts)
    {
        Charts = charts;
    }

    public IReadOnlyList<RepositoryIndexChart> Charts { get; }

    public sealed class Builder : IBuilder<RepositoryIndexContents>
    {
        private readonly List<RepositoryIndexChart> charts = new();

        public void AddChart(RepositoryIndexChart chart) => charts.Add(chart);

        public RepositoryIndexContents Build()
        {
            var duplicateCharts = charts
                .GroupBy(chart => chart.Name, StringComparer.Ordinal)
                .ToDictionary(charts => charts.Key, charts => charts.Count())
                .Where(charts => charts.Value > 1)
                .Select(charts => charts.Key)
                .ToArray();
            if (duplicateCharts.Length > 0)
            {
                throw new ValidationException($"The following charts have been added for the repository more than once: {string.Join(", ", duplicateCharts.Select(chart => $"\"{chart}\""))}.");
            }

            // Filter out charts with no versions
            var chartsWithVersions = charts.Where(chart => chart.Versions.Count > 0).ToArray();

            return new(chartsWithVersions);
        }
    }
}
