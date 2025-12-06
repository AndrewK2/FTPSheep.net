namespace FTPSheep.Core.Models;

/// <summary>
/// Summary information about a deployment profile for display in listings.
/// </summary>
public sealed class ProfileSummary
{
    /// <summary>
    /// Gets or sets the profile name.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Gets or sets the connection string (e.g., "ftp://ftp.example.com:21").
    /// </summary>
    public required string ConnectionString { get; set; }

    /// <summary>
    /// Gets or sets the path to the project file being deployed.
    /// </summary>
    public string? ProjectPath { get; set; }

    /// <summary>
    /// Gets or sets the remote deployment path.
    /// </summary>
    public required string RemotePath { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether credentials are stored for this profile.
    /// </summary>
    public bool HasCredentials { get; set; }

    /// <summary>
    /// Gets or sets the last modification time of the profile file.
    /// </summary>
    public DateTime LastModified { get; set; }

    /// <summary>
    /// Gets or sets the full file path to the profile JSON file.
    /// </summary>
    public required string FilePath { get; set; }
}
