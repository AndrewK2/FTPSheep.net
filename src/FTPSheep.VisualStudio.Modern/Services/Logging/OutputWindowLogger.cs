using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Extensibility.Documents;

namespace FTPSheep.VisualStudio.Modern.Services.Logging;

/// <summary>
/// Logger implementation that writes to Visual Studio Output Window.
/// </summary>
[Experimental("VSEXTPREVIEW_OUTPUTWINDOW")]
internal sealed class OutputWindowLogger : ILogger {
    private readonly string categoryName;
    private readonly OutputChannel outputChannel;

    /// <summary>
    /// Initializes a new instance of the <see cref="OutputWindowLogger"/> class.
    /// </summary>
    /// <param name="categoryName">The category name for this logger.</param>
    /// <param name="outputChannel">The Visual Studio output channel to write to.</param>
    public OutputWindowLogger(string categoryName, OutputChannel outputChannel) {
        this.categoryName = categoryName ?? throw new ArgumentNullException(nameof(categoryName));
        this.outputChannel = outputChannel ?? throw new ArgumentNullException(nameof(outputChannel));
    }

    /// <inheritdoc />
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull {
        // Scopes are not currently supported
        return null;
    }

    /// <inheritdoc />
    public bool IsEnabled(LogLevel logLevel) {
        // Enable all log levels
        return logLevel != LogLevel.None;
    }

    /// <inheritdoc />
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) {
        if(!IsEnabled(logLevel)) {
            return;
        }

        var message = formatter(state, exception);

        // Format the log message with timestamp, level, and category
        var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
        var level = GetLogLevelString(logLevel);
        var formattedMessage = $"[{timestamp}] [{level}] {categoryName}: {message}";

        // Add exception details if present
        if(exception != null) {
            formattedMessage += $"\n{exception}";
        }

        // Write to output window asynchronously (fire and forget)
        // Note: We can't await here since Log() is synchronous
        _ = outputChannel.WriteLineAsync(formattedMessage);
    }

    private static string GetLogLevelString(LogLevel logLevel) {
        return logLevel switch {
            LogLevel.Trace => "TRACE",
            LogLevel.Debug => "DEBUG",
            LogLevel.Information => "INFO ",
            LogLevel.Warning => "WARN ",
            LogLevel.Error => "ERROR",
            LogLevel.Critical => "CRIT ",
            _ => "     "
        };
    }
}