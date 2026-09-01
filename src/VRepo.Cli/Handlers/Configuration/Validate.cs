using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Logging;
using VRepo.Cli.Exceptions;
using VRepo.Engine.Services;

namespace VRepo.Cli.Handlers.Configuration;

public sealed class Validate(
        ILogger<Validate> logger,
        IVirtualRepositoryReader virtualRepositoryReader)
    : IHandler<Validate.Parameters>
{
    public record Parameters(FileInfo Configuration);

    public async ValueTask Handle(Parameters parameters, CancellationToken cancellationToken = default)
    {
        if (!parameters.Configuration.Exists)
        {
            throw new HandlingException("Configuration YAML file does not exist.");
        }

        try
        {
            logger.LogInformation("Reading configuration from \"{File}\"", parameters.Configuration.ToString());
            using var configurationReader = parameters.Configuration.OpenText();
            await virtualRepositoryReader.Read(configurationReader, cancellationToken);
        }
        catch (ValidationException exception)
        {
            throw new HandlingException(exception.Message, exception);
        }

        logger.LogInformation("Configuration is valid.");
    }
}
