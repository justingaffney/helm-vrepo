using DotMake.CommandLine;
using Handler = VRepo.Cli.Handlers.Configuration.Schemas.List;

namespace VRepo.Cli.Commands.Configuration.Schemas;

[CliCommand(
    Parent = typeof(Schema),
    Name = "list",
    Description = "List all supported configuration API versions.",
    ShortFormAutoGenerate = CliNameAutoGenerate.None
)]
internal class List : Command<Handler.Parameters, Handler>
{
    protected override Handler.Parameters GetParameters() => new();
}
