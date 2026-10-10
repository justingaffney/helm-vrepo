using Json.Schema.Generation;
using VRepo.Common.Serialization.Exceptions;
using VRepo.Common.Serialization.Yaml;
using VRepo.Common.Serialization.Yaml.Converters;
using VRepo.Common.Serialization.Yaml.EventEmitters;
using VRepo.Common.Serialization.Yaml.NodeTypeResolvers;
using VRepo.Common.Versioning;
using VRepo.Configuration.Models;
using VRepo.Configuration.Serialization.Yaml.Attributes;
using VRepo.Configuration.Serialization.Yaml.Converters;
using VRepo.Configuration.Serialization.Yaml.ObjectGraphVisitors;
using VRepo.Helm.Specification.Models.Charts;
using VRepo.Helm.Specification.Models.Repositories;
using VRepo.Helm.Specification.Models.Repositories.References;
using VRepo.Helm.Specification.Serialization.Yaml.Converters;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using ApiVersionValue = VRepo.Configuration.Versions.V1Alpha1.ApiVersion;

namespace VRepo.Configuration.Versions.V1Alpha1.Models;

public sealed class VirtualRepositoryConfig : ApiVersionable, IConfigurationRoot
{
    private const string DefaultJsonSchemaId = "https://github.com/justingaffney/helm-vrepo/releases/latest/download/config-schema.json";

    private static readonly ISerializer Serializer = ApplyDefaultSerializationConfiguration(new SerializerBuilder())
        .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitDefaults | DefaultValuesHandling.OmitEmptyCollections)
        .DisableAliases()
        .WithIndentedSequences()
        .WithEventEmitter(next => new QuotedBooleanStringEventEmitter(next))
        .Build();

    private static readonly IDeserializer Deserializer = ApplyDefaultSerializationConfiguration(new DeserializerBuilder())
        .IgnoreUnmatchedProperties()
        .WithNodeTypeResolver(new ReadOnlyCollectionNodeTypeResolver())
        .Build();

    private static readonly ISerializer ExampleSerializer = ApplyDefaultSerializationConfiguration(new SerializerBuilder())
        .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitDefaults)
        .DisableAliases()
        .WithIndentedSequences()
        .WithEventEmitter(next => new QuotedBooleanStringEventEmitter(next))
        .WithEmissionPhaseObjectGraphVisitor(args => new ExampleCommentObjectGraphVisitor(args.InnerVisitor))
        .Build();

    [Const(ApiVersionValue.Value)]
    public override string ApiVersion { get; } = ApiVersionValue.Value;

    [YamlMember(Alias = "upstream")]
    [Description("Upstream chart information, used to retrieve and filter charts to include in the virtual repository index during generation.")]
    public UpstreamConfig? Upstream { get; init; }

    [YamlMember(Alias = "manualRepository")]
    [YamlExampleComment("Charts that do not exist in any upstream repositories, and must be included manually.")]
    [Description("Charts that do not exist in any upstream repositories, and must be included manually.\n\nIn the same format as a repository index (https://helm.sh/docs/topics/chart_repository/#the-index-file).")]
    public ManualRepositoryIndex? ManualRepository { get; init; }

    public string Serialize() => Serializer.Serialize(this);

    public void Serialize(TextWriter writer) => Serializer.Serialize(writer, this);

    public static VirtualRepositoryConfig Deserialize(string value)
    {
        try
        {
            return Deserializer.Deserialize<VirtualRepositoryConfig>(value);
        }
        catch (YamlException exception)
        {
            throw new SerializationException($"Failed to deserialize YAML with error: \"{exception.Message}\".", exception);
        }
    }

    public static VirtualRepositoryConfig Deserialize(TextReader reader)
    {
        try
        {
            return Deserializer.Deserialize<VirtualRepositoryConfig>(reader);
        }
        catch (YamlException exception)
        {
            throw new SerializationException($"Failed to deserialize YAML with error: \"{exception.Message}\".", exception);
        }
    }

    private static TBuilder ApplyDefaultSerializationConfiguration<TBuilder>(TBuilder builder) where TBuilder : BuilderSkeleton<TBuilder>
    {
        return builder
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .WithTypeConverter(new DateTimeOffsetIsoWithoutMillisecondsConverter())
            .WithTypeConverter(new RepositoryReferenceConverter())
            .WithTypeConverter(new RemoteRepositoryReferenceConverter())
            .WithTypeConverter(new VersionPatternConverter())
            .IgnoreFields();
    }

    public static string CreateSerializedExample()
    {
        var configuration = CreateExample();

        return YamlLanguageServerSchemaCommentBuilder.PrefixYamlWithSchema(ExampleSerializer.Serialize(configuration), DefaultJsonSchemaId);
    }

    public sealed class UpstreamConfig
    {
        [YamlMember(Alias = "versionPolicy")]
        [YamlExampleComment("Version policy applied to all charts in all repositories.")]
        [Description("The policy applied to all charts in all repositories for filtering versions.")]
        public ChartVersionPolicyConfig? GlobalChartVersionPolicy { get; init; }

        [YamlMember(Alias = "repositories")]
        [YamlExampleComment("Upstream repositories to include.")]
        [Description("The list of upstream repositories.")]
        public required IReadOnlyList<UpstreamRepositoryConfig> Repositories { get; init; }
    }

    public sealed class UpstreamRepositoryConfig
    {
        [YamlMember(Alias = "url")]
        [YamlConverter(typeof(RemoteRepositoryReferenceConverter))]
        [Description("The remote Helm repository URL. \"https://\" or \"http://\" for HTTP repositories, \"oci://\" for OCI repositories.")]
        public required RemoteRepositoryReference Url { get; init; }

        [YamlMember(Alias = "oci")]
        [YamlExampleComment("OCI repository configuration.")]
        [Description("The OCI repository configuration.")]
        public OciRepositoryConfig? OciRepository { get; init; }

        [YamlMember(Alias = "versionPolicy")]
        [YamlExampleComment("Version policy applied to all charts in this repository.\nThese settings override any settings in the global policy.")]
        [Description("The policy applied to all charts in this repository for filtering versions. These settings override any settings in the global policy.")]
        public ChartVersionPolicyConfig? ChartVersionPolicy { get; init; }

        [YamlMember(Alias = "charts")]
        [Description("The charts to include.")]
        public required IReadOnlyDictionary<string, ChartConfig> Charts { get; init; }

        public sealed class OciRepositoryConfig
        {
            [YamlMember(Alias = "authenticationRealmHostname")]
            [YamlExampleComment("Authentication realm hostname for the OCI repository.\nThis is only required for OCI registries with different authentication realm hostnames.")]
            [Description("The authentication realm hostname for the OCI repository. This is only required for OCI registries with different authentication realm hostnames.")]
            public string? AuthenticationRealmHostname { get; init; }
        }

        public sealed class ChartConfig
        {
            [YamlMember(Alias = "versionPolicy")]
            [YamlExampleComment("Version policy applied to all versions of this chart.\nThese settings override any repository or global settings.")]
            [Description("The policy used to filter this chart's versions. These settings override any settings in the repository or global policies.")]
            public ChartVersionPolicyConfig? VersionPolicy { get; init; }
        }
    }

    public sealed class ManualRepositoryIndex
    {
        [YamlMember(Alias = "entries")]
        [Description("The charts and a list of their versions to include.")]
        public required IReadOnlyDictionary<string, IReadOnlyList<RepositoryChartMetadata>> Entries { get; init; }
    }

    private static VirtualRepositoryConfig CreateExample() => new()
    {
        Upstream = new()
        {
            GlobalChartVersionPolicy = new()
            {
                ExclusionRules = new()
                {
                    ExcludeInvalidSemVer = true,
                    ExcludePrereleaseVersions = false
                }
            },
            Repositories =
            [
                new()
                {
                    Url = new HttpUrlRepository("https://basic.repository.helm.example.com/charts"),
                    Charts = new Dictionary<string, UpstreamRepositoryConfig.ChartConfig>()
                    {
                        ["http-chart-basic-example"] = new()
                    }
                },
                new()
                {
                    Url = new HttpUrlRepository("https://advanced.repository.helm.example.com/charts"),
                    ChartVersionPolicy = new()
                    {
                        ExclusionRules = new()
                        {
                            ExcludePrereleaseVersions = true
                        }
                    },
                    Charts = new Dictionary<string, UpstreamRepositoryConfig.ChartConfig>()
                    {
                        ["http-chart-advanced-include-all-versions"] = new()
                        {
                            VersionPolicy = new()
                            {
                                ExclusionRules = new()
                                {
                                    ExcludePrereleaseVersions = false
                                }
                            }
                        },
                        ["http-chart-advanced-include-invalid-semver-versions"] = new()
                        {
                            VersionPolicy = new()
                            {
                                ExclusionRules = new()
                                {
                                    ExcludeInvalidSemVer = false
                                }
                            }
                        },
                        ["http-chart-advanced-exclude-old-versions"] = new()
                        {
                            VersionPolicy = new()
                            {
                                ExclusionRules = new()
                                {
                                    VersionExclusionPatterns = [VersionPattern.Parse("<15.0.0")]
                                }
                            }
                        }
                    }
                },
                new()
                {
                    Url = new OciUrlRepository("oci://registry.example.com/basic-example-repository/charts"),
                    Charts = new Dictionary<string, UpstreamRepositoryConfig.ChartConfig>()
                    {
                        ["oci-chart-basic-example"] = new()
                    }
                },
                new()
                {
                    Url = new OciUrlRepository("oci://registry.example.com/advanced-example-repository/charts"),
                    OciRepository = new()
                    {
                        AuthenticationRealmHostname = "auth.registry.example.com"
                    },
                    Charts = new Dictionary<string, UpstreamRepositoryConfig.ChartConfig>()
                    {
                        ["oci-chart-advanced-include-single-version-only"] = new()
                        {
                            VersionPolicy = new()
                            {
                                ExclusionRules = new()
                                {
                                    VersionExclusionPatterns = [VersionPattern.Parse("<1.2.5 || >1.2.5")]
                                }
                            }
                        },
                        ["oci-chart-advanced-exclude-single-version-only"] = new()
                        {
                            VersionPolicy = new()
                            {
                                ExclusionRules = new()
                                {
                                    VersionExclusionPatterns = [VersionPattern.Parse("1.2.5")]
                                }
                            }
                        },
                        ["oci-chart-advanced-exclude-ranges-of-versions"] = new()
                        {
                            VersionPolicy = new()
                            {
                                ExclusionRules = new()
                                {
                                    VersionExclusionPatterns =
                                    [
                                        VersionPattern.Parse("1.0.8 - 1.0.13"),
                                        VersionPattern.Parse("~1.2.8")
                                    ]
                                }
                            }
                        },
                        ["oci-chart-advanced-exclude-regex-matches"] = new()
                        {
                            VersionPolicy = new()
                            {
                                ExclusionRules = new()
                                {
                                    ExcludeInvalidSemVer = false,
                                    VersionExclusionPatterns = [VersionPattern.Parse("^(?!chartversionprefix-).*$")]
                                }
                            }
                        },
                    }
                }
            ]
        },
        ManualRepository = new()
        {
            Entries = new Dictionary<string, IReadOnlyList<RepositoryChartMetadata>>
            {
                ["manual-chart"] =
                [
                    new()
                    {
                        ApiVersion = ChartMetadata.LatestStableApiVersion,
                        Name = "manual-chart",
                        Description = "Manual chart example",
                        HomeUrl = "https://helm.example.com/charts/manual-chart",
                        Version = "1.2.3",
                        AppVersion = "v1.2.3",
                        Keywords = [],
                        Dependencies = [],
                        Maintainers =
                        [
                            new()
                            {
                                Name = "helm-vrepo"
                            }
                        ],
                        SourceUrls = ["https://helm.example.com/charts/manual-chart"],
                        Urls = ["https://helm.example.com/charts/manual-chart/releases/1.2.3/manual-chart-1.2.3.tar.gz"],
                        CreationTime = DateTimeOffset.UtcNow
                    }
                ]
            }
        }
    };
}
