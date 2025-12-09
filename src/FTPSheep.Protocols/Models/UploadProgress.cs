namespace FTPSheep.Protocols.Models;

/// <summary>
/// Represents the real-time progress of concurrent uploads.
/// </summary>
public class UploadProgress {
    /// <summary>
    /// Gets or sets the total number of files to upload.
    /// </summary>
    public int TotalFiles { get; init; }

    /// <summary>
    /// Gets or sets the number of files completed (success or failure).
    /// </summary>
    public int CompletedFiles { get; init; }

    /// <summary>
    /// Gets or sets the number of files currently uploading.
    /// </summary>
    public int ActiveUploads { get; init; }

    /// <summary>
    /// Gets or sets the number of files pending upload.
    /// </summary>
    public int PendingFiles { get; init; }

    /// <summary>
    /// Gets or sets the number of successfully uploaded files.
    /// </summary>
    public int SuccessfulUploads { get; init; }

    /// <summary>
    /// Gets or sets the number of failed uploads.
    /// </summary>
    public int FailedUploads { get; init; }

    /// <summary>
    /// Gets or sets the total bytes to upload.
    /// </summary>
    public long TotalBytes { get; init; }

    /// <summary>
    /// Gets or sets the bytes uploaded so far.
    /// </summary>
    public long UploadedBytes { get; init; }

    /// <summary>
    /// Gets or sets the current upload speed in bytes per second.
    /// </summary>
    public double BytesPerSecond { get; init; }

    /// <summary>
    /// Gets or sets the average upload speed in bytes per second.
    /// </summary>
    public double AverageBytesPerSecond { get; init; }

    /// <summary>
    /// Gets or sets the estimated time remaining.
    /// </summary>
    public TimeSpan? EstimatedTimeRemaining { get; init; }

    /// <summary>
    /// Gets or sets the timestamp when uploads started.
    /// </summary>
    public DateTime StartedAt { get; init; }

    /// <summary>
    /// Gets the upload progress percentage (0-100).
    /// </summary>
    public double ProgressPercentage {
        get {
            if(TotalFiles == 0) return 0;
            return Math.Round((double)CompletedFiles / TotalFiles * 100, 2);
        }
    }

    /// <summary>
    /// Gets the byte progress percentage (0-100).
    /// </summary>
    public double ByteProgressPercentage {
        get {
            if(TotalBytes == 0) return 0;
            return Math.Round((double)UploadedBytes / TotalBytes * 100, 2);
        }
    }

    /// <summary>
    /// Gets the elapsed time since upload started.
    /// </summary>
    public TimeSpan ElapsedTime => DateTime.UtcNow - StartedAt;

    /// <summary>
    /// Gets the formatted current upload speed.
    /// </summary>
    public string FormattedSpeed => FormatSpeed(BytesPerSecond);

    /// <summary>
    /// Gets the formatted average upload speed.
    /// </summary>
    public string FormattedAverageSpeed => FormatSpeed(AverageBytesPerSecond);

    /// <summary>
    /// Gets a value indicating whether all uploads are complete.
    /// </summary>
    public bool IsComplete => CompletedFiles >= TotalFiles;

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
