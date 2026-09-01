using VRepo.Configuration.Serialization.Yaml.Attributes;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.ObjectGraphVisitors;

namespace VRepo.Configuration.Serialization.Yaml.ObjectGraphVisitors;

public sealed class ExampleCommentObjectGraphVisitor(IObjectGraphVisitor<IEmitter> nextVisitor) : ChainedObjectGraphVisitor(nextVisitor)
{
    public override bool EnterMapping(IPropertyDescriptor key, IObjectDescriptor value, IEmitter context, ObjectSerializer serializer)
    {
        var commentValue = key.GetCustomAttribute<YamlExampleCommentAttribute>()?.Comment;
        if (value.Value is not null && !string.IsNullOrWhiteSpace(commentValue))
        {
            context.Emit(new Comment(commentValue, isInline: false));
        }

        return base.EnterMapping(key, value, context, serializer);
    }
}
