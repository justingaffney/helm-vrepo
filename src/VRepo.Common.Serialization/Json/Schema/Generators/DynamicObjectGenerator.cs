using Json.Schema;
using Json.Schema.Generation;
using Json.Schema.Generation.Generators;
using Json.Schema.Generation.Intents;
using VRepo.Common.Serialization.Json.Schema.Extensions;

namespace VRepo.Common.Serialization.Json.Schema.Generators;

public class DynamicObjectGenerator : ISchemaGenerator
{
    public bool Handles(Type type) =>
        type.IsGenericType &&
        (
            type.GetGenericTypeDefinition() == typeof(Dictionary<,>) ||
            type.GetGenericTypeDefinition() == typeof(IDictionary<,>) ||
            type.GetGenericTypeDefinition() == typeof(IReadOnlyDictionary<,>)
        ) &&
        type.GetGenericArguments()[0] == typeof(string);

    public void AddConstraints(SchemaGenerationContextBase context)
    {
        context.Intents.Add(new TypeIntent(SchemaValueType.Object));

        // Add SchemaValueType.Null, to allow YAML properties with just a key, for example:
        // ```
        // key-without-value:
        // key-with-simple-value: 123
        // key-with-complex-value:
        //   value: abc123
        // ```
        var valueType = context.Type.GetGenericArguments()[1];
        var valueContext = SchemaGenerationContextCache.Get(valueType).Clone();
        valueContext.AddNullSchemaValueType();
        context.Intents.Add(new AdditionalPropertiesIntent(valueContext));
    }
}
