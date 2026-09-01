namespace VRepo.Common.Serialization.Json.Schema.Attributes;

[AttributeUsage(AttributeTargets.Property)]
public sealed class StringEnumAttribute(params string[] values) : Attribute
{
    public IReadOnlyList<string> Values { get; } = values.ToArray();
}
