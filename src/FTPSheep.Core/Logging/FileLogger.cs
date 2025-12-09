using FTPSheep.Core.Utils;
using Microsoft.Extensions.Logging;

namespace FTPSheep.Core.Logging;

/// <summary>
/// A file logger that supports log rotation by size and date.
/// </summary>
public sealed class FileLogger : ILogger, IDisposable {
    private readonly string categoryName;
    private readonly string logDirectory;
    private readonly long maxFileSizeBytes;
    private readonly int maxFileCount;
    private readonly LogLevel minLevel;
    private readonly SemaphoreSlim writeLock = new(1, 1);
    private string? currentLogFile;

    /// <summary>
    /// Initializes a new instance of the <see cref="FileLogger"/> class.
    /// </summary>
    /// <param name="categoryName">The category name for the logger.</param>
    /// <param name="logDirectory">The directory to store log files. If null, uses application data directory.</param>
    /// <param name="maxFileSizeMb">The maximum log file size in MB before rotation (default 10MB).</param>
    /// <param name="maxFileCount">The maximum number of log files to keep (default 5).</param>
    /// <param name="minLevel">The minimum log level to output.</param>
    public FileLogger(
        string categoryName,
        string? logDirectory = null,
        int maxFileSizeMb = 10,
        int maxFileCount = 5,
        LogLevel minLevel = LogLevel.Information) {
        this.categoryName = categoryName;
        this.logDirectory = logDirectory ?? Path.Combine(PathResolver.GetApplicationDataPath(), "logs");
        maxFileSizeBytes = maxFileSizeMb * 1024 * 1024;
        this.maxFileCount = maxFileCount;
        this.minLevel = minLevel;

        EnsureLogDirectoryExists();
    }

    /// <inheritdoc />
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull {
        return null;
    }

    /// <inheritdoc />
    public bool IsEnabled(LogLevel logLevel) {
        return logLevel >= minLevel;
    }

    /// <inheritdoc />
    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter) {
        if(!IsEnabled(logLevel)) {
            return;
        }

        var message = formatter(state, exception);
        if(string.IsNullOrEmpty(message) && exception == null) {
            return;
        }

        var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");
        var logLevelString = GetLogLevelString(logLevel);
        var logLine = $"[{timestamp}] [{logLevelString}] {categoryName}: {message}";

        if(exception != null) {
            logLine += Environment.NewLine + exception.ToString();
        }

        logLine += Environment.NewLine;

        WriteToFile(logLine);
    }

    private void WriteToFile(string logLine) {
        writeLock.Wait();
        try {
            var logFile = GetCurrentLogFile();

            // Check if rotation is needed
            if(File.Exists(logFile)) {
                var fileInfo = new FileInfo(logFile);
                if(fileInfo.Length >= maxFileSizeBytes) {
                    RotateLogFiles();
                    logFile = GetCurrentLogFile();
                }
            }

            File.AppendAllText(logFile, logLine);
        } catch {
            // Swallow exceptions to prevent logging from breaking the application
        } finally {
            writeLock.Release();
        }
    }

    private string GetCurrentLogFile() {
        if(currentLogFile == null) {
            var today = DateTime.UtcNow.ToString("yyyy-MM-dd");
            currentLogFile = Path.Combine(logDirectory, $"ftpsheep-{today}.log");
        }

        return currentLogFile;
    }

    private void RotateLogFiles() {
        // Get all log files sorted by creation time
        var logFiles = Directory.GetFiles(logDirectory, "ftpsheep-*.log")
            .Select(f => new FileInfo(f))
            .OrderByDescending(f => f.CreationTimeUtc)
            .ToList();

        // Delete old files beyond the max count
        if(logFiles.Count >= maxFileCount) {
            foreach(var file in logFiles.Skip(maxFileCount - 1)) {
                try {
                    file.Delete();
                } catch {
                    // Ignore deletion errors
                }
            }
        }

        // Create a new log file with timestamp
        var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd-HHmmss");
        currentLogFile = Path.Combine(logDirectory, $"ftpsheep-{timestamp}.log");
    }

    private void EnsureLogDirectoryExists() {
        if(!Directory.Exists(logDirectory)) {
            Directory.CreateDirectory(logDirectory);
        }
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

    /// <inheritdoc />
    public void Dispose() {
        writeLock?.Dispose();
    }
}
