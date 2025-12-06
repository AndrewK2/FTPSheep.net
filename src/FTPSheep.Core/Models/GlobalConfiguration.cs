namespace FTPSheep.Core.Models;

/// <summary>
/// Global configuration settings that apply as defaults to all deployment profiles.
/// </summary>
public sealed class GlobalConfiguration
{
    /// <summary>
    /// Gets or sets the default number of concurrent file transfers.
    /// </summary>
    public int DefaultConcurrency { get; set; } = 4;

    /// <summary>
    /// Gets or sets the default number of retry attempts for failed operations.
    /// </summary>
    public int DefaultRetryCount { get; set; } = 3;

    /// <summary>
    /// Gets or sets the default timeout in seconds for server operations.
    /// </summary>
    public int DefaultTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Gets or sets the default file exclusion patterns applied to all deployments.
    /// </summary>
    public List<string> DefaultExclusionPatterns { get; set; } = new()
    {
        "**/.git/**",
        "**/.vs/**",
        "**/.vscode/**",
        "**/obj/**",
        "**/bin/Debug/**",
        "**/*.user",
        "**/*.suo"
    };

    /// <summary>
    /// Gets or sets the default build configuration.
    /// </summary>
    public string DefaultBuildConfiguration { get; set; } = "Release";

    /// <summary>
    /// Gets or sets a value indicating whether verbose logging is enabled.
    /// </summary>
    public bool VerboseLogging { get; set; } = false;

    /// <summary>
    /// Gets or sets a custom profile storage path. If null, the default %APPDATA%\.ftpsheep\profiles location is used.
    /// </summary>
    public string? ProfileStoragePath { get; set; }

    /// <summary>
    /// Creates a new instance of <see cref="GlobalConfiguration"/> with default values.
    /// </summary>
    /// <returns>A new <see cref="GlobalConfiguration"/> instance with default settings.</returns>
    public static GlobalConfiguration CreateDefault()
    {
        return new GlobalConfiguration();
    }
}
