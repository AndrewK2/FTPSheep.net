namespace FTPSheep.Core.Exceptions;

/// <summary>
/// Base exception for all deployment-related errors.
/// </summary>
public class DeploymentException : Exception
{
    /// <summary>
    /// Gets the profile name associated with this exception, if applicable.
    /// </summary>
    public string? ProfileName { get; }

    /// <summary>
    /// Gets the deployment phase where the error occurred.
    /// </summary>
    public DeploymentPhase Phase { get; init; }

    /// <summary>
    /// Gets a value indicating whether this error is recoverable/retryable.
    /// </summary>
    public bool IsRetryable { get; init; }

    /// <summary>
    /// Initializes a new instance of the <see cref="DeploymentException"/> class.
    /// </summary>
    public DeploymentException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DeploymentException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public DeploymentException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DeploymentException"/> class with a specified error message and inner exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public DeploymentException(string message, Exception innerException) : base(message, innerException)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DeploymentException"/> class with profile name and phase.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="profileName">The profile name.</param>
    /// <param name="phase">The deployment phase where the error occurred.</param>
    /// <param name="isRetryable">Whether this error is retryable.</param>
    public DeploymentException(string message, string profileName, DeploymentPhase phase, bool isRetryable = false)
        : base(message)
    {
        ProfileName = profileName;
        Phase = phase;
        IsRetryable = isRetryable;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DeploymentException"/> class with profile name, phase, and inner exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="profileName">The profile name.</param>
    /// <param name="phase">The deployment phase where the error occurred.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    /// <param name="isRetryable">Whether this error is retryable.</param>
    public DeploymentException(string message, string profileName, DeploymentPhase phase, Exception innerException, bool isRetryable = false)
        : base(message, innerException)
    {
        ProfileName = profileName;
        Phase = phase;
        IsRetryable = isRetryable;
    }
}

/// <summary>
/// Defines the phases of a deployment operation.
/// </summary>
public enum DeploymentPhase
{
    /// <summary>
    /// Unknown or unspecified phase.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// Initialization phase (loading profile, validating settings).
    /// </summary>
    Initialization = 1,

    /// <summary>
    /// Build phase (compiling and publishing the project).
    /// </summary>
    Build = 2,

    /// <summary>
    /// Connection phase (establishing connection to server).
    /// </summary>
    Connection = 3,

    /// <summary>
    /// Authentication phase (authenticating with the server).
    /// </summary>
    Authentication = 4,

    /// <summary>
    /// Upload phase (transferring files to the server).
    /// </summary>
    Upload = 5,

    /// <summary>
    /// Verification phase (verifying uploaded files).
    /// </summary>
    Verification = 6,

    /// <summary>
    /// Cleanup phase (cleaning up temporary files, closing connections).
    /// </summary>
    Cleanup = 7
}

/// <summary>
/// Exception thrown when a file transfer fails during deployment.
/// </summary>
public class FileTransferException : DeploymentException
{
    /// <summary>
    /// Gets the file path that failed to transfer.
    /// </summary>
    public string? FilePath { get; }

    /// <summary>
    /// Gets the remote path where the file was being transferred.
    /// </summary>
    public string? RemotePath { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="FileTransferException"/> class.
    /// </summary>
    public FileTransferException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FileTransferException"/> class with file paths.
    /// </summary>
    /// <param name="filePath">The local file path.</param>
    /// <param name="remotePath">The remote path.</param>
    public FileTransferException(string filePath, string remotePath)
        : base($"Failed to transfer file '{filePath}' to '{remotePath}'.")
    {
        FilePath = filePath;
        RemotePath = remotePath;
        Phase = DeploymentPhase.Upload;
        IsRetryable = true; // File transfers are often retryable
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FileTransferException"/> class with file paths and inner exception.
    /// </summary>
    /// <param name="filePath">The local file path.</param>
    /// <param name="remotePath">The remote path.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public FileTransferException(string filePath, string remotePath, Exception innerException)
        : base($"Failed to transfer file '{filePath}' to '{remotePath}': {innerException.Message}", innerException)
    {
        FilePath = filePath;
        RemotePath = remotePath;
        Phase = DeploymentPhase.Upload;
        IsRetryable = true;
    }
}

/// <summary>
/// Exception thrown when there is insufficient disk space on the remote server.
/// </summary>
public class InsufficientDiskSpaceException : DeploymentException
{
    /// <summary>
    /// Gets the required disk space in bytes.
    /// </summary>
    public long RequiredBytes { get; }

    /// <summary>
    /// Gets the available disk space in bytes.
    /// </summary>
    public long AvailableBytes { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="InsufficientDiskSpaceException"/> class.
    /// </summary>
    public InsufficientDiskSpaceException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="InsufficientDiskSpaceException"/> class with required and available space.
    /// </summary>
    /// <param name="requiredBytes">The required disk space in bytes.</param>
    /// <param name="availableBytes">The available disk space in bytes.</param>
    public InsufficientDiskSpaceException(long requiredBytes, long availableBytes)
        : base($"Insufficient disk space on remote server. Required: {requiredBytes / 1024 / 1024} MB, Available: {availableBytes / 1024 / 1024} MB")
    {
        RequiredBytes = requiredBytes;
        AvailableBytes = availableBytes;
        Phase = DeploymentPhase.Upload;
        IsRetryable = false; // Disk space issues are not retryable without intervention
    }
}
