using FTPSheep.Core.Utils;
using Microsoft.Extensions.Logging;

namespace FTPSheep.Core.Logging;

/// <summary>
/// A file logger that supports log rotation by size and date.
/// </summary>
public sealed class FileLogger : ILogger, IDisposable {
    private readonly string _categoryName;
    private readonly string _logDirectory;
    private readonly long _maxFileSizeBytes;
    private readonly int _maxFileCount;
    private readonly LogLevel _minLevel;
    private readonly SemaphoreSlim _writeLock = new(1, 1);
    private string? _currentLogFile;

    /// <summary>
    /// Initializes a new instance of the <see cref="FileLogger"/> class.
    /// </summary>
    /// <param name="categoryName">The category name for the logger.</param>
    /// <param name="logDirectory">The directory to store log files. If null, uses application data directory.</param>
    /// <param name="maxFileSizeMB">The maximum log file size in MB before rotation (default 10MB).</param>
    /// <param name="maxFileCount">The maximum number of log files to keep (default 5).</param>
    /// <param name="minLevel">The minimum log level to output.</param>
    public FileLogger(
        string categoryName,
        string? logDirectory = null,
        int maxFileSizeMB = 10,
        int maxFileCount = 5,
        LogLevel minLevel = LogLevel.Information) {
        _categoryName = categoryName;
        _logDirectory = logDirectory ?? Path.Combine(PathResolver.GetApplicationDataPath(), "logs");
        _maxFileSizeBytes = maxFileSizeMB * 1024 * 1024;
        _maxFileCount = maxFileCount;
        _minLevel = minLevel;

        EnsureLogDirectoryExists();
    }

    /// <inheritdoc />
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull {
        return null;
    }

    /// <inheritdoc />
    public bool IsEnabled(LogLevel logLevel) {
        return logLevel >= _minLevel;
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
        var logLine = $"[{timestamp}] [{logLevelString}] {_categoryName}: {message}";

        if(exception != null) {
            logLine += Environment.NewLine + exception.ToString();
        }

        logLine += Environment.NewLine;

        WriteToFile(logLine);
    }

    private void WriteToFile(string logLine) {
        _writeLock.Wait();
        try {
            var logFile = GetCurrentLogFile();

            // Check if rotation is needed
            if(File.Exists(logFile)) {
                var fileInfo = new FileInfo(logFile);
                if(fileInfo.Length >= _maxFileSizeBytes) {
                    RotateLogFiles();
                    logFile = GetCurrentLogFile();
                }
            }

            File.AppendAllText(logFile, logLine);
        } catch {
            // Swallow exceptions to prevent logging from breaking the application
        } finally {
            _writeLock.Release();
        }
    }

    private string GetCurrentLogFile() {
        if(_currentLogFile == null) {
            var today = DateTime.UtcNow.ToString("yyyy-MM-dd");
            _currentLogFile = Path.Combine(_logDirectory, $"ftpsheep-{today}.log");
        }

        return _currentLogFile;
    }

    private void RotateLogFiles() {
        // Get all log files sorted by creation time
        var logFiles = Directory.GetFiles(_logDirectory, "ftpsheep-*.log")
            .Select(f => new FileInfo(f))
            .OrderByDescending(f => f.CreationTimeUtc)
            .ToList();

        // Delete old files beyond the max count
        if(logFiles.Count >= _maxFileCount) {
            foreach(var file in logFiles.Skip(_maxFileCount - 1)) {
                try {
                    file.Delete();
                } catch {
                    // Ignore deletion errors
                }
            }
        }

        // Create a new log file with timestamp
        var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd-HHmmss");
        _currentLogFile = Path.Combine(_logDirectory, $"ftpsheep-{timestamp}.log");
    }

    private void EnsureLogDirectoryExists() {
        if(!Directory.Exists(_logDirectory)) {
            Directory.CreateDirectory(_logDirectory);
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
        _writeLock?.Dispose();
    }
}
