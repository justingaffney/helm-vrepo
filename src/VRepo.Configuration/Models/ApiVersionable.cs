using YamlDotNet.Serialization;
using ApiVersionRoot = VRepo.Configuration.Versions.ApiVersion;

namespace VRepo.Configuration.Models;

public interface IApiVersionable
{
    string ApiVersion { get; }
}

public abstract class ApiVersionable : IApiVersionable
{
    [YamlMember(Alias = ApiVersionRoot.YamlMemberName)]
    public abstract string ApiVersion { get; }
}
