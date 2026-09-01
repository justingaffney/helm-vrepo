using Json.Schema.Generation;
using YamlDotNet.Serialization;

namespace VRepo.Common.Serialization.Json.Schema.AttributeHandlers;

public class YamlIgnoreAttributeHandler : IAttributeHandler<YamlIgnoreAttribute>
{
    public void AddConstraints(SchemaGenerationContextBase context, Attribute attribute)
    {
        // Suppress property generation
        context.Intents.Clear();
    }
}
