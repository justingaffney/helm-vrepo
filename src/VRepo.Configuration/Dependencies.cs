using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using VRepo.Configuration.Services;
using V1Alpha1 = VRepo.Configuration.Versions.V1Alpha1;

namespace VRepo.Configuration;

public static class Dependencies
{
    public static IServiceCollection AddConfiguration(this IServiceCollection services) =>
        services
            .AddSingleton<IJsonSchemaGenerator, JsonSchemaGenerator>()
            .AddV1Alpha1();

    private static IServiceCollection AddV1Alpha1(this IServiceCollection services) =>
        services.AddSingleton<IValidateOptions<V1Alpha1.Models.VirtualRepositoryConfig>, V1Alpha1.Validation.VirtualRepositoryConfigValidator>();
}
