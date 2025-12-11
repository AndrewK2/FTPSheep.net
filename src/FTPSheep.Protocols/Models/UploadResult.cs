namespace FTPSheep.Protocols.Models;

/// <summary>
/// Represents the result of a file upload operation.
/// </summary>
public class UploadResult {
    /// <summary>
    /// Gets or sets the upload task that was executed.
    /// </summary>
    public required UploadTask Task { get; init; }

    /// <summary>
    /// Gets or sets whether the upload was successful.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Gets or sets the error message (if upload failed).
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Gets or sets the exception that occurred (if any).
    /// </summary>
    public Exception? Exception { get; init; }

    /// <summary>
    /// Gets or sets the upload duration.
    /// </summary>
    public TimeSpan Duration { get; init; }

    /// <summary>
    /// Gets or sets the upload speed in bytes per second.
    /// </summary>
    public double BytesPerSecond { get; init; }

    /// <summary>
    /// Gets or sets the number of retry attempts made.
    /// </summary>
    public int RetryAttempts { get; init; }

    /// <summary>
    /// Gets or sets the timestamp when upload started.
    /// </summary>
    public DateTime StartedAt { get; init; }

    /// <summary>
    /// Gets or sets the timestamp when upload completed.
    /// </summary>
    public DateTime CompletedAt { get; init; }

    /// <summary>
    /// Gets the formatted upload speed (e.g., "1.2 MB/s").
    /// </summary>
    public string FormattedSpeed => FormatSpeed(BytesPerSecond);

    /// <summary>
    /// Creates a successful upload result.
    /// </summary>
    public static UploadResult FromSuccess(
        UploadTask task,
        bool success,
        DateTime startedAt,
        DateTime completedAt,
        int retryAttempts = 0) {
        var duration = completedAt - startedAt;
        var bytesPerSecond = duration.TotalSeconds > 0 ? task.FileSize / duration.TotalSeconds : 0;

        return new UploadResult {
            Task = task,
            Success = success,
            Duration = duration,
            BytesPerSecond = bytesPerSecond,
            RetryAttempts = retryAttempts,
            StartedAt = startedAt,
            CompletedAt = completedAt
        };
    }

    /// <summary>
    /// Creates a failed upload result.
    /// </summary>
    public static UploadResult FromFailure(
        UploadTask task,
        Exception exception,
        DateTime startedAt,
        DateTime completedAt,
        int retryAttempts = 0) {
        return new UploadResult {
            Task = task,
            Success = false,
            ErrorMessage = exception.Message,
            Exception = exception,
            Duration = completedAt - startedAt,
            BytesPerSecond = 0,
            RetryAttempts = retryAttempts,
            StartedAt = startedAt,
            CompletedAt = completedAt
        };
    }

    /// <summary>
    /// Formats bytes per second to human-readable speed.
    /// </summary>
    private static string FormatSpeed(double bytesPerSecond) {
        if(bytesPerSecond == 0) return "0 B/s";

        var units = new[] { "B/s", "KB/s", "MB/s", "GB/s" };
        var unitIndex = 0;
        var speed = bytesPerSecond;

        while(speed >= 1024 && unitIndex < units.Length - 1) {
            speed /= 1024;
            unitIndex++;
        }

        return $"{speed:F2} {units[unitIndex]}";
    }
}
