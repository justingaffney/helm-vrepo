using Json.Schema;
using Json.Schema.Generation;
using Json.Schema.Generation.Generators;
using Json.Schema.Generation.Intents;

namespace VRepo.Common.Serialization.Json.Schema.Generators;

public abstract class StringTypeGenerator<T> : ISchemaGenerator
{
    protected virtual bool InlineSingleElementSequences => false;

    public bool Handles(Type type) =>
        type == typeof(T) ||
        type == typeof(T[]) ||
        type == typeof(List<T>) ||
        type == typeof(IReadOnlyList<T>) ||
        type == typeof(IReadOnlyCollection<T>);

    public void AddConstraints(SchemaGenerationContextBase context)
    {
        if (context.Type == typeof(T))
        {
            // Single value
            context.Intents.Add(new TypeIntent(SchemaValueType.String));
        }
        else
        {
            // Sequence of values
            var valueType = SchemaValueType.Array;
            if (InlineSingleElementSequences)
            {
                // Allow inline values
                valueType |= SchemaValueType.String;
            }

            context.Intents.Add(new TypeIntent(valueType));
            context.Intents.Add(new ItemsIntent(SchemaGenerationContextCache.Get(typeof(string))));
        }
    }
}
