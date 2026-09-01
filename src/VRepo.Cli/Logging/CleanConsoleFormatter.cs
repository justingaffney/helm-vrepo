using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;

namespace VRepo.Cli.Logging;

/// <summary>
/// Writes only the message, excluding the category and scope.
/// </summary>
public class CleanConsoleFormatter(bool enableColorization) : ConsoleFormatter(FormatterName)
{
    public const string FormatterName = "clean";

    public override void Write<TState>(in LogEntry<TState> logEntry, IExternalScopeProvider? scopeProvider, TextWriter textWriter)
    {
        using var _ = enableColorization
            ? new LogLevelColorizer(textWriter, logEntry.LogLevel)
            : null;

        var message = logEntry.Formatter?.Invoke(logEntry.State, logEntry.Exception);
        if (message is not null)
        {
            textWriter.WriteLine(message);
        }

        if (logEntry.Exception is not null)
        {
            textWriter.WriteLine(logEntry.Exception.Message);
        }
    }

    private class LogLevelColorizer : IDisposable
    {
        private const string RedColorCode = "\u001b[31m";
        private const string YellowColorCode = "\u001b[33m";
        private const string ResetColorCode = "\u001b[0m";

        private readonly TextWriter _writer;
        private readonly bool _isColorizing;

        public LogLevelColorizer(TextWriter writer, LogLevel logLevel)
        {
            _writer = writer;

            var colorCode = logLevel switch
            {
                LogLevel.Warning => YellowColorCode,
                LogLevel.Error or LogLevel.Critical => RedColorCode,
                _ => null,
            };
            _isColorizing = colorCode is not null;

            if (_isColorizing)
            {
                writer.Write(colorCode);
            }
        }

        public void Dispose()
        {
            if (_isColorizing)
            {
                _writer.Write(ResetColorCode);
            }
        }
    }
}
