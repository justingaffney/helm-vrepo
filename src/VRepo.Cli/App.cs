using DotMake.CommandLine;

namespace VRepo.Cli;

[CliCommand(Description = "Helm repository virtualization.")]
internal sealed class App
{
    private static readonly CliSettings CliSettings = new()
    {
        EnableDefaultExceptionHandler = true,
        EnablePosixBundling = false,
        EnableEnvironmentVariablesDirective = true,
        EnableSuggestDirective = true,
        EnableDiagramDirective = true
    };

    public static async Task<int> Run(string[] args) => await DotMake.CommandLine.Cli.RunAsync<App>(args, CliSettings);
}
