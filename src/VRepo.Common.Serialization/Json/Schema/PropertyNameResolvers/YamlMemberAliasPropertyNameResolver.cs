using System.Reflection;
using Json.Schema.Generation;
using YamlDotNet.Serialization;

namespace VRepo.Common.Serialization.Json.Schema.PropertyNameResolvers;

public static class YamlMemberAliasPropertyNameResolver
{
    public static string Resolve(MemberInfo member, PropertyNameResolver fallbackResolver)
    {
        var yamlMemberAttribute = member.GetCustomAttribute<YamlMemberAttribute>();
        return !string.IsNullOrWhiteSpace(yamlMemberAttribute?.Alias)
            ? yamlMemberAttribute.Alias
            : fallbackResolver(member);
    }
}
