using System.ComponentModel.DataAnnotations;
using Json.Schema.Generation;
using VRepo.Common.Serialization.Exceptions;
using VRepo.Common.Serialization.Json.Schema.Attributes;
using VRepo.Common.Serialization.Yaml.Converters;
using VRepo.Common.Serialization.Yaml.EventEmitters;
using VRepo.Common.Serialization.Yaml.NodeTypeResolvers;
using VRepo.Helm.Specification.Models.Repositories;
using VRepo.Helm.Specification.Serialization.Yaml.Converters;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace VRepo.Helm.Specification.Models.Charts;

/// <summary>
/// https://helm.sh/docs/topics/charts/#the-chartyaml-file
/// <br />
/// https://www.schemastore.org/chart.json
/// </summary>
public class ChartMetadata
{
    private const string SourceJsonSchemaDescriptionSuffix = "\n\nCopied from: https://www.schemastore.org/chart.json";

    public const string ApiVersionV1 = "v1";
    public const string ApiVersionV2 = "v2";
    public const string ApiVersionV3 = "v3";

    public const string LatestStableApiVersion = ApiVersionV2;

    public static readonly IReadOnlyList<string> ValidApiVersions =
    [
        ApiVersionV1,
        ApiVersionV2,
        ApiVersionV3
    ];

    private static readonly ISerializer serializer = ApplyDefaultSerializationConfiguration(new SerializerBuilder())
        .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitDefaults | DefaultValuesHandling.OmitEmptyCollections)
        .DisableAliases()
        .WithIndentedSequences()
        .WithEventEmitter(next => new QuotedBooleanStringEventEmitter(next))
        .Build();

    private static readonly IDeserializer deserializer = ApplyDefaultSerializationConfiguration(new DeserializerBuilder())
        .IgnoreUnmatchedProperties()
        .WithNodeTypeResolver(new ReadOnlyCollectionNodeTypeResolver())
        .Build();

    [YamlMember(Alias = "apiVersion")]
    [StringEnum(ApiVersionV1, ApiVersionV2, ApiVersionV3)]
    [Description($"The apiVersion field should be v2 for Helm charts that require at least Helm 3. Charts supporting previous Helm versions have an apiVersion set to v1 and are still installable by Helm 3. v3 is experimental.{SourceJsonSchemaDescriptionSuffix}")]
    public required string ApiVersion { get; init; }

    [YamlMember(Alias = "name")]
    [Description($"The name of the chart.{SourceJsonSchemaDescriptionSuffix}")]
    public required string Name { get; init; }

    [YamlMember(Alias = "version", ScalarStyle = ScalarStyle.DoubleQuoted)]
    [Description($"A SemVer 2 version.{SourceJsonSchemaDescriptionSuffix}")]
    public required string Version { get; init; }

    [YamlMember(Alias = "kubeVersion", ScalarStyle = ScalarStyle.DoubleQuoted)]
    [Description($"The optional kubeVersion field can define semver constraints on supported Kubernetes versions. Helm will validate the version constraints when installing the chart and fail if the cluster runs an unsupported Kubernetes version.{SourceJsonSchemaDescriptionSuffix}")]
    public string? KubeVersion { get; init; }

    [YamlMember(Alias = "description")]
    [Description($"A single-sentence description of this project.{SourceJsonSchemaDescriptionSuffix}")]
    public string? Description { get; init; }

    [YamlMember(Alias = "type")]
    [Description($"The type of the chart.{SourceJsonSchemaDescriptionSuffix}")]
    [StringEnum(ChartTypes.Application, ChartTypes.Library)]
    [Default(ChartTypes.Application)]
    public string Type { get; init; } = ChartTypes.Application;

    [YamlMember(Alias = "keywords")]
    [Description($"A list of keywords about this project.{SourceJsonSchemaDescriptionSuffix}")]
    public IReadOnlyList<string>? Keywords { get; init; }

    [YamlMember(Alias = "home")]
    [Description($"The URL of this projects home page.{SourceJsonSchemaDescriptionSuffix}")]
    [Url]
    public string? HomeUrl { get; init; }

    [YamlMember(Alias = "sources")]
    [Description($"A list of URLs to source code for this project.{SourceJsonSchemaDescriptionSuffix}")]
    public IReadOnlyList<string>? SourceUrls { get; init; }

    [YamlMember(Alias = "dependencies")]
    [Description($"In Helm, one chart may depend on any number of other charts. These dependencies can be dynamically linked using the dependencies field in Chart.yaml or brought in to the charts/ directory and managed manually.\nThe charts required by the current chart are defined as a list in the dependencies field.{SourceJsonSchemaDescriptionSuffix}")]
    public IReadOnlyList<Dependency>? Dependencies { get; init; }

    [YamlMember(Alias = "maintainers")]
    public IReadOnlyList<Maintainer>? Maintainers { get; init; }

    [YamlMember(Alias = "icon")]
    [Description($"A URL to an SVG or PNG image to be used as an icon.{SourceJsonSchemaDescriptionSuffix}")]
    [Url]
    public string? IconUrl { get; init; }

    [YamlMember(Alias = "appVersion", ScalarStyle = ScalarStyle.DoubleQuoted)]
    [Description($"Note that the appVersion field is not related to the version field. It is a way of specifying the version of the application. For example, the drupal chart may have an appVersion: \"8.2.1\", indicating that the version of Drupal included in the chart (by default) is 8.2.1. This field is informational, and has no impact on chart version calculations. Wrapping the version in quotes is highly recommended. It forces the YAML parser to treat the version number as a string. Leaving it unquoted can lead to parsing issues in some cases. For example, YAML interprets 1.0 as a floating point value, and a git commit SHA like 1234e10 as scientific notation.{SourceJsonSchemaDescriptionSuffix}")]
    public string? AppVersion { get; init; }

    [YamlMember(Alias = "deprecated")]
    [Description($"When managing charts in a Chart Repository, it is sometimes necessary to deprecate a chart. The optional deprecated field in Chart.yaml can be used to mark a chart as deprecated. If the latest version of a chart in the repository is marked as deprecated, then the chart as a whole is considered to be deprecated. The chart name can be later reused by publishing a newer version that is not marked as deprecated.{SourceJsonSchemaDescriptionSuffix}")]
    public bool Deprecated { get; init; } = false;

    [YamlMember(Alias = "annotations")]
    [Description($"A list of annotations keyed by name.{SourceJsonSchemaDescriptionSuffix}")]
    public IReadOnlyDictionary<string, string>? Annotations { get; init; }

    public string Serialize() => serializer.Serialize(this);

    public void Serialize(TextWriter writer) => serializer.Serialize(writer, this);

    public static ChartMetadata Deserialize(string value)
    {
        try
        {
            return deserializer.Deserialize<ChartMetadata>(value);
        }
        catch (YamlException exception)
        {
            throw new SerializationException($"Failed to deserialize YAML with error: \"{exception.Message}\".", exception);
        }
    }

    public static ChartMetadata Deserialize(TextReader reader)
    {
        try
        {
            return deserializer.Deserialize<ChartMetadata>(reader);
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
            .IgnoreFields();
    }

    public static class ChartTypes
    {
        public const string Application = "application";
        public const string Library = "library";
    }

    public class Dependency
    {
        [YamlMember(Alias = "name")]
        [Description($"The name of the chart.{SourceJsonSchemaDescriptionSuffix}")]
        public required string Name { get; init; }

        [YamlMember(Alias = "version", ScalarStyle = ScalarStyle.DoubleQuoted)]
        [Description($"The version of the chart.{SourceJsonSchemaDescriptionSuffix}")]
        public required string Version { get; init; }

        [YamlMember(Alias = "repository")]
        [YamlConverter(typeof(RepositoryReferenceConverter))]
        [Description($"The repository URL or alias.{SourceJsonSchemaDescriptionSuffix}")]
        public RepositoryReference? Repository { get; init; }

        [YamlMember(Alias = "condition")]
        [Description($"A YAML path that resolves to a boolean, used for enabling/disabling charts.{SourceJsonSchemaDescriptionSuffix}")]
        public string? Condition { get; init; }

        [YamlMember(Alias = "tags")]
        [Description($"Tags can be used to group charts for enabling/disabling together.{SourceJsonSchemaDescriptionSuffix}")]
        public IReadOnlyList<string>? Tags { get; init; }

        [YamlMember(Alias = "import-values")]
        [Description($"ImportValues holds the mapping of source values to parent key to be imported. Each item can be a string or pair of child/parent sublist items.{SourceJsonSchemaDescriptionSuffix}")]
        public IReadOnlyDictionary<string, object>? ImportValues { get; init; } // TODO Fix, this does not actually match what the JSON schema defines

        [YamlMember(Alias = "alias")]
        [Description($"Alias to be used for the chart. Useful when you have to add the same chart multiple times.{SourceJsonSchemaDescriptionSuffix}")]
        public string? Alias { get; init; }

        [YamlMember(Alias = "depends-on")]
        [Description($"A list of subcharts (by name or alias) that need to be ready before this subchart can be installed. Only available when apiVersion is v3.{SourceJsonSchemaDescriptionSuffix}")]
        public IReadOnlyList<string>? DependsOn { get; init; }
    }

    public class Maintainer
    {
        [YamlMember(Alias = "name")]
        [Description($"The maintainers name.{SourceJsonSchemaDescriptionSuffix}")]
        public required string Name { get; init; }

        [YamlMember(Alias = "email")]
        [Description($"The maintainers email.{SourceJsonSchemaDescriptionSuffix}")]
        [EmailAddress]
        public string? Email { get; init; }

        [YamlMember(Alias = "url")]
        [Description($"A URL for the maintainer.{SourceJsonSchemaDescriptionSuffix}")]
        [Url]
        public string? Url { get; init; }
    }
}
