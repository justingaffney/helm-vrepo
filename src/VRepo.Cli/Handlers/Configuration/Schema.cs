using Microsoft.Extensions.Logging;
using VRepo.Cli.Exceptions;
using VRepo.Cli.Models;
using VRepo.Cli.Services;
using VRepo.Configuration.Services;
using VRepo.Configuration.Versions;

namespace VRepo.Cli.Handlers.Configuration;

public sealed class Schema(
        ILogger<Schema> logger,
        IProcessOutput processOutput,
        IJsonSchemaGenerator jsonSchemaGenerator)
    : IHandler<Schema.Parameters>
{
    public record Parameters(ApiVersion? ApiVersion, TextOutputDestination Output);

    public async ValueTask Handle(Parameters parameters, CancellationToken cancellationToken = default)
    {
        TextOutputDestination.File? outputFile = parameters.Output is TextOutputDestination.File file ? file : null;
        if (outputFile is not null && outputFile.Info.Directory is not null && !outputFile.Info.Directory.Exists)
        {
            throw new HandlingException($"Output file path's parent directory \"{outputFile.Info.Directory}\" does not exist.");
        }

        var jsonSchema = parameters.ApiVersion.HasValue
            ? jsonSchemaGenerator.Generate(parameters.ApiVersion.Value)
            : jsonSchemaGenerator.Generate();

        try
        {
            using var outputFileWriter = outputFile?.Info.CreateText();
            if (outputFile is not null)
            {
                logger.LogInformation("Writing configuration JSON schema to \"{File}\"", outputFile.Info.ToString());
            }

            var outputWriter = outputFileWriter ?? processOutput.Stdout;
            await outputWriter.WriteAsync(jsonSchema, cancellationToken);
        }
        catch (IOException exception)
        {
            throw new HandlingException($"Failed to write configuration JSON schema with error: \"{exception.GetBaseException().Message}\".", exception);
        }
    }
}
