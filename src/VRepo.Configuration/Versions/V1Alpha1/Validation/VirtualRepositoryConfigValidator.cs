using Microsoft.Extensions.Options;
using VRepo.Configuration.Versions.V1Alpha1.Models;

namespace VRepo.Configuration.Versions.V1Alpha1.Validation;

internal class VirtualRepositoryConfigValidator : IValidateOptions<VirtualRepositoryConfig>
{
    public ValidateOptionsResult Validate(string? name, VirtualRepositoryConfig configuration)
    {
        var errors = new List<string>();

        if (configuration.Upstream is not null && configuration.Upstream.Repositories.Count > 0)
        {
            var duplicateRepositoryUrls = configuration.Upstream.Repositories
                .Select(repository => repository.Url)
                .GroupBy(url => url)
                .Where(urls => urls.Count() > 1)
                .Select(urls => urls.Key)
                .ToArray();

            foreach (var url in duplicateRepositoryUrls)
            {
                errors.Add($"URL '{url}' has been specified by multiple repositories.");
            }
        }

        // Check for chart name collisions across repositories
        var chartNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (configuration.Upstream is not null)
        {
            for (int i = 0; i < configuration.Upstream.Repositories.Count; i++)
            {
                var repository = configuration.Upstream.Repositories[i];
                if (repository.Charts is null)
                {
                    errors.Add($"Repository[{i}] has no charts specified.");
                    continue;
                }

                foreach (var (chartName, chartConfiguration) in repository.Charts)
                {
                    if (chartNames.Contains(chartName))
                    {
                        errors.Add($"Chart name \"{chartName}\" in repository[{i}] has already been specified.");
                        continue;
                    }

                    chartNames.Add(chartName);
                }
            }
        }

        if (configuration.ManualRepository is not null)
        {
            foreach (var chartName in configuration.ManualRepository.Entries.Keys)
            {
                if (chartNames.Contains(chartName))
                {
                    errors.Add($"Chart name \"{chartName}\" has already been specified in a repository.");
                    continue;
                }
            }
        }

        return errors.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(errors);
    }
}
