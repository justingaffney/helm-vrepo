using DotMake.CommandLine;

namespace VRepo.Cli.Commands;

// Root subcommand alias for nested subcommand
[CliCommand(
    Parent = typeof(App),
    Name = "init",
    Description = "Initializes a virtual repository configuration file.",
    ShortFormAutoGenerate = CliNameAutoGenerate.None
)]
internal class Initialize : Configuration.Initialize;
