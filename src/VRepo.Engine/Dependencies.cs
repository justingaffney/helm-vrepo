using Microsoft.Extensions.DependencyInjection;
using VRepo.Engine.Services;

namespace VRepo.Engine;

public static class Dependencies
{
    public static IServiceCollection AddEngine(this IServiceCollection services) =>
        services
            .AddSingleton<IVirtualRepositoryReader, VirtualRepositoryReader>()
            .AddSingleton<IVirtualRepositoryIndexGenerator, VirtualRepositoryIndexGenerator>()
            .AddSingleton<IChartVersionPolicyEvaluator, ChartVersionPolicyEvaluator>();
}
