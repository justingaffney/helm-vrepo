using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Logging;
using VRepo.Cli.Exceptions;
using VRepo.Cli.Models;
using VRepo.Cli.Services;
using VRepo.Engine.Models;
using VRepo.Engine.Services;

namespace VRepo.Cli.Handlers.Indexes;

public sealed class Generate(
        ILogger<Generate> logger,
        IProcessOutput processOutput,
        IVirtualRepositoryReader virtualRepositoryReader,
        IVirtualRepositoryIndexGenerator virtualRepositoryIndexGenerator)
    : IHandler<Generate.Parameters>
{
    public record Parameters(FileInfo Configuration, TextOutputDestination Output);

    public async ValueTask Handle(Parameters parameters, CancellationToken cancellationToken = default)
    {
        if (!parameters.Configuration.Exists)
        {
            throw new HandlingException("Configuration YAML file does not exist.");
        }

        // Read input
        VirtualRepository repository;
        try
        {
            logger.LogInformation("Reading configuration from \"{File}\"", parameters.Configuration.ToString());
            using var configurationReader = parameters.Configuration.OpenText();
            repository = await virtualRepositoryReader.Read(configurationReader, cancellationToken);
        }
        catch (ValidationException exception)
        {
            throw new HandlingException(exception.Message, exception);
        }

        TextOutputDestination.File? outputFile = parameters.Output is TextOutputDestination.File file ? file : null;
        if (outputFile is not null && outputFile.Info.Directory is not null && !outputFile.Info.Directory.Exists)
        {
            throw new HandlingException($"Output file path's parent directory \"{outputFile.Info.Directory}\" does not exist.");
        }

        var index = await virtualRepositoryIndexGenerator.Generate(repository, cancellationToken);

        // Write output
        try
        {
            using var outputFileWriter = outputFile?.Info.CreateText();
            if (outputFile is not null)
            {
                logger.LogInformation("Writing repository index to \"{File}\"", outputFile.Info.ToString());
            }

            var outputWriter = outputFileWriter ?? processOutput.Stdout;
            index.Serialize(outputWriter);
        }
        catch (IOException exception)
        {
            throw new HandlingException($"Failed to write repository index YAML with error: \"{exception.GetBaseException().Message}\".", exception);
        }
    }
}
