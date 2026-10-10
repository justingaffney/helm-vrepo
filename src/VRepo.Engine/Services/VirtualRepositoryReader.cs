using System.ComponentModel.DataAnnotations;
using System.Text;
using Microsoft.Extensions.Options;
using VRepo.Common.Serialization.Exceptions;
using VRepo.Configuration.Exceptions;
using VRepo.Configuration.Models;
using VRepo.Configuration.Versions.V1Alpha1.Models;
using VRepo.Engine.Models;
using VRepo.Engine.Models.Upstream;
using VRepo.Helm.Specification.Models.Repositories.References;
using YamlDotNet.Serialization;
using ApiVersionValue = VRepo.Configuration.Versions.ApiVersion;
using V1Alpha1ApiVersion = VRepo.Configuration.Versions.V1Alpha1.ApiVersion;
using V1Alpha1Configuration = VRepo.Configuration.Versions.V1Alpha1.Models.VirtualRepositoryConfig;

namespace VRepo.Engine.Services;

public interface IVirtualRepositoryReader
{
    Task<VirtualRepository> Read(TextReader rawConfigurationReader, CancellationToken cancellationToken = default);
}

internal sealed class VirtualRepositoryReader(
        IValidateOptions<V1Alpha1Configuration> v1Alpha1ConfigurationValidator)
    : IVirtualRepositoryReader
{
    private static readonly IDeserializer apiVersionDeserializer = new DeserializerBuilder()
        .IgnoreUnmatchedProperties()
        .Build();

    public async Task<VirtualRepository> Read(TextReader rawConfigurationReader, CancellationToken cancellationToken = default)
    {
        var configurationYaml = await rawConfigurationReader.ReadToEndAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(configurationYaml))
        {
            throw new ValidationException("Configuration YAML is blank.");
        }

        var header = apiVersionDeserializer.Deserialize<ApiVersionHeader>(configurationYaml);
        if (string.IsNullOrWhiteSpace(header.ApiVersion))
        {
            throw new ValidationException($"Configuration YAML `{ApiVersionValue.YamlMemberName}` value is missing or blank.");
        }

        var apiVersion = ApiVersionValue.Parse(header.ApiVersion);
        switch (apiVersion)
        {
            case V1Alpha1ApiVersion:
                {
                    V1Alpha1Configuration configuration;
                    try
                    {
                        configuration = V1Alpha1Configuration.Deserialize(configurationYaml);
                    }
                    catch (SerializationException exception)
                    {
                        throw new ValidationException($"Failed to deserialize configuration YAML with error: \"{exception.GetBaseException().Message}\".", exception);
                    }

                    ValidateConfiguration(configuration);

                    return MapConfiguration(configuration);
                }
            default:
                throw new InvalidApiVersionException(header.ApiVersion.ToLowerInvariant());
        }
    }

    private class ApiVersionHeader : IApiVersionable
    {
        [YamlMember(Alias = ApiVersionValue.YamlMemberName)]
        public required string ApiVersion { get; init; }
    }

    private void ValidateConfiguration(V1Alpha1Configuration configuration)
    {
        var validationResult = v1Alpha1ConfigurationValidator.Validate(null, configuration);
        if (!validationResult.Failed)
        {
            return;
        }

        var validationErrors = validationResult.Failures
            .Where(errorMessage => !string.IsNullOrWhiteSpace(errorMessage))
            .ToArray();

        string errorMessage;
        if (validationErrors.Length > 0)
        {
            var errorMessageBuilder = new StringBuilder();

            errorMessageBuilder.AppendLine("Configuration is not valid:");
            foreach (var validationError in validationErrors)
            {
                errorMessageBuilder.Append("- ");
                errorMessageBuilder.AppendLine(validationError);
            }

            errorMessage = errorMessageBuilder.ToString().Trim();
        }
        else
        {
            errorMessage = "Configuration is not valid.";
        }

        throw new ValidationException(errorMessage);
    }

    private static VirtualRepository MapConfiguration(V1Alpha1Configuration configuration)
    {
        var builder = new VirtualRepository.Builder();

        if (configuration.Upstream is not null)
        {
            var globalChartVersionPolicy = configuration.Upstream.GlobalChartVersionPolicy ?? ChartVersionPolicyConfig.Blank;
            foreach (var repository in configuration.Upstream.Repositories)
            {
                // Merge the repository chart version policy into the global chart version policy
                var repositoryChartVersionPolicy = globalChartVersionPolicy.Merge(repository.ChartVersionPolicy ?? ChartVersionPolicyConfig.Blank);

                var charts = repository.Charts.Select(chartConfiguration =>
                {
                    var (chartName, chart) = chartConfiguration;

                    chart ??= new();

                    // Merge the chart version policy into the repository chart version policy
                    var chartVersionPolicy = repositoryChartVersionPolicy.Merge(chart.VersionPolicy ?? ChartVersionPolicyConfig.Blank);

                    return new UpstreamChart()
                    {
                        Name = chartName,
                        VersionPolicy = new()
                        {
                            ExclusionRules = new()
                            {
                                ExcludeInvalidSemVer = chartVersionPolicy.ExclusionRules?.ExcludeInvalidSemVer ?? true, // Exclude by default
                                ExcludePrereleaseVersions = chartVersionPolicy.ExclusionRules?.ExcludePrereleaseVersions ?? false, // Do not exclude by default
                                VersionExclusionPatterns = chartVersionPolicy.ExclusionRules?.VersionExclusionPatterns ?? []
                            }
                        }
                    };
                });

                switch (repository.Url)
                {
                    case HttpUrlRepository url:
                        {
                            builder.AddUpstreamHttpRepository(url, repositoryBuilder =>
                            {
                                foreach (var chart in charts)
                                {
                                    repositoryBuilder.AddChart(chart);
                                }
                            });
                            break;
                        }
                    case OciUrlRepository url:
                        {
                            builder.AddUpstreamOciRepository(url, repositoryBuilder =>
                            {
                                if (!string.IsNullOrWhiteSpace(repository.OciRepository?.AuthenticationRealmHostname))
                                {
                                    repositoryBuilder.SetAuthenticationRealmHostname(repository.OciRepository.AuthenticationRealmHostname);
                                }

                                foreach (var chart in charts)
                                {
                                    repositoryBuilder.AddChart(chart);
                                }
                            });
                            break;
                        }
                }
            }
        }

        if (configuration.ManualRepository is not null)
        {
            builder.AddManualRepository(repositoryBuilder =>
            {
                var charts = configuration.ManualRepository.Entries
                    .Select(entry =>
                    {
                        var (chartName, chartVersions) = entry;

                        return new RepositoryIndexChart
                        {
                            Name = chartName,
                            Versions = chartVersions
                        };
                    });

                foreach (var chart in charts)
                {
                    repositoryBuilder.AddChart(chart);
                }
            });
        }

        return builder.Build();
    }
}
