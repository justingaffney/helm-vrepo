using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using VRepo.Configuration;
using VRepo.Configuration.Services;
using VRepo.Configuration.Versions.V1Alpha1.Models;
using VRepo.Engine.Services;
using VRepo.Helm.Sources.Http;
using VRepo.Helm.Sources.Http.Services;
using VRepo.Helm.Sources.Oci;
using VRepo.Helm.Sources.Oci.Services;

namespace VRepo.Engine.Tests;

public class DependenciesTests
{
    [Fact]
    public void RegisteredServices_CanAllBeResolved()
    {
        using var serviceProvider = new ServiceCollection()
            .AddLogging()
            .AddSingleton(TimeProvider.System)
            .AddConfiguration()
            .AddHttpProvider()
            .AddOciProvider()
            .AddEngine()
            .BuildServiceProvider(new ServiceProviderOptions
            {
                ValidateOnBuild = true,
                ValidateScopes = true
            });

        Assert.NotNull(serviceProvider.GetRequiredService<IJsonSchemaGenerator>());
        Assert.NotNull(serviceProvider.GetRequiredService<IValidateOptions<VirtualRepositoryConfig>>());
        Assert.NotNull(serviceProvider.GetRequiredService<IHttpRepositoryIndexProvider>());
        Assert.NotNull(serviceProvider.GetRequiredService<IOciChartProvider>());
        Assert.NotNull(serviceProvider.GetRequiredService<IVirtualRepositoryReader>());
        Assert.NotNull(serviceProvider.GetRequiredService<IVirtualRepositoryIndexGenerator>());
        Assert.NotNull(serviceProvider.GetRequiredService<IChartVersionPolicyEvaluator>());
    }
}
