namespace FTPSheep.Core.Models;

/// <summary>
/// Defines the stages of a deployment workflow.
/// </summary>
public enum DeploymentStage {
    /// <summary>
    /// Deployment has not started.
    /// </summary>
    NotStarted = 0,

    /// <summary>
    /// Stage 1: Loading profile and validating configuration.
    /// </summary>
    LoadingProfile = 1,

    /// <summary>
    /// Stage 2: Validating FTP connection and write permissions.
    /// </summary>
    ValidatingConnection = 2,

    /// <summary>
    /// Stage 3: Building and publishing the project.
    /// </summary>
    BuildingProject = 3,

    /// <summary>
    /// Stage 4: Connecting to server and initializing upload engine.
    /// </summary>
    ConnectingToServer = 4,

    /// <summary>
    /// Stage 5: Displaying pre-deployment summary and waiting for confirmation.
    /// </summary>
    PreDeploymentSummary = 5,

    /// <summary>
    /// Stage 6: Uploading app_offline.htm (if enabled).
    /// </summary>
    UploadingAppOffline = 6,

    /// <summary>
    /// Stage 7: Uploading all published files (concurrent).
    /// </summary>
    UploadingFiles = 7,

    /// <summary>
    /// Stage 8: Cleaning up obsolete files (if cleanup mode enabled).
    /// </summary>
    CleaningUpObsoleteFiles = 8,

    /// <summary>
    /// Stage 9: Deleting app_offline.htm (if deployment succeeded).
    /// </summary>
    DeletingAppOffline = 9,

    /// <summary>
    /// Stage 10: Recording deployment history and displaying summary.
    /// </summary>
    RecordingHistory = 10,

    /// <summary>
    /// Deployment completed successfully.
    /// </summary>
    Completed = 11,

    /// <summary>
    /// Deployment failed.
    /// </summary>
    Failed = 12,

    /// <summary>
    /// Deployment was cancelled by user.
    /// </summary>
    Cancelled = 13
}
