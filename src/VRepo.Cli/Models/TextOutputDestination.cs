namespace VRepo.Cli.Models;

public readonly union TextOutputDestination(TextOutputDestination.File, TextOutputDestination.Stdout)
{
    public record File(FileInfo Info);

    public struct Stdout
    {
        public const string Value = "-";
    }

    public static TextOutputDestination Parse(string value) =>
        !string.Equals(Stdout.Value, value, StringComparison.Ordinal)
            ? new File(new(value))
            : new Stdout();

    public override readonly string ToString() => this switch
    {
        File file => file.Info.ToString(),
        Stdout => Stdout.Value,
        _ => throw new InvalidOperationException($"Unknown `{nameof(TextOutputDestination)}` value.")
    };
}
