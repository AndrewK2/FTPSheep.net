namespace FTPSheep.Core.Interfaces;

/// <summary>
/// Defines the contract for tracking deployment progress.
/// </summary>
public interface IProgressTracker {
    /// <summary>
    /// Reports overall deployment progress.
    /// </summary>
    /// <param name="stage">The current deployment stage.</param>
    /// <param name="percentComplete">The percentage complete (0-100).</param>
    /// <param name="message">Optional status message.</param>
    void ReportProgress(DeploymentStage stage, double percentComplete, string? message = null);

    /// <summary>
    /// Updates the current status message.
    /// </summary>
    /// <param name="message">The status message.</param>
    void UpdateStatus(string message);

    /// <summary>
    /// Reports that a file has been uploaded.
    /// </summary>
    /// <param name="fileName">The name of the file.</param>
    /// <param name="fileSize">The size of the file in bytes.</param>
    /// <param name="index">The index of this file in the total.</param>
    /// <param name="total">The total number of files.</param>
    void ReportFileUploaded(string fileName, long fileSize, int index, int total);

    /// <summary>
    /// Reports that a file upload has started.
    /// </summary>
    /// <param name="fileName">The name of the file.</param>
    /// <param name="fileSize">The size of the file in bytes.</param>
    void ReportFileUploadStarted(string fileName, long fileSize);

    /// <summary>
    /// Reports progress for the current file upload.
    /// </summary>
    /// <param name="fileName">The name of the file.</param>
    /// <param name="bytesTransferred">The number of bytes transferred.</param>
    /// <param name="totalBytes">The total file size in bytes.</param>
    void ReportFileProgress(string fileName, long bytesTransferred, long totalBytes);

    /// <summary>
    /// Reports an error during deployment.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="exception">Optional exception that caused the error.</param>
    void ReportError(string message, Exception? exception = null);

    /// <summary>
    /// Reports a warning during deployment.
    /// </summary>
    /// <param name="message">The warning message.</param>
    void ReportWarning(string message);

    /// <summary>
    /// Reports that the deployment has completed.
    /// </summary>
    /// <param name="success">Whether the deployment was successful.</param>
    /// <param name="message">Optional completion message.</param>
    void ReportComplete(bool success, string? message = null);
}

/// <summary>
/// Defines the stages of a deployment operation.
/// </summary>
public enum DeploymentStage {
    /// <summary>
    /// Initializing the deployment.
    /// </summary>
    Initializing,

    /// <summary>
    /// Building the project.
    /// </summary>
    Building,

    /// <summary>
    /// Publishing the project.
    /// </summary>
    Publishing,

    /// <summary>
    /// Connecting to the FTP server.
    /// </summary>
    Connecting,

    /// <summary>
    /// Uploading app_offline.htm to take the site offline.
    /// </summary>
    TakingOffline,

    /// <summary>
    /// Uploading files to the server.
    /// </summary>
    Uploading,

    /// <summary>
    /// Cleaning up obsolete files.
    /// </summary>
    CleaningUp,

    /// <summary>
    /// Removing app_offline.htm to bring the site back online.
    /// </summary>
    BringingOnline,

    /// <summary>
    /// Completing the deployment.
    /// </summary>
    Completing,

    /// <summary>
    /// Deployment completed.
    /// </summary>
    Completed
}
