using DotMake.CommandLine;

namespace VRepo.Cli.Commands;

[CliCommand(
    Parent = typeof(App),
    Name = "index",
    Description = "Group of commands for virtual repository indexes.",
    ShortFormAutoGenerate = CliNameAutoGenerate.None
)]
internal class Index;
