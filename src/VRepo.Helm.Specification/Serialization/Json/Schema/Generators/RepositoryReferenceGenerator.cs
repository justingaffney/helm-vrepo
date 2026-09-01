using VRepo.Common.Serialization.Json.Schema.Generators;
using VRepo.Helm.Specification.Models.Repositories;

namespace VRepo.Helm.Specification.Serialization.Json.Schema.Generators;

public sealed class RepositoryReferenceGenerator : StringTypeGenerator<RepositoryReference>;

public sealed class LocalRepositoryReferenceGenerator : StringTypeGenerator<LocalRepositoryReference>;

public sealed class RemoteRepositoryReferenceGenerator : StringTypeGenerator<RemoteRepositoryReference>;
