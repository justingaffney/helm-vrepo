using System.Reflection;
using System.Text.Json;
using Json.Schema;
using Json.Schema.Generation;
using Json.Schema.Generation.DataAnnotations;
using Json.Schema.Generation.Generators;
using Json.Schema.Generation.Intents;
using VRepo.Common.Serialization.Json.Schema.AttributeHandlers;
using VRepo.Common.Serialization.Json.Schema.Extensions;
using VRepo.Common.Serialization.Json.Schema.Generators;
using VRepo.Common.Serialization.Json.Schema.PropertyNameResolvers;
using VRepo.Configuration.Models;
using VRepo.Configuration.Serialization.Json.Schema.Generators;
using VRepo.Configuration.Versions;
using VRepo.Helm.Specification.Serialization.Json.Schema.Generators;
using YamlDotNet.Serialization.NamingConventions;
using V1Alpha1ApiVersion = VRepo.Configuration.Versions.V1Alpha1.ApiVersion;
using V1Alpha1Configuration = VRepo.Configuration.Versions.V1Alpha1.Models.VirtualRepositoryConfig;

namespace VRepo.Configuration.Services;

public interface IJsonSchemaGenerator
{
    /// <summary>
    /// Generates a JSON schema that includes all configuration API versions.
    /// </summary>
    /// <returns>The JSON schema.</returns>
    string Generate();

    /// <summary>
    /// Generates a JSON schema for the specified configuration API version.
    /// </summary>
    /// <param name="apiVersion">The API version.</param>
    /// <returns>The JSON schema.</returns>
    string Generate(ApiVersion apiVersion);
}

internal sealed class JsonSchemaGenerator : IJsonSchemaGenerator
{
    private static readonly JsonSerializerOptions jsonSerializerOptions = new()
    {
        WriteIndented = true
    };

    static JsonSchemaGenerator()
    {
        // Enable data annotation support
        DataAnnotationsSupport.AddDataAnnotations();

        // Register standard attribute handlers
        AttributeHandler.AddHandler<YamlIgnoreAttributeHandler>();
        AttributeHandler.AddHandler<StringEnumAttributeHandler>();
        AttributeHandler.AddHandler<HostnameAttributeHandler>();

        // Register standard generators
        GeneratorRegistry.Register(new DateTimeOffsetGenerator());
        GeneratorRegistry.Register(new DynamicObjectGenerator());

        // Register custom generators
        GeneratorRegistry.Register(new RepositoryReferenceGenerator());
        GeneratorRegistry.Register(new RemoteRepositoryReferenceGenerator());
        GeneratorRegistry.Register(new LocalRepositoryReferenceGenerator());
        GeneratorRegistry.Register(new VersionPatternGenerator());
    }

    public string Generate()
    {
        var generatorConfiguration = CreateGeneratorConfiguration();

        generatorConfiguration.Generators.Add(new MultiApiVersionConfigurationRoot.Generator());

        var schema = new JsonSchemaBuilder()
            .Title("helm-vrepo configuration YAML")
            .FromType<MultiApiVersionConfigurationRoot>(generatorConfiguration)
            .Build();

        return JsonSerializer.Serialize(schema, jsonSerializerOptions);
    }

    public string Generate(ApiVersion apiVersion)
    {
        var generatorConfiguration = CreateGeneratorConfiguration();

        var configurationType = apiVersion switch
        {
            V1Alpha1ApiVersion => typeof(V1Alpha1Configuration),
            _ => throw new ArgumentOutOfRangeException(nameof(apiVersion), "Unrecognized API version value.")
        };

        var schema = new JsonSchemaBuilder()
            .Title($"helm-vrepo configuration YAML ({apiVersion})")
            .FromType(configurationType, generatorConfiguration)
            .Build();

        return JsonSerializer.Serialize(schema, jsonSerializerOptions);
    }

    private static SchemaGeneratorConfiguration CreateGeneratorConfiguration() => new()
    {
        DefaultDialect = MetaSchemas.Draft202012Id,
        PropertyNameResolver = member => YamlMemberAliasPropertyNameResolver.Resolve(member, member => CamelCaseNamingConvention.Instance.Apply(member.Name)),
        TypeNameGenerator = new ConflictingApiVersionModelNameTypeNameGenerator()
    };

    /// <summary>
    /// Use the full type name when generating for a type from the Helm specification or configuration
    /// API version, so type names do not conflict when including multiple API versions in a schema.
    /// </summary>
    private class ConflictingApiVersionModelNameTypeNameGenerator : ITypeNameGenerator
    {
        public string? GenerateName(Type type)
        {
            var typeNamespace = type.Namespace ?? string.Empty;

            return
                typeNamespace.StartsWith("VRepo.Helm.Specification.Models.", StringComparison.Ordinal) ||
                typeNamespace.StartsWith("VRepo.Configuration.Versions.", StringComparison.Ordinal)
                    ? type.FullName
                    : null;
        }
    }

    /// <summary>
    /// Just a placeholder to hook the <see cref="ISchemaGenerator" /> onto. 
    /// </summary>
    private record MultiApiVersionConfigurationRoot
    {
        private static readonly IReadOnlyList<Type> configurationTypes = [
            typeof(V1Alpha1Configuration)
        ];

        private static IEnumerable<ConfigurationTypeWithApiVersion> GetConfigurationTypes() => configurationTypes
            .Select(type =>
            {
                var apiVersionStr = type
                    .GetProperty(nameof(ApiVersionable.ApiVersion))?
                    .GetCustomAttribute<ConstAttribute>()?
                    .Value?
                    .GetValue<string>();
                if (apiVersionStr is null)
                {
                    throw new Exception($"Configuration type \"{type.Name}\" does not implement `{nameof(ApiVersionable)} or set the JSON schema constant using `{nameof(ConstAttribute)}`.");
                }

                var apiVersion = ApiVersion.Parse(apiVersionStr);
                return new ConfigurationTypeWithApiVersion(type, apiVersion);
            })
            .ToArray();

        private record ConfigurationTypeWithApiVersion(Type Type, ApiVersion ApiVersion);

        public class Generator : ISchemaGenerator
        {
            public bool Handles(Type type) => type == typeof(MultiApiVersionConfigurationRoot);

            public void AddConstraints(SchemaGenerationContextBase context)
            {
                var configurationTypes = GetConfigurationTypes().ToArray();

                context.Intents.Clear();
                context.Intents.Add(new TypeIntent(SchemaValueType.Object));
                context.Intents.Add(new RequiredIntent([ApiVersion.YamlMemberName]));

                // Create enum for all API versions
                var allApiVersions = configurationTypes.Select(x => x.ApiVersion.ToString());
                var apiVersionEnumIntent = new EnumIntent(allApiVersions);
                var apiVersionContext = SchemaGenerationContextCache.Get(typeof(string)).Clone();
                apiVersionContext.Intents.Add(apiVersionEnumIntent);
                var apiVersionPropertiesIntent = new PropertiesIntent(new() { [ApiVersion.YamlMemberName] = apiVersionContext });
                context.Intents.Add(apiVersionPropertiesIntent);

                // Create "allOf" elements for each API version
                var allOfConditionalBranches = new List<(ISchemaKeywordIntent If, ISchemaKeywordIntent Then)>();
                foreach (var configurationType in configurationTypes.Select(x => x.Type))
                {
                    var configTypeContext = SchemaGenerationContextCache.Get(configurationType).Clone();

                    // Build "if", matching the specific API version value
                    // Remove everything except for the API version const intent
                    var ifTypeContext = configTypeContext.Clone();
                    var ifPropertiesIntent = ifTypeContext.Intents.OfType<PropertiesIntent>().First();
                    var apiVersionConstProperty = ifPropertiesIntent.Properties[ApiVersion.YamlMemberName];
                    ifPropertiesIntent.Properties.Clear();
                    ifPropertiesIntent.Properties.Add(ApiVersion.YamlMemberName, apiVersionConstProperty);
                    var ifIntent = new IfIntent([ifPropertiesIntent]);

                    // Build "then", adding the configuration type schema as is
                    var thenTypeContext = configTypeContext.Clone();
                    var thenIntent = new ThenIntent(thenTypeContext.Intents);

                    // Add conditional branch
                    allOfConditionalBranches.Add((ifIntent, thenIntent));
                }

                var allOfIntent = new AllOfIntent(allOfConditionalBranches.Select(branch => new ISchemaKeywordIntent[] { branch.If, branch.Then }));
                context.Intents.Add(allOfIntent);
            }
        }
    }
}
