namespace FTPSheep.Core.Models;

/// <summary>
/// Represents the result of a deployment operation.
/// </summary>
public sealed class DeploymentResult {
    /// <summary>
    /// Gets or sets the unique identifier for this deployment.
    /// </summary>
    public Guid DeploymentId { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Gets or sets a value indicating whether the deployment was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the final deployment stage.
    /// </summary>
    public DeploymentStage FinalStage { get; set; }

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
    /// Gets or sets the total number of files in the deployment.
    /// </summary>
    public int TotalFiles { get; set; }

    /// <summary>
    /// Gets or sets the number of files uploaded.
    /// </summary>
    public int FilesUploaded { get; set; }

    /// <summary>
    /// Gets or sets the number of files that failed to upload.
    /// </summary>
    public int FilesFailed { get; set; }

    /// <summary>
    /// Gets or sets the total size of files uploaded in bytes.
    /// </summary>
    public long TotalBytes { get; set; }

    /// <summary>
    /// Gets or sets the size successfully uploaded (in bytes).
    /// </summary>
    public long SizeUploaded { get; set; }

    /// <summary>
    /// Gets or sets the number of obsolete files deleted.
    /// </summary>
    public int ObsoleteFilesDeleted { get; set; }

    /// <summary>
    /// Gets or sets whether the deployment was cancelled by the user.
    /// </summary>
    public bool WasCancelled { get; set; }

    /// <summary>
    /// Gets the average upload speed in bytes per second.
    /// </summary>
    public double? AverageSpeedBytesPerSecond {
        get {
            if(Duration?.TotalSeconds > 0) {
                return SizeUploaded / Duration.Value.TotalSeconds;
            }
            return null;
        }
    }

    /// <summary>
    /// Gets a formatted string representation of the average upload speed.
    /// </summary>
    public string FormattedUploadSpeed {
        get {
            var speed = AverageSpeedBytesPerSecond ?? 0;
            if(speed < 1024) return $"{speed:F2} B/s";
            if(speed < 1024 * 1024) return $"{speed / 1024:F2} KB/s";
            return $"{speed / (1024 * 1024):F2} MB/s";
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
    /// Gets or sets a list of files that failed to upload.
    /// </summary>
    public List<string> FailedFiles { get; set; } = new();

    /// <summary>
    /// Gets or sets the profile name used for this deployment.
    /// </summary>
    public string? ProfileName { get; set; }

    /// <summary>
    /// Gets or sets the build output path that was deployed.
    /// </summary>
    public string? PublishPath { get; set; }

    /// <summary>
    /// Gets or sets the project path that was deployed.
    /// </summary>
    public string? ProjectPath { get; set; }

    /// <summary>
    /// Gets or sets the target server host.
    /// </summary>
    public string? TargetHost { get; set; }

    /// <summary>
    /// Gets or sets the exception that caused the deployment to fail, if any.
    /// </summary>
    public Exception? Exception { get; set; }

    /// <summary>
    /// Adds an error message to the result.
    /// </summary>
    /// <param name="message">The error message to add.</param>
    public void AddError(string message) {
        ErrorMessages.Add(message);
        Success = false;
    }

    /// <summary>
    /// Adds a warning message to the result.
    /// </summary>
    /// <param name="message">The warning message to add.</param>
    public void AddWarning(string message) {
        WarningMessages.Add(message);
    }

    /// <summary>
    /// Marks the deployment as complete.
    /// </summary>
    /// <param name="success">Whether the deployment was successful.</param>
    public void Complete(bool success = true) {
        EndTime = DateTime.UtcNow;
        Success = success && ErrorMessages.Count == 0;
        FinalStage = Success ? DeploymentStage.Completed : DeploymentStage.Failed;
    }

    /// <summary>
    /// Creates a successful deployment result from a deployment state.
    /// </summary>
    public static DeploymentResult FromSuccess(DeploymentState state) {
        return new DeploymentResult {
            DeploymentId = state.DeploymentId,
            Success = true,
            FinalStage = DeploymentStage.Completed,
            StartTime = state.StartedAt ?? DateTime.UtcNow,
            EndTime = state.CompletedAt ?? DateTime.UtcNow,
            ProfileName = state.ProfileName,
            ProjectPath = state.ProjectPath,
            TargetHost = state.TargetHost,
            TotalFiles = state.TotalFiles,
            FilesUploaded = state.FilesUploaded,
            FilesFailed = state.FilesFailed,
            TotalBytes = state.TotalSize,
            SizeUploaded = state.SizeUploaded,
            ObsoleteFilesDeleted = state.ObsoleteFilesDeleted
        };
    }

    /// <summary>
    /// Creates a failed deployment result from a deployment state.
    /// </summary>
    public static DeploymentResult FromFailure(DeploymentState state, string errorMessage, Exception? exception = null) {
        return new DeploymentResult {
            DeploymentId = state.DeploymentId,
            Success = false,
            FinalStage = DeploymentStage.Failed,
            Exception = exception,
            StartTime = state.StartedAt ?? DateTime.UtcNow,
            EndTime = state.CompletedAt ?? DateTime.UtcNow,
            ProfileName = state.ProfileName,
            ProjectPath = state.ProjectPath,
            TargetHost = state.TargetHost,
            TotalFiles = state.TotalFiles,
            FilesUploaded = state.FilesUploaded,
            FilesFailed = state.FilesFailed,
            TotalBytes = state.TotalSize,
            SizeUploaded = state.SizeUploaded,
            ObsoleteFilesDeleted = state.ObsoleteFilesDeleted,
            ErrorMessages = new List<string> { errorMessage }
        };
    }

    /// <summary>
    /// Creates a cancelled deployment result from a deployment state.
    /// </summary>
    public static DeploymentResult FromCancellation(DeploymentState state) {
        return new DeploymentResult {
            DeploymentId = state.DeploymentId,
            Success = false,
            FinalStage = DeploymentStage.Cancelled,
            WasCancelled = true,
            StartTime = state.StartedAt ?? DateTime.UtcNow,
            EndTime = state.CompletedAt ?? DateTime.UtcNow,
            ProfileName = state.ProfileName,
            ProjectPath = state.ProjectPath,
            TargetHost = state.TargetHost,
            TotalFiles = state.TotalFiles,
            FilesUploaded = state.FilesUploaded,
            FilesFailed = state.FilesFailed,
            TotalBytes = state.TotalSize,
            SizeUploaded = state.SizeUploaded,
            ObsoleteFilesDeleted = state.ObsoleteFilesDeleted,
            ErrorMessages = new List<string> { "Deployment was cancelled by user" }
        };
    }
}
