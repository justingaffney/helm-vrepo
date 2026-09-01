using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace VRepo.Common.Serialization.Yaml.NodeTypeResolvers;

public sealed class ReadOnlyCollectionNodeTypeResolver : INodeTypeResolver
{
    private static readonly Dictionary<Type, Type> interfaceImplementations = new()
    {
        [typeof(IReadOnlyCollection<>)] = typeof(List<>),
        [typeof(IReadOnlyList<>)] = typeof(List<>),
        [typeof(IReadOnlyDictionary<,>)] = typeof(Dictionary<,>)
    };

    public bool Resolve(NodeEvent? nodeEvent, ref Type type)
    {
        if (!type.IsInterface ||
            !type.IsGenericType ||
            !interfaceImplementations.TryGetValue(type.GetGenericTypeDefinition(), out var concreteType))
        {
            return false;
        }

        type = concreteType.MakeGenericType(type.GetGenericArguments());
        return true;
    }
}
