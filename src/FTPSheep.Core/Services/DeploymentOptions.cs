using FTPSheep.BuildTools.Models;
using FTPSheep.Core.Models;

namespace FTPSheep.Core.Services;

/// <summary>
/// Options for deployment execution.
/// </summary>
public class DeploymentOptions {
    /// <summary>
    /// Gets or sets the profile name to use for deployment.
    /// </summary>
    public string? ProfileName { get; set; }

    /// <summary>
    /// Gets or sets the project path to deploy.
    /// </summary>
    public string? ProjectPath { get; set; }

    /// <summary>
    /// Gets or sets the target server host.
    /// </summary>
    public string? TargetHost { get; set; }

    /// <summary>
    /// Gets or sets whether to use app_offline.htm during deployment.
    /// </summary>
    public bool UseAppOffline { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to enable cleanup mode (delete obsolete files).
    /// </summary>
    public bool CleanupMode { get; set; }

    /// <summary>
    /// Gets or sets whether to skip user confirmation prompts.
    /// </summary>
    public bool SkipConfirmation { get; set; }

    /// <summary>
    /// Gets or sets whether to skip connection validation.
    /// </summary>
    public bool SkipConnectionTest { get; set; }

    /// <summary>
    /// Gets or sets whether this is a dry-run (no actual changes).
    /// </summary>
    public bool DryRun { get; set; }

    /// <summary>
    /// Gets or sets the build configuration (e.g., "Release").
    /// </summary>
    public string BuildConfiguration { get; set; } = "Release";

    /// <summary>
    /// Gets or sets the maximum number of concurrent uploads.
    /// </summary>
    public int MaxConcurrentUploads { get; set; } = 4;

    /// <summary>
    /// Gets or sets a pre-loaded deployment profile (optional).
    /// If provided, skips the LoadProfile stage.
    /// </summary>
    public DeploymentProfile? Profile { get; set; }

    /// <summary>
    /// Gets or sets pre-scanned publish output (optional).
    /// If provided, skips the Build stage.
    /// </summary>
    public PublishOutput? PublishOutput { get; set; }
}