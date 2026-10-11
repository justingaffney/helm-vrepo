using VRepo.Common.Serialization.Yaml.Converters;
using YamlDotNet.Serialization;

namespace VRepo.Common.Tests.Serialization;

public class DateTimeOffsetIsoWithoutMillisecondsConverterTests
{
    [Fact]
    public void Serialize_And_Deserialize_RoundTripValueInUtc()
    {
        var expected = new DateTimeOffset(2024, 10, 2, 3, 4, 5, TimeSpan.Zero);
        var serializer = new SerializerBuilder().WithTypeConverter(new DateTimeOffsetIsoWithoutMillisecondsConverter()).Build();
        var yaml = serializer.Serialize(new { Value = expected });

        Assert.Contains("Value: 2024-10-02T03:04:05Z", yaml, StringComparison.Ordinal);

        var deserializer = new DeserializerBuilder().WithTypeConverter(new DateTimeOffsetIsoWithoutMillisecondsConverter()).Build();
        var model = deserializer.Deserialize<RoundTripDateTimeOffsetContainer>(yaml);

        Assert.Equal(expected, model.Value);
    }

    [Fact]
    public void Serialize_ConvertsNonUtcOffsetToUtc()
    {
        var value = new DateTimeOffset(2024, 10, 2, 13, 4, 5, TimeSpan.FromHours(10));
        var serializer = new SerializerBuilder().WithTypeConverter(new DateTimeOffsetIsoWithoutMillisecondsConverter()).Build();

        var yaml = serializer.Serialize(new { Value = value });

        Assert.Contains("Value: 2024-10-02T03:04:05Z", yaml, StringComparison.Ordinal);
    }

    private sealed class RoundTripDateTimeOffsetContainer
    {
        public DateTimeOffset Value { get; set; }
    }
}
