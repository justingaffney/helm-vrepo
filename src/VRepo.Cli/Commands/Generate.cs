using DotMake.CommandLine;

namespace VRepo.Cli.Commands;

// Root subcommand alias for nested subcommand
[CliCommand(
    Parent = typeof(App),
    Name = "generate",
    Alias = "gen",
    Description = "Generates a virtual repository index.",
    ShortFormAutoGenerate = CliNameAutoGenerate.None
)]
internal class Generate : Indexes.Generate;
