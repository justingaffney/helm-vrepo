using Json.Schema;
using Json.Schema.Generation;
using Json.Schema.Generation.Generators;
using Json.Schema.Generation.Intents;

namespace VRepo.Common.Serialization.Json.Schema.Generators;

public class DateTimeOffsetGenerator : ISchemaGenerator
{
    public bool Handles(Type type) => type == typeof(DateTimeOffset);

    public void AddConstraints(SchemaGenerationContextBase context)
    {
        context.Intents.Add(new TypeIntent(SchemaValueType.String));
        context.Intents.Add(new FormatIntent(Formats.DateTime));
    }
}
