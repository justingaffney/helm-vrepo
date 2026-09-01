using DotMake.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using VRepo.Cli.Exceptions;
using VRepo.Cli.Models;
using VRepo.Configuration;
using VRepo.Engine;
using VRepo.Helm.Sources.Http;
using VRepo.Helm.Sources.Oci;
using Handler = VRepo.Cli.Handlers.Indexes.Generate;

namespace VRepo.Cli.Commands.Indexes;

[CliCommand(
    Parent = typeof(Index),
    Name = "generate",
    Alias = "gen",
    Description = "Generates a virtual repository index.",
    ShortFormAutoGenerate = CliNameAutoGenerate.None
)]
internal class Generate : Command<Handler.Parameters, Handler>.WithConfigurableLogging
{
    [CliOption(
        Name = "config",
        Alias = "c",
        Required = true,
        Description = "The file path to read the configuration YAML from."
    )]
    public required FileInfo Configuration { get; set; }

    [CliOption(
        Name = "output",
        Alias = "o",
        Required = false,
        Description = $"The file path to write the generated repository index YAML to. Specify \"{TextOutputDestination.Stdout.Value}\" to write to standard output (stdout)."
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

        return new(Configuration, Output);
    }

    protected override void ConfigureServices(IServiceCollection services)
    {
        base.ConfigureServices(services);

        services
            .AddConfiguration()
            .AddHttpProvider()
            .AddOciProvider()
            .AddEngine();
    }
}
