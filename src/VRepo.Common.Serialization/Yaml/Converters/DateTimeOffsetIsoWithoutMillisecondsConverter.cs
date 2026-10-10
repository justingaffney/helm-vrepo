using System.Globalization;

namespace VRepo.Common.Serialization.Yaml.Converters;

public sealed class DateTimeOffsetIsoWithoutMillisecondsConverter : StringTypeConverter<DateTimeOffset>
{
    protected override string ToString(DateTimeOffset value) => value.UtcDateTime.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture);

    protected override DateTimeOffset FromString(string value) => DateTimeOffset.Parse(value);
}
