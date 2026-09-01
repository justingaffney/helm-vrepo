using VRepo.Common.Serialization.Yaml.NodeTypeResolvers;

namespace VRepo.Common.Tests.Serialization;

public class ReadOnlyCollectionNodeTypeResolverTests
{
    [Fact]
    public void Resolve_ResolvesReadOnlyInterfaces()
    {
        var resolver = new ReadOnlyCollectionNodeTypeResolver();

        var readOnlyListType = typeof(IReadOnlyList<string>);
        var listType = readOnlyListType;
        Assert.True(resolver.Resolve(null, ref listType));
        Assert.Equal(typeof(List<string>), listType);

        var readOnlyDictionaryType = typeof(IReadOnlyDictionary<string, int>);
        var dictionaryType = readOnlyDictionaryType;
        Assert.True(resolver.Resolve(null, ref dictionaryType));
        Assert.Equal(typeof(Dictionary<string, int>), dictionaryType);
    }
}
