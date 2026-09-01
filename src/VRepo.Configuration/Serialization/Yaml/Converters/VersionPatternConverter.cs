using VRepo.Common.Serialization.Yaml.Converters;
using VRepo.Common.Versioning;

namespace VRepo.Configuration.Serialization.Yaml.Converters;

public sealed class VersionPatternConverter : StringTypeConverter<VersionPattern>
{
    protected override bool InlineSingleElementSequences => true;

    protected override string ToString(VersionPattern value) => value.ToString();

    protected override VersionPattern FromString(string value) => VersionPattern.Parse(value);
}
