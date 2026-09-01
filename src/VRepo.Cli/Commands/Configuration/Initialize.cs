using DotMake.CommandLine;
using VRepo.Cli.Exceptions;
using Handler = VRepo.Cli.Handlers.Configuration.Initialize;

namespace VRepo.Cli.Commands.Configuration;

[CliCommand(
    Parent = typeof(Config),
    Name = "init",
    Description = "Initializes a virtual repository configuration file.",
    ShortFormAutoGenerate = CliNameAutoGenerate.None
)]
internal class Initialize : Command<Handler.Parameters, Handler>.WithConfigurableLogging
{
    [CliOption(
        Name = "output",
        Alias = "o",
        Required = true,
        Description = $"The file path to write the initialized configuration YAML to."
    )]
    public required FileInfo Output { get; set; }

    [CliOption(
        Name = "overwrite",
        Required = false,
        Description = $"Whether to overwrite if a file already exists at the specified path."
    )]
    public bool OverwriteExistingFile { get; set; } = false;

    protected override Handler.Parameters GetParameters()
    {
        if (Output.Exists && !OverwriteExistingFile)
        {
            throw new HandlingException("A file already exists at the specified path. Include `--overwrite` to overwrite the existing file.");
        }

        return new(Output);
    }
}
