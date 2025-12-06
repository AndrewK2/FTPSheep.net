namespace FTPSheep.Core.Models;

/// <summary>
/// Represents the result of a deployment operation.
/// </summary>
public sealed class DeploymentResult
{
    /// <summary>
    /// Gets or sets a value indicating whether the deployment was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the deployment start time.
    /// </summary>
    public DateTime StartTime { get; set; }

    /// <summary>
    /// Gets or sets the deployment end time.
    /// </summary>
    public DateTime? EndTime { get; set; }

    /// <summary>
    /// Gets the deployment duration.
    /// </summary>
    public TimeSpan? Duration => EndTime.HasValue ? EndTime.Value - StartTime : null;

    /// <summary>
    /// Gets or sets the number of files uploaded.
    /// </summary>
    public int FilesUploaded { get; set; }

    /// <summary>
    /// Gets or sets the total size of files uploaded in bytes.
    /// </summary>
    public long TotalBytes { get; set; }

    /// <summary>
    /// Gets the average upload speed in bytes per second.
    /// </summary>
    public double? AverageSpeedBytesPerSecond
    {
        get
        {
            if (Duration?.TotalSeconds > 0)
            {
                return TotalBytes / Duration.Value.TotalSeconds;
            }
            return null;
        }
    }

    /// <summary>
    /// Gets or sets the list of error messages encountered during deployment.
    /// </summary>
    public List<string> ErrorMessages { get; set; } = new();

    /// <summary>
    /// Gets or sets the list of warning messages encountered during deployment.
    /// </summary>
    public List<string> WarningMessages { get; set; } = new();

    /// <summary>
    /// Gets or sets the profile name used for this deployment.
    /// </summary>
    public string? ProfileName { get; set; }

    /// <summary>
    /// Gets or sets the build output path that was deployed.
    /// </summary>
    public string? PublishPath { get; set; }

    /// <summary>
    /// Gets or sets the exception that caused the deployment to fail, if any.
    /// </summary>
    public Exception? Exception { get; set; }

    /// <summary>
    /// Adds an error message to the result.
    /// </summary>
    /// <param name="message">The error message to add.</param>
    public void AddError(string message)
    {
        ErrorMessages.Add(message);
        Success = false;
    }

    /// <summary>
    /// Adds a warning message to the result.
    /// </summary>
    /// <param name="message">The warning message to add.</param>
    public void AddWarning(string message)
    {
        WarningMessages.Add(message);
    }

    /// <summary>
    /// Marks the deployment as complete.
    /// </summary>
    /// <param name="success">Whether the deployment was successful.</param>
    public void Complete(bool success = true)
    {
        EndTime = DateTime.UtcNow;
        Success = success && ErrorMessages.Count == 0;
    }
}
