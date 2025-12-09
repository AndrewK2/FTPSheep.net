namespace FTPSheep.Protocols.Models;

/// <summary>
/// Represents a file upload task.
/// </summary>
public class UploadTask {
    /// <summary>
    /// Gets or sets the local file path.
    /// </summary>
    public required string LocalPath { get; init; }

    /// <summary>
    /// Gets or sets the remote file path (relative to root).
    /// </summary>
    public required string RemotePath { get; init; }

    /// <summary>
    /// Gets or sets the file size in bytes.
    /// </summary>
    public long FileSize { get; init; }

    /// <summary>
    /// Gets or sets whether to overwrite existing files.
    /// </summary>
    public bool Overwrite { get; init; } = true;

    /// <summary>
    /// Gets or sets whether to create remote directories if they don't exist.
    /// </summary>
    public bool CreateRemoteDir { get; init; } = true;

    /// <summary>
    /// Gets or sets the task priority (lower value = higher priority).
    /// </summary>
    public int Priority { get; init; } = 0;

    /// <summary>
    /// Gets or sets optional metadata for this upload task.
    /// </summary>
    public Dictionary<string, object> Metadata { get; init; } = new();
}
