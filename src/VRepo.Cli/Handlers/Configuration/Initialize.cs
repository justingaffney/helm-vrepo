using Microsoft.Extensions.Logging;
using VRepo.Cli.Exceptions;
using ConfigurationModel = VRepo.Configuration.Versions.V1Alpha1.Models.VirtualRepositoryConfig;

namespace VRepo.Cli.Handlers.Configuration;

public sealed class Initialize(ILogger<Initialize> logger) : IHandler<Initialize.Parameters>
{
    public record Parameters(FileInfo Output);

    public async ValueTask Handle(Parameters parameters, CancellationToken cancellationToken = default)
    {
        if (parameters.Output.Directory is not null && !parameters.Output.Directory.Exists)
        {
            throw new HandlingException($"Output file path's parent directory \"{parameters.Output.Directory}\" does not exist.");
        }

        var configurationYaml = ConfigurationModel.CreateSerializedExample();

        // Write output
        try
        {
            using var outputWriter = parameters.Output.CreateText();

            logger.LogInformation("Writing repository index to \"{File}\"", parameters.Output.ToString());
            await outputWriter.WriteAsync(configurationYaml, cancellationToken);
        }
        catch (IOException exception)
        {
            throw new HandlingException($"Failed to write configuration YAML with error: \"{exception.GetBaseException().Message}\".", exception);
        }
    }
}
