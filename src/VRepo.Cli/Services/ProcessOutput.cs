namespace VRepo.Cli.Services;

public interface IProcessOutput
{
    TextWriter Stdout { get; }

    TextWriter Stderr { get; }
}

internal sealed class ProcessOutput(TextWriter stdout, TextWriter stderr) : IProcessOutput
{
    public TextWriter Stdout => stdout;

    public TextWriter Stderr => stderr;
}
