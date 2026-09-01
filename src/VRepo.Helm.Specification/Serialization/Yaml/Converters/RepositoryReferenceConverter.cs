using VRepo.Common.Serialization.Yaml.Converters;
using VRepo.Helm.Specification.Models.Repositories;

namespace VRepo.Helm.Specification.Serialization.Yaml.Converters;

public sealed class RepositoryReferenceConverter : StringTypeConverter<RepositoryReference>
{
    protected override string ToString(RepositoryReference value) => value.ToString()!;

    protected override RepositoryReference FromString(string value) => RepositoryReference.Parse(value);
}

public sealed class LocalRepositoryReferenceConverter : StringTypeConverter<LocalRepositoryReference>
{
    protected override string ToString(LocalRepositoryReference value) => value.ToString()!;

    protected override LocalRepositoryReference FromString(string value) => LocalRepositoryReference.Parse(value);
}

public sealed class RemoteRepositoryReferenceConverter : StringTypeConverter<RemoteRepositoryReference>
{
    protected override string ToString(RemoteRepositoryReference value) => value.ToString()!;

    protected override RemoteRepositoryReference FromString(string value) => RemoteRepositoryReference.Parse(value);
}
