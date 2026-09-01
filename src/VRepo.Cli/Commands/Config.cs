using DotMake.CommandLine;

namespace VRepo.Cli.Commands;

[CliCommand(
    Parent = typeof(App),
    Name = "config",
    Description = "Group of commands for virtual repository configuration.",
    ShortFormAutoGenerate = CliNameAutoGenerate.None
)]
internal class Config;
