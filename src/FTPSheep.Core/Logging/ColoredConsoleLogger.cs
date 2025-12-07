using Microsoft.Extensions.Logging;

namespace FTPSheep.Core.Logging;

/// <summary>
/// A console logger that supports colored output based on log level.
/// </summary>
public sealed class ColoredConsoleLogger : ILogger
{
    private readonly string _categoryName;
    private readonly bool _enableColors;
    private readonly LogLevel _minLevel;

    /// <summary>
    /// Initializes a new instance of the <see cref="ColoredConsoleLogger"/> class.
    /// </summary>
    /// <param name="categoryName">The category name for the logger.</param>
    /// <param name="enableColors">Whether to enable colored output.</param>
    /// <param name="minLevel">The minimum log level to output.</param>
    public ColoredConsoleLogger(string categoryName, bool enableColors = true, LogLevel minLevel = LogLevel.Information)
    {
        _categoryName = categoryName;
        _enableColors = enableColors && !Console.IsOutputRedirected;
        _minLevel = minLevel;
    }

    /// <inheritdoc />
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        return null;
    }

    /// <inheritdoc />
    public bool IsEnabled(LogLevel logLevel)
    {
        return logLevel >= _minLevel;
    }

    /// <inheritdoc />
    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
        {
            return;
        }

        var message = formatter(state, exception);
        if (string.IsNullOrEmpty(message) && exception == null)
        {
            return;
        }

        var timestamp = DateTime.Now.ToString("HH:mm:ss");
        var logLevelString = GetLogLevelString(logLevel);

        if (_enableColors)
        {
            WriteColoredMessage(timestamp, logLevelString, message, exception, logLevel);
        }
        else
        {
            WriteMessage(timestamp, logLevelString, message, exception);
        }
    }

    private void WriteColoredMessage(
        string timestamp,
        string logLevelString,
        string message,
        Exception? exception,
        LogLevel logLevel)
    {
        var originalColor = Console.ForegroundColor;

        try
        {
            // Write timestamp in gray
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write($"[{timestamp}] ");

            // Write log level in appropriate color
            Console.ForegroundColor = GetLogLevelColor(logLevel);
            Console.Write($"{logLevelString} ");

            // Write message in default or error color
            if (logLevel >= LogLevel.Error)
            {
                Console.ForegroundColor = ConsoleColor.Red;
            }
            else if (logLevel == LogLevel.Warning)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
            }
            else
            {
                Console.ForegroundColor = originalColor;
            }

            Console.WriteLine(message);

            // Write exception if present
            if (exception != null)
            {
                Console.ForegroundColor = ConsoleColor.DarkRed;
                Console.WriteLine(exception.ToString());
            }
        }
        finally
        {
            Console.ForegroundColor = originalColor;
        }
    }

    private static void WriteMessage(
        string timestamp,
        string logLevelString,
        string message,
        Exception? exception)
    {
        Console.WriteLine($"[{timestamp}] {logLevelString} {message}");

        if (exception != null)
        {
            Console.WriteLine(exception.ToString());
        }
    }

    private static string GetLogLevelString(LogLevel logLevel)
    {
        return logLevel switch
        {
            LogLevel.Trace => "TRCE",
            LogLevel.Debug => "DBUG",
            LogLevel.Information => "INFO",
            LogLevel.Warning => "WARN",
            LogLevel.Error => "FAIL",
            LogLevel.Critical => "CRIT",
            _ => "    "
        };
    }

    private static ConsoleColor GetLogLevelColor(LogLevel logLevel)
    {
        return logLevel switch
        {
            LogLevel.Trace => ConsoleColor.Gray,
            LogLevel.Debug => ConsoleColor.DarkGray,
            LogLevel.Information => ConsoleColor.Green,
            LogLevel.Warning => ConsoleColor.Yellow,
            LogLevel.Error => ConsoleColor.Red,
            LogLevel.Critical => ConsoleColor.DarkRed,
            _ => ConsoleColor.White
        };
    }
}
