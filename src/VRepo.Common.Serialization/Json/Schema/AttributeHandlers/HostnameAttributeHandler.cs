using Json.Schema;
using Json.Schema.Generation;
using Json.Schema.Generation.Intents;
using VRepo.Common.Serialization.Json.Schema.Attributes;

namespace VRepo.Common.Serialization.Json.Schema.AttributeHandlers;

public sealed class HostnameAttributeHandler : IAttributeHandler<HostnameAttribute>
{
    public void AddConstraints(SchemaGenerationContextBase context, Attribute attribute)
    {
        if (attribute is not HostnameAttribute)
        {
            return;
        }

        context.Intents.Add(new FormatIntent(Formats.Hostname));
    }
}
