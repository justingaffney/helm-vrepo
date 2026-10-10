using DotMake.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using VRepo.Configuration;
using VRepo.Engine;
using Handler = VRepo.Cli.Handlers.Configuration.Validate;

namespace VRepo.Cli.Commands.Configuration;

[CliCommand(
    Parent = typeof(Config),
    Name = "validate",
    Description = "Validates a virtual repository configuration file.",
    ShortFormAutoGenerate = CliNameAutoGenerate.None
)]
internal class Validate : Command<Handler.Parameters, Handler>.WithConfigurableLogging
{
    [CliOption(
        Name = "config",
        Alias = "c",
        Required = true,
        Description = "The file path to read the configuration YAML from."
    )]
    public required FileInfo Configuration { get; set; }

    protected override Handler.Parameters GetParameters() => new(Configuration);

    protected override void ConfigureServices(IServiceCollection services)
    {
        base.ConfigureServices(services);

        services
            .AddConfiguration()
            .AddEngine();
    }
}
