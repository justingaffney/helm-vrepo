using VRepo.Cli.Logging;

namespace VRepo.Cli.Services;

public interface IApplicationContext
{
    LogVerbosity LogVerbosity { get; }
}

internal sealed class ApplicationContext(LogVerbosity logVerbosity) : IApplicationContext
{
    public LogVerbosity LogVerbosity { get; } = logVerbosity;
}
