using Json.Schema.Generation;
using VRepo.Common.Serialization.Exceptions;
using VRepo.Common.Serialization.Json.Schema.Attributes;
using VRepo.Common.Serialization.Yaml.Converters;
using VRepo.Common.Serialization.Yaml.EventEmitters;
using VRepo.Common.Serialization.Yaml.NodeTypeResolvers;
using VRepo.Helm.Specification.Serialization.Yaml.Converters;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace VRepo.Helm.Specification.Models.Repositories;

/// <summary>
/// https://helm.sh/docs/topics/chart_repository/#the-index-file
/// </summary>
public class RepositoryIndex
{
    public const string ValidApiVersion = "v1";

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
    [StringEnum(ValidApiVersion)]
    [Default(ValidApiVersion)]
    public string ApiVersion { get; init; } = ValidApiVersion;

    [YamlMember(Alias = "entries")]
    [Description("The charts and a list of their versions.")]
    public required IReadOnlyDictionary<string, IReadOnlyList<RepositoryChartMetadata>> Entries { get; init; }

    [YamlMember(Alias = "generated")]
    [YamlConverter(typeof(DateTimeOffsetIsoWithoutMillisecondsConverter))]
    [Description("The ISO 8601 formatted timestamp of when the repository index was generated.")]
    public required DateTimeOffset GenerationTime { get; init; }

    public string Serialize() => serializer.Serialize(this);

    public void Serialize(TextWriter writer) => serializer.Serialize(writer, this);

    public static RepositoryIndex Deserialize(string value)
    {
        try
        {
            return deserializer.Deserialize<RepositoryIndex>(value);
        }
        catch (YamlException exception)
        {
            throw new SerializationException($"Failed to deserialize YAML with error: \"{exception.Message}\".", exception);
        }
    }

    public static RepositoryIndex Deserialize(TextReader reader)
    {
        try
        {
            return deserializer.Deserialize<RepositoryIndex>(reader);
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
}
