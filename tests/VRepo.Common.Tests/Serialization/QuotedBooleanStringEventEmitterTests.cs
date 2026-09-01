using VRepo.Common.Serialization.Yaml.EventEmitters;
using YamlDotNet.Serialization;

namespace VRepo.Common.Tests.Serialization;

public class QuotedBooleanStringEventEmitterTests
{
    [Fact]
    public void Emit_QuotesBooleanLikeStrings()
    {
        var serializer = new SerializerBuilder()
            .WithEventEmitter(next => new QuotedBooleanStringEventEmitter(next))
            .Build();

        var yaml = serializer.Serialize(new { Enabled = "false", Disabled = "true" });

        Assert.Contains("Enabled: \"false\"", yaml, StringComparison.Ordinal);
        Assert.Contains("Disabled: \"true\"", yaml, StringComparison.Ordinal);
    }
}
