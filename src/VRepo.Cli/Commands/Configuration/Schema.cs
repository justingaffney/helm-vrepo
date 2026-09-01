using DotMake.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using VRepo.Cli.Exceptions;
using VRepo.Cli.Models;
using VRepo.Configuration;
using VRepo.Configuration.Versions;
using Handler = VRepo.Cli.Handlers.Configuration.Schema;

namespace VRepo.Cli.Commands.Configuration;

[CliCommand(
    Parent = typeof(Config),
    Name = "schema",
    Description = "Output the configuration JSON schema.",
    ShortFormAutoGenerate = CliNameAutoGenerate.None
)]
internal class Schema : Command<Handler.Parameters, Handler>
{
    [CliOption(
        Name = "api-version",
        Alias = "v",
        Required = false,
        Description = $"The configuration API version targeted by the JSON schema. If not specified the JSON schema will include all supported API versions."
    )]
    public ApiVersion? ApiVersion { get; set; }

    [CliOption(
        Name = "output",
        Alias = "o",
        Required = false,
        Description = $"The file path to write the configuration JSON schema to. Specify \"{TextOutputDestination.Stdout.Value}\" to write to standard output (stdout)."
    )]
    public TextOutputDestination Output { get; set; } = new TextOutputDestination.Stdout();

    [CliOption(
        Name = "overwrite",
        Required = false,
        Description = $"Whether to overwrite if a file already exists at the specified path."
    )]
    public bool OverwriteExistingFile { get; set; } = false;

    protected override Handler.Parameters GetParameters()
    {
        if (Output is TextOutputDestination.File outputFile && outputFile.Info.Exists && !OverwriteExistingFile)
        {
            throw new HandlingException("A file already exists at the specified path. Include `--overwrite` to overwrite the existing file.");
        }

        return new(ApiVersion, Output);
    }

    protected override void ConfigureServices(IServiceCollection services)
    {
        base.ConfigureServices(services);

        services.AddConfiguration();
    }
}
