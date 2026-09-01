using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace VRepo.Common.Serialization.Yaml.Converters;

public abstract class StringTypeConverter<T> : IYamlTypeConverter
{
    protected virtual bool InlineSingleElementSequences => false;

    public bool Accepts(Type type) =>
        type == typeof(T) ||
        type == typeof(T[]) ||
        type == typeof(List<T>) ||
        type == typeof(IReadOnlyList<T>) ||
        type == typeof(IReadOnlyCollection<T>);

    public void WriteYaml(IEmitter emitter, object? value, Type type, ObjectSerializer serializer)
    {
        if (value is null)
        {
            // TODO Is this actually the right way to handle it?
            return;
        }

        // Single value
        if (value is T singleValue)
        {
            WriteSingleValue(emitter, singleValue);
        }

        // Sequence of values
        if (value is IEnumerable<T> sequenceValues)
        {
            var values = sequenceValues.ToArray();
            if (values.Length == 1 && InlineSingleElementSequences)
            {
                WriteSingleValue(emitter, values[0]);
            }
            else
            {
                emitter.Emit(new SequenceStart(AnchorName.Empty, TagName.Empty, true, SequenceStyle.Any));
                foreach (var sequenceValue in sequenceValues)
                {
                    WriteSingleValue(emitter, sequenceValue);
                }
                emitter.Emit(new SequenceEnd());
            }
        }
    }

    public object ReadYaml(IParser parser, Type type, ObjectDeserializer rootDeserializer)
    {
        // Single value
        if (type == typeof(T) || Nullable.GetUnderlyingType(type) == typeof(T))
        {
            return ReadSingleValue(parser)!;
        }

        // Sequence of values
        var list = new List<T>();
        if (parser.Accept<Scalar>(out _) && InlineSingleElementSequences)
        {
            var value = ReadSingleValue(parser);
            list.Add(value);
        }
        else
        {
            parser.Consume<SequenceStart>();
            while (!parser.Accept<SequenceEnd>(out _))
            {
                var value = ReadSingleValue(parser);
                list.Add(value);
            }
            parser.Consume<SequenceEnd>();
        }

        return type == typeof(T[])
            ? list.ToArray()
            : list;
    }

    protected abstract string ToString(T value);

    protected abstract T FromString(string value);

    private void WriteSingleValue(IEmitter emitter, T value) => emitter.Emit(new Scalar(ToString(value)));

    private T ReadSingleValue(IParser parser) => FromString(parser.Consume<Scalar>().Value);
}
