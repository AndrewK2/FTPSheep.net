namespace FTPSheep.Core.Models;

/// <summary>
/// Represents the current state of a deployment operation.
/// </summary>
public class DeploymentState
{
    /// <summary>
    /// Gets or sets the unique identifier for this deployment.
    /// </summary>
    public Guid DeploymentId { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Gets or sets the current deployment stage.
    /// </summary>
    public DeploymentStage CurrentStage { get; set; } = DeploymentStage.NotStarted;

    /// <summary>
    /// Gets or sets the timestamp when the deployment started.
    /// </summary>
    public DateTime? StartedAt { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the current stage started.
    /// </summary>
    public DateTime? CurrentStageStartedAt { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the deployment completed (successfully, failed, or cancelled).
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Gets or sets the name of the profile being deployed.
    /// </summary>
    public string? ProfileName { get; set; }

    /// <summary>
    /// Gets or sets the project path being deployed.
    /// </summary>
    public string? ProjectPath { get; set; }

    /// <summary>
    /// Gets or sets the target server host.
    /// </summary>
    public string? TargetHost { get; set; }

    /// <summary>
    /// Gets or sets the total number of files to upload.
    /// </summary>
    public int TotalFiles { get; set; }

    /// <summary>
    /// Gets or sets the number of files uploaded so far.
    /// </summary>
    public int FilesUploaded { get; set; }

    /// <summary>
    /// Gets or sets the number of files that failed to upload.
    /// </summary>
    public int FilesFailed { get; set; }

    /// <summary>
    /// Gets or sets the total size of all files to upload (in bytes).
    /// </summary>
    public long TotalSize { get; set; }

    /// <summary>
    /// Gets or sets the size uploaded so far (in bytes).
    /// </summary>
    public long SizeUploaded { get; set; }

    /// <summary>
    /// Gets or sets the number of obsolete files to clean up.
    /// </summary>
    public int ObsoleteFilesCount { get; set; }

    /// <summary>
    /// Gets or sets the number of obsolete files cleaned up so far.
    /// </summary>
    public int ObsoleteFilesDeleted { get; set; }

    /// <summary>
    /// Gets or sets whether the deployment can be cancelled.
    /// </summary>
    public bool CanCancel { get; set; } = true;

    /// <summary>
    /// Gets or sets whether cancellation has been requested.
    /// </summary>
    public bool CancellationRequested { get; set; }

    /// <summary>
    /// Gets or sets the error message if the deployment failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the exception that caused the deployment to fail.
    /// </summary>
    public Exception? Exception { get; set; }

    /// <summary>
    /// Gets the overall progress percentage (0-100).
    /// </summary>
    public double ProgressPercentage
    {
        get
        {
            if (TotalFiles == 0) return 0;
            return (FilesUploaded / (double)TotalFiles) * 100;
        }
    }

    /// <summary>
    /// Gets the elapsed time since deployment started.
    /// </summary>
    public TimeSpan? ElapsedTime
    {
        get
        {
            if (StartedAt == null) return null;
            var endTime = CompletedAt ?? DateTime.UtcNow;
            return endTime - StartedAt.Value;
        }
    }

    /// <summary>
    /// Gets whether the deployment is currently in progress.
    /// </summary>
    public bool IsInProgress =>
        CurrentStage != DeploymentStage.NotStarted &&
        CurrentStage != DeploymentStage.Completed &&
        CurrentStage != DeploymentStage.Failed &&
        CurrentStage != DeploymentStage.Cancelled;

    /// <summary>
    /// Gets whether the deployment has completed (successfully, failed, or cancelled).
    /// </summary>
    public bool IsCompleted =>
        CurrentStage == DeploymentStage.Completed ||
        CurrentStage == DeploymentStage.Failed ||
        CurrentStage == DeploymentStage.Cancelled;

    /// <summary>
    /// Gets whether the deployment completed successfully.
    /// </summary>
    public bool IsSuccess => CurrentStage == DeploymentStage.Completed;

    /// <summary>
    /// Gets whether the deployment failed.
    /// </summary>
    public bool IsFailed => CurrentStage == DeploymentStage.Failed;

    /// <summary>
    /// Gets whether the deployment was cancelled.
    /// </summary>
    public bool IsCancelled => CurrentStage == DeploymentStage.Cancelled;
}
