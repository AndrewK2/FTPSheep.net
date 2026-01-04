using System.Text.Json.Serialization;

namespace FTPSheep.Core.Models;

/// <summary>
/// Represents a deployment profile configuration.
/// </summary>
public sealed class DeploymentProfile {
    /// <summary>
    /// Gets or sets the unique name of the deployment profile.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the server connection settings.
    /// </summary>
    public ServerConnection Connection { get; set; } = new();

    /// <summary>
    /// Gets or sets the FTP username.
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// Gets or sets the encrypted FTP password. Should not be used directly.
    /// </summary>
    public string? EncryptedPassword { get; set; }

    /// <summary>
    /// Gets or sets the plain-text password. This is never persisted to disk and is only used at runtime.
    /// </summary>
    [JsonIgnore]
    public string? Password { get; set; }

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
    /// Gets or sets the number of retry attempts for failed operations.
    /// </summary>
    public int RetryCount { get; set; } = 3;

    /// <summary>
    /// Gets or sets the build configuration settings.
    /// </summary>
    public BuildConfiguration Build { get; set; } = new();

    /// <summary>
    /// Gets or sets the file exclusion patterns (glob patterns).
    /// </summary>
    public List<string> ExclusionPatterns { get; set; } = [];

    /// <summary>
    /// Gets or sets the cleanup mode for obsolete files.
    /// </summary>
    public CleanupMode CleanupMode { get; set; } = CleanupMode.None;

    /// <summary>
    /// Gets or sets a value indicating whether to create app_offline.htm before deployment.
    /// </summary>
    public bool AppOfflineEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the custom app_offline.htm template content.
    /// If null or empty, the default template will be used.
    /// </summary>
    public string? AppOfflineTemplate { get; set; }

    /// <summary>
    /// Gets or sets the URL to open in browser after successful deployment.
    /// Maps to SiteUrlToLaunchAfterPublish in .pubxml files.
    /// </summary>
    public string? SiteUrlToLaunchAfterPublish { get; set; }

    /// <summary>
    /// Gets or sets whether to automatically open the URL after successful deployment.
    /// Maps to LaunchSiteAfterPublish in .pubxml files. Defaults to false.
    /// </summary>
    public bool LaunchSiteAfterPublish { get; set; } = false;

    // Obsolete properties for backward compatibility
    // These will be removed in V2.0

    /// <summary>
    /// Gets or sets the FTP server hostname or IP address.
    /// </summary>
    [Obsolete("Use Connection.Host instead. This property will be removed in V2.0.")]
    [JsonIgnore]
    public string Server {
        get => Connection.Host;
        set => Connection.Host = value;
    }

    /// <summary>
    /// Gets or sets the FTP server port. Default is 21 for FTP, 22 for SFTP.
    /// </summary>
    [Obsolete("Use Connection.Port instead. This property will be removed in V2.0.")]
    [JsonIgnore]
    public int Port {
        get => Connection.Port;
        set => Connection.Port = value;
    }

    /// <summary>
    /// Gets or sets the protocol to use (FTP or SFTP).
    /// </summary>
    [Obsolete("Use Connection.Protocol instead. This property will be removed in V2.0.")]
    [JsonIgnore]
    public ProtocolType Protocol {
        get => Connection.Protocol;
        set => Connection.Protocol = value;
    }

    /// <summary>
    /// Gets or sets the connection timeout in seconds.
    /// </summary>
    [Obsolete("Use Connection.TimeoutSeconds instead. This property will be removed in V2.0.")]
    [JsonIgnore]
    public int TimeoutSeconds {
        get => Connection.TimeoutSeconds;
        set => Connection.TimeoutSeconds = value;
    }

    /// <summary>
    /// Gets or sets the build configuration (Debug, Release, etc.).
    /// </summary>
    [Obsolete("Use Build.Configuration instead. This property will be removed in V2.0.")]
    [JsonIgnore]
    public string BuildConfiguration {
        get => Build.Configuration;
        set => Build.Configuration = value;
    }

    /// <summary>
    /// Gets or sets the target framework for publishing (e.g., net8.0).
    /// </summary>
    [Obsolete("Use Build.TargetFramework instead. This property will be removed in V2.0.")]
    [JsonIgnore]
    public string? TargetFramework {
        get => Build.TargetFramework;
        set => Build.TargetFramework = value;
    }

    /// <summary>
    /// Gets or sets the runtime identifier for publishing (e.g., win-x64).
    /// </summary>
    [Obsolete("Use Build.RuntimeIdentifier instead. This property will be removed in V2.0.")]
    [JsonIgnore]
    public string? RuntimeIdentifier {
        get => Build.RuntimeIdentifier;
        set => Build.RuntimeIdentifier = value;
    }
}

/// <summary>
/// Defines the supported FTP protocol types.
/// </summary>
public enum ProtocolType {
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
public enum CleanupMode {
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
