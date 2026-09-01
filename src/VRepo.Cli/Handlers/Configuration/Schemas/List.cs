using VRepo.Cli.Services;
using VRepo.Configuration.Versions;

namespace VRepo.Cli.Handlers.Configuration.Schemas;

public sealed class List(IProcessOutput processOutput) : IHandler<List.Parameters>
{
    public struct Parameters();

    public ValueTask Handle(Parameters parameters, CancellationToken cancellationToken = default)
    {
        processOutput.Stdout.WriteLine($"[{string.Join(", ", ApiVersion.AllValues.Select(value => $"\"{value}\""))}]");

        return ValueTask.CompletedTask;
    }
}
