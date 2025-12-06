namespace FTPSheep.Core.Models;

/// <summary>
/// Represents a deployment profile configuration.
/// </summary>
public sealed class DeploymentProfile
{
    /// <summary>
    /// Gets or sets the unique name of the deployment profile.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the FTP server hostname or IP address.
    /// </summary>
    public string Server { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the FTP server port. Default is 21 for FTP, 22 for SFTP.
    /// </summary>
    public int Port { get; set; } = 21;

    /// <summary>
    /// Gets or sets the protocol to use (FTP or SFTP).
    /// </summary>
    public ProtocolType Protocol { get; set; } = ProtocolType.Ftp;

    /// <summary>
    /// Gets or sets the FTP username.
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// Gets or sets the encrypted FTP password. Should not be used directly.
    /// </summary>
    public string? EncryptedPassword { get; set; }

    /// <summary>
    /// Gets or sets the remote path on the FTP server where files will be deployed.
    /// </summary>
    public string RemotePath { get; set; } = "/";

    /// <summary>
    /// Gets or sets the local project path to deploy.
    /// </summary>
    public string ProjectPath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the maximum number of concurrent file uploads.
    /// </summary>
    public int Concurrency { get; set; } = 4;

    /// <summary>
    /// Gets or sets the connection timeout in seconds.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Gets or sets the number of retry attempts for failed operations.
    /// </summary>
    public int RetryCount { get; set; } = 3;

    /// <summary>
    /// Gets or sets the build configuration (Debug, Release, etc.).
    /// </summary>
    public string BuildConfiguration { get; set; } = "Release";

    /// <summary>
    /// Gets or sets the file exclusion patterns (glob patterns).
    /// </summary>
    public List<string> ExclusionPatterns { get; set; } = new();

    /// <summary>
    /// Gets or sets the cleanup mode for obsolete files.
    /// </summary>
    public CleanupMode CleanupMode { get; set; } = CleanupMode.None;

    /// <summary>
    /// Gets or sets a value indicating whether to create app_offline.htm before deployment.
    /// </summary>
    public bool AppOfflineEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the target framework for publishing (e.g., net8.0).
    /// </summary>
    public string? TargetFramework { get; set; }

    /// <summary>
    /// Gets or sets the runtime identifier for publishing (e.g., win-x64).
    /// </summary>
    public string? RuntimeIdentifier { get; set; }
}

/// <summary>
/// Defines the supported FTP protocol types.
/// </summary>
public enum ProtocolType
{
    /// <summary>
    /// File Transfer Protocol.
    /// </summary>
    Ftp,

    /// <summary>
    /// SSH File Transfer Protocol (low priority for V1).
    /// </summary>
    Sftp
}

/// <summary>
/// Defines cleanup modes for obsolete files on the server.
/// </summary>
public enum CleanupMode
{
    /// <summary>
    /// Do not delete any files.
    /// </summary>
    None,

    /// <summary>
    /// Delete files that exist on server but not in the published output.
    /// </summary>
    DeleteObsolete,

    /// <summary>
    /// Delete all files before uploading (clean deployment).
    /// </summary>
    DeleteAll
}
