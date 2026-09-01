using Microsoft.Extensions.DependencyInjection;
using VRepo.Helm.Sources.Http.Services;

namespace VRepo.Helm.Sources.Http;

public static class Dependencies
{
    public static IServiceCollection AddHttpProvider(this IServiceCollection services) =>
        services.AddHttpClient<IHttpRepositoryIndexProvider, HttpRepositoryIndexProvider>().Services;
}
