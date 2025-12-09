namespace FTPSheep.Core.Models;

/// <summary>
/// Represents a single deployment history entry.
/// </summary>
public sealed class DeploymentHistoryEntry {
    /// <summary>
    /// Gets or sets the unique identifier for this deployment.
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Gets or sets the timestamp when the deployment started.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the profile name used for this deployment.
    /// </summary>
    public string ProfileName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the server host.
    /// </summary>
    public string ServerHost { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the deployment was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the deployment duration in seconds.
    /// </summary>
    public double DurationSeconds { get; set; }

    /// <summary>
    /// Gets or sets the number of files uploaded.
    /// </summary>
    public int FilesUploaded { get; set; }

    /// <summary>
    /// Gets or sets the total size in bytes.
    /// </summary>
    public long TotalBytes { get; set; }

    /// <summary>
    /// Gets or sets the average upload speed in bytes per second.
    /// </summary>
    public double AverageSpeedBytesPerSecond { get; set; }

    /// <summary>
    /// Gets or sets error messages if the deployment failed.
    /// </summary>
    public List<string> ErrorMessages { get; set; } = new();

    /// <summary>
    /// Gets or sets warning messages.
    /// </summary>
    public List<string> WarningMessages { get; set; } = new();

    /// <summary>
    /// Gets or sets the build configuration used (e.g., Release, Debug).
    /// </summary>
    public string? BuildConfiguration { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="DeploymentHistoryEntry"/> class.
    /// </summary>
    public DeploymentHistoryEntry() {
    }

    /// <summary>
    /// Creates a deployment history entry from a deployment result.
    /// </summary>
    /// <param name="profileName">The profile name.</param>
    /// <param name="serverHost">The server host.</param>
    /// <param name="result">The deployment result.</param>
    /// <param name="buildConfiguration">The build configuration.</param>
    /// <returns>A new deployment history entry.</returns>
    public static DeploymentHistoryEntry FromResult(
        string profileName,
        string serverHost,
        DeploymentResult result,
        string? buildConfiguration = null) {
        return new DeploymentHistoryEntry {
            Timestamp = result.StartTime,
            ProfileName = profileName,
            ServerHost = serverHost,
            Success = result.Success,
            DurationSeconds = result.Duration?.TotalSeconds ?? 0,
            FilesUploaded = result.FilesUploaded,
            TotalBytes = result.TotalBytes,
            AverageSpeedBytesPerSecond = result.AverageSpeedBytesPerSecond ?? 0,
            ErrorMessages = result.ErrorMessages.ToList(),
            WarningMessages = result.WarningMessages.ToList(),
            BuildConfiguration = buildConfiguration
        };
    }
}
