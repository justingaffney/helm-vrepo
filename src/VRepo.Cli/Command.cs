using DotMake.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using VRepo.Cli.Exceptions;
using VRepo.Cli.Handlers;
using VRepo.Cli.Logging;
using VRepo.Cli.Services;

namespace VRepo.Cli;

public abstract class Command<TParameters, THandler> : ICliRunAsyncWithContextAndReturn
    where THandler : class, IHandler<TParameters>
{
    protected virtual LogVerbosity LogVerbosity => LogVerbosity.Quiet;

    protected abstract TParameters GetParameters();

    public async Task<int> RunAsync(CliContext cliContext)
    {
        var provider = BuildServiceProvider(cliContext);

        var handler = provider.GetRequiredService<THandler>();
        try
        {
            var parameters = GetParameters();
            await handler.Handle(parameters, cliContext.CancellationToken);
            return 0;
        }
        catch (OperationCanceledException) when (cliContext.CancellationToken.IsCancellationRequested)
        {
            // SIGTERM exit code
            return 143;
        }
        catch (HandlingException exception)
        {
            var logger = provider.GetRequiredService<ILogger<Command<TParameters, THandler>>>();
            logger.LogError(exception, "Command failed with an error.");
            return 1;
        }
    }

    protected virtual void ConfigureServices(IServiceCollection services)
    {
    }

    private IServiceProvider BuildServiceProvider(CliContext cliContext)
    {
        var services = new ServiceCollection();

        var applicationContext = new ApplicationContext(LogVerbosity);

        // Register host services
        services
            .AddSingleton<IProcessOutput>(new ProcessOutput(cliContext.Output, cliContext.Error))
            .AddSingleton<IApplicationContext>(applicationContext)
            .AddHostServices(applicationContext);

        // Register command services
        ConfigureServices(services);

        // Register command's handler
        services.AddTransient<THandler>();

        return services.BuildServiceProvider();
    }

    public abstract class WithConfigurableLogging : Command<TParameters, THandler>
    {
        [CliOption(
            Name = "quiet",
            Alias = "q",
            Required = false,
            Description = "Suppress all logging except for warnings and errors. Overrides the verbose flag."
        )]
        public bool QuietLogging { get; set; }

        [CliOption(
            Name = "verbose",
            Alias = "v",
            Required = false,
            Description = "Enable verbose logging."
        )]
        public bool VerboseLogging { get; set; }

        protected override LogVerbosity LogVerbosity =>
            QuietLogging
                ? LogVerbosity.Quiet
                : VerboseLogging
                    ? LogVerbosity.Verbose
                    : LogVerbosity.Normal;
    }
}
