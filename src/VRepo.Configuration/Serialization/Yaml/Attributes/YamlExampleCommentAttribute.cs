namespace VRepo.Configuration.Serialization.Yaml.Attributes;

[AttributeUsage(AttributeTargets.Property)]
public sealed class YamlExampleCommentAttribute(string comment) : Attribute
{
    public string Comment { get; } = comment;
}
