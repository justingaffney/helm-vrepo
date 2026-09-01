using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.EventEmitters;

namespace VRepo.Common.Serialization.Yaml.EventEmitters;

public sealed class QuotedBooleanStringEventEmitter(IEventEmitter nextEmitter) : ChainedEventEmitter(nextEmitter)
{
    public override void Emit(ScalarEventInfo eventInfo, IEmitter emitter)
    {
        if (eventInfo.Source.Value is string strValue &&
            (string.Equals(strValue, "true", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(strValue, "false", StringComparison.OrdinalIgnoreCase)))
        {
            // Value could be misinterpreted as a boolean value, ensure it is quoted
            // so it is only interpreted as a string
            eventInfo.Style = ScalarStyle.DoubleQuoted;
        }

        base.Emit(eventInfo, emitter);
    }
}
