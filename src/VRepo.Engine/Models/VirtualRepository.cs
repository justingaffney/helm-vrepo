using System.ComponentModel.DataAnnotations;
using VRepo.Engine.Models.Upstream;
using VRepo.Helm.Specification.Models.Repositories.References;

namespace VRepo.Engine.Models;

public sealed class VirtualRepository
{
    private VirtualRepository(UpstreamRepositories upstreamRepositories, RepositoryIndexContents manualRepository)
    {
        UpstreamRepositories = upstreamRepositories;
        ManualRepository = manualRepository;
    }

    public UpstreamRepositories UpstreamRepositories { get; }

    public RepositoryIndexContents ManualRepository { get; }

    public sealed class Builder : IBuilder<VirtualRepository>
    {
        private readonly List<UpstreamHttpRepository> upstreamHttpRepositories = new();
        private readonly List<UpstreamOciRepository> upstreamOciRepositories = new();
        private RepositoryIndexContents? manualRepository;

        public void AddUpstreamHttpRepository(HttpUrlRepository url, Action<UpstreamHttpRepository.Builder> configure)
        {
            var builder = new UpstreamHttpRepository.Builder(url);
            configure(builder);

            var repository = builder.Build();
            upstreamHttpRepositories.Add(repository);
        }

        public void AddUpstreamOciRepository(OciUrlRepository url, Action<UpstreamOciRepository.Builder> configure)
        {
            var builder = new UpstreamOciRepository.Builder(url);
            configure(builder);

            var repository = builder.Build();
            upstreamOciRepositories.Add(repository);
        }

        public void AddManualRepository(Action<RepositoryIndexContents.Builder> configure)
        {
            if (manualRepository is not null)
            {
                throw new InvalidOperationException("Manual repository index has already been added.");
            }

            var builder = new RepositoryIndexContents.Builder();
            configure(builder);

            manualRepository = builder.Build();
        }

        public VirtualRepository Build()
        {
            var upstreamRepositories = new UpstreamRepositories
            {
                Http = upstreamHttpRepositories,
                Oci = upstreamOciRepositories
            };
            var manualRepositoryIndex = manualRepository ?? new RepositoryIndexContents.Builder().Build();

            ValidateNoUpstreamRepositoriesAreDuplicated(upstreamRepositories);
            ValidateChartNamesHaveNoConflicts(upstreamRepositories, manualRepositoryIndex);

            return new(upstreamRepositories, manualRepositoryIndex);
        }

        private static void ValidateNoUpstreamRepositoriesAreDuplicated(UpstreamRepositories upstreamRepositories)
        {
            var duplicateHttpRepositories = upstreamRepositories.Http
                .GroupBy(repository => repository.Url)
                .ToDictionary(repositories => repositories.Key, repositories => repositories.Count())
                .Where(repositories => repositories.Value > 1)
                .Select(repositories => repositories.Key)
                .ToArray();
            if (duplicateHttpRepositories.Length > 0)
            {
                throw new ValidationException($"The following upstream HTTP repositories have been added more than once: {string.Join(", ", duplicateHttpRepositories.Select(repository => $"\"{repository}\""))}.");
            }

            var duplicateOciRepositories = upstreamRepositories.Oci
                .GroupBy(repository => repository.Url)
                .ToDictionary(repositories => repositories.Key, repositories => repositories.Count())
                .Where(repositories => repositories.Value > 1)
                .Select(repositories => repositories.Key)
                .ToArray();
            if (duplicateOciRepositories.Length > 0)
            {
                throw new ValidationException($"The following upstream OCI repositories have been added more than once: {string.Join(", ", duplicateOciRepositories.Select(repository => $"\"{repository}\""))}.");
            }
        }

        private static void ValidateChartNamesHaveNoConflicts(UpstreamRepositories upstreamRepositories, RepositoryIndexContents manualRepository)
        {
            var chartNames = new Dictionary<string, string>(StringComparer.Ordinal);

            foreach (var upstreamRepository in upstreamRepositories.Http)
            {
                var upstreamRepositoryUrl = upstreamRepository.Url.ToString();
                foreach (var chart in upstreamRepository.Charts)
                {
                    if (chartNames.TryGetValue(chart.Name, out var chartSource))
                    {
                        throw new ValidationException($"Chart name \"{chart.Name}\" is already used in source \"{chartSource}\". Chart names can only be used once, so it cannot be added for the upstream HTTP repository \"{upstreamRepositoryUrl}\".");
                    }

                    chartNames.Add(chart.Name, upstreamRepositoryUrl);
                }
            }

            foreach (var upstreamRepository in upstreamRepositories.Oci)
            {
                var upstreamRepositoryUrl = upstreamRepository.Url.ToString();
                foreach (var chart in upstreamRepository.Charts)
                {
                    if (chartNames.TryGetValue(chart.Name, out var chartSource))
                    {
                        throw new ValidationException($"Chart name \"{chart.Name}\" is already used in source \"{chartSource}\". Chart names can only be used once, so it cannot be added for the upstream OCI repository \"{upstreamRepositoryUrl}\".");
                    }

                    chartNames.Add(chart.Name, upstreamRepositoryUrl);
                }
            }

            const string manualRepositorySource = "manual";
            foreach (var chart in manualRepository.Charts)
            {
                if (chartNames.TryGetValue(chart.Name, out var chartSource))
                {
                    throw new ValidationException($"Chart name \"{chart.Name}\" is already used in source \"{chartSource}\". Chart names can only be used once, so it cannot be added for the manual repository.");
                }

                chartNames.Add(chart.Name, manualRepositorySource);
            }
        }
    }
}
