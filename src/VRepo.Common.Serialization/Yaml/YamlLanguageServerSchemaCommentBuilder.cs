namespace VRepo.Common.Serialization.Yaml;

public static class YamlLanguageServerSchemaCommentBuilder
{
    public static string PrefixYamlWithSchema(string yaml, string jsonSchemaId) => $"# {BuildCommentValue(jsonSchemaId)}\n{yaml}";

    private static string BuildCommentValue(string jsonSchemaId) => $"yaml-language-server: $schema={jsonSchemaId}";
}
