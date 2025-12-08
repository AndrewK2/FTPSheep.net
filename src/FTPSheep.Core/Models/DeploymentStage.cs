namespace FTPSheep.Core.Models;

/// <summary>
/// Defines the stages of a deployment workflow.
/// </summary>
public enum DeploymentStage
{
    /// <summary>
    /// Deployment has not started.
    /// </summary>
    NotStarted = 0,

    /// <summary>
    /// Stage 1: Loading profile and validating configuration.
    /// </summary>
    LoadingProfile = 1,

    /// <summary>
    /// Stage 2: Building and publishing the project.
    /// </summary>
    BuildingProject = 2,

    /// <summary>
    /// Stage 3: Connecting to server and validating connection.
    /// </summary>
    ConnectingToServer = 3,

    /// <summary>
    /// Stage 4: Displaying pre-deployment summary and waiting for confirmation.
    /// </summary>
    PreDeploymentSummary = 4,

    /// <summary>
    /// Stage 5: Uploading app_offline.htm (if enabled).
    /// </summary>
    UploadingAppOffline = 5,

    /// <summary>
    /// Stage 6: Uploading all published files (concurrent).
    /// </summary>
    UploadingFiles = 6,

    /// <summary>
    /// Stage 7: Cleaning up obsolete files (if cleanup mode enabled).
    /// </summary>
    CleaningUpObsoleteFiles = 7,

    /// <summary>
    /// Stage 8: Deleting app_offline.htm (if deployment succeeded).
    /// </summary>
    DeletingAppOffline = 8,

    /// <summary>
    /// Stage 9: Recording deployment history and displaying summary.
    /// </summary>
    RecordingHistory = 9,

    /// <summary>
    /// Deployment completed successfully.
    /// </summary>
    Completed = 10,

    /// <summary>
    /// Deployment failed.
    /// </summary>
    Failed = 11,

    /// <summary>
    /// Deployment was cancelled by user.
    /// </summary>
    Cancelled = 12
}
