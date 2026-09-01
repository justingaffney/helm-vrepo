using VRepo.Common.Serialization.Json.Schema.Generators;
using VRepo.Common.Versioning;

namespace VRepo.Configuration.Serialization.Json.Schema.Generators;

public sealed class VersionPatternGenerator : StringTypeGenerator<VersionPattern>
{
    protected override bool InlineSingleElementSequences => true;
}
