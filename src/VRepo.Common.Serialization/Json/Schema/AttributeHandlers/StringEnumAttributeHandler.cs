using Json.Schema.Generation;
using Json.Schema.Generation.Intents;
using VRepo.Common.Serialization.Json.Schema.Attributes;

namespace VRepo.Common.Serialization.Json.Schema.AttributeHandlers;

public sealed class StringEnumAttributeHandler : IAttributeHandler<StringEnumAttribute>
{
    public void AddConstraints(SchemaGenerationContextBase context, Attribute attribute)
    {
        if (attribute is not StringEnumAttribute stringEnumAttribute)
        {
            return;
        }

        context.Intents.Add(new EnumIntent(stringEnumAttribute.Values.ToArray()));
    }
}
