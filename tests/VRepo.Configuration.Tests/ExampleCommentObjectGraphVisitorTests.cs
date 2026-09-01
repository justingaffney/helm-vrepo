using VRepo.Configuration.Serialization.Yaml.Attributes;
using VRepo.Configuration.Serialization.Yaml.ObjectGraphVisitors;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace VRepo.Configuration.Tests;

public class ExampleCommentObjectGraphVisitorTests
{
    private sealed class Target
    {
        [YamlExampleComment("This is an example comment")]
        public string? WithComment { get; init; }

        public string? WithoutComment { get; init; }
    }

    [Fact]
    public void Serializer_Includes_ExampleComment_In_Output()
    {
        var serializer = new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .DisableAliases()
            .WithEmissionPhaseObjectGraphVisitor(args => new ExampleCommentObjectGraphVisitor(args.InnerVisitor))
            .Build();

        var target = new Target { WithComment = "value", WithoutComment = "v2" };

        var yaml = serializer.Serialize(target);

        Assert.Contains("This is an example comment", yaml);
        // Comment should be prefixed with '#'
        Assert.Contains("# This is an example comment", yaml);
    }
}
