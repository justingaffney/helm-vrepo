using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using VRepo.Cli.Logging;
using VRepo.Cli.Services;

namespace VRepo.Cli;

public static class Dependencies
{
    public static IServiceCollection AddHostServices(this IServiceCollection services, IApplicationContext applicationContext)
    {
        return services
            .AddSingleton(TimeProvider.System)
            .AddSingleton<IConfiguration>(new ConfigurationBuilder().AddEnvironmentVariables().Build())
            .AddLogging(builder =>
            {
                // Write all logs to stderr, so actual command output can be written to stdout
                builder.AddConsole(options => options.LogToStandardErrorThreshold = LogLevel.Trace);

                // Configure log formatting
                switch (applicationContext.LogVerbosity)
                {
                    case LogVerbosity.Verbose:
                        {
                            // Use console formatter that outputs full log details
                            builder
                                .AddSimpleConsole(options =>
                                {
                                    options.IncludeScopes = true;
                                    options.SingleLine = false;
                                    options.TimestampFormat = "yyyy-MM-dd HH:mm:ss ";
                                });
                            break;
                        }
                    default:
                        {
                            // Use console formatter that just outputs the log message
                            var formatter = new CleanConsoleFormatter(enableColorization: !Console.IsErrorRedirected);
                            builder.Services.AddSingleton<ConsoleFormatter>(formatter);
                            builder
                                .AddConsole(options => options.FormatterName = CleanConsoleFormatter.FormatterName)
                                .AddConsoleFormatter<CleanConsoleFormatter, ConsoleFormatterOptions>();
                            break;
                        }
                }

                // Configure log verbosity
                builder
                    .SetMinimumLevel(applicationContext.LogVerbosity switch
                    {
                        LogVerbosity.Quiet => LogLevel.Warning,
                        LogVerbosity.Normal => LogLevel.Information,
                        LogVerbosity.Verbose => LogLevel.Debug,
                        _ => LogLevel.Information
                    })
                    .AddFilter("System.Net.Http", applicationContext.LogVerbosity switch
                    {
                        LogVerbosity.Quiet => LogLevel.Error,
                        LogVerbosity.Normal => LogLevel.Warning,
                        LogVerbosity.Verbose => LogLevel.Debug,
                        _ => LogLevel.Warning
                    })
                    .AddFilter("Polly", applicationContext.LogVerbosity switch
                    {
                        LogVerbosity.Quiet => LogLevel.Error,
                        LogVerbosity.Normal => LogLevel.Warning,
                        LogVerbosity.Verbose => LogLevel.Debug,
                        _ => LogLevel.Warning
                    });
            })
            .ConfigureHttpClientDefaults(builder => builder.AddStandardResilienceHandler());
    }
}
