using Microsoft.Extensions.DependencyInjection;
using VRepo.Helm.Sources.Oci.Services;

namespace VRepo.Helm.Sources.Oci;

public static class Dependencies
{
    public static IServiceCollection AddOciProvider(this IServiceCollection services) =>
        services
            .AddHttpClient<IOciRepositoryClientFactory, OciRepositoryClientFactory>().Services
            .AddSingleton<IOciChartProvider, OciChartProvider>();
}
