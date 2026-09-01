using VRepo.Common.Serialization.Yaml.Converters;
using YamlDotNet.Serialization;

namespace VRepo.Common.Tests.Serialization;

public class StringTypeConverterTests
{
    [Fact]
    public void WriteYaml_And_ReadYaml_HandleScalarAndSequenceValues()
    {
        var serializer = new SerializerBuilder().WithTypeConverter(new DemoStringConverter()).Build();
        var text = serializer.Serialize(new DemoContainer
        {
            Name = "single",
            Tags = ["alpha", "beta"]
        });

        Assert.Contains("Name: single", text, StringComparison.Ordinal);
        Assert.Contains("- alpha", text, StringComparison.Ordinal);
        Assert.Contains("- beta", text, StringComparison.Ordinal);

        var deserializer = new DeserializerBuilder().WithTypeConverter(new DemoStringConverter()).Build();
        var value = deserializer.Deserialize<DemoContainer>(text);

        Assert.Equal("single", value.Name);
        Assert.Equal(["alpha", "beta"], value.Tags);
    }

    private sealed class DemoContainer
    {
        public string Name { get; set; } = string.Empty;
        public IReadOnlyList<string> Tags { get; set; } = [];
    }

    private sealed class DemoStringConverter : StringTypeConverter<string>
    {
        protected override string ToString(string value) => value;
        protected override string FromString(string value) => value;
    }
}
