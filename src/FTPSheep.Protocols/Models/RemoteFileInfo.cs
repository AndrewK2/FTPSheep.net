namespace FTPSheep.Protocols.Models;

/// <summary>
/// Represents information about a remote file or directory.
/// Protocol-agnostic model for file listings.
/// </summary>
public class RemoteFileInfo {
    /// <summary>
    /// Gets or sets the full path of the file or directory.
    /// </summary>
    public string FullPath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the name of the file or directory.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether this is a directory.
    /// </summary>
    public bool IsDirectory { get; set; }

    /// <summary>
    /// Gets or sets the size of the file in bytes (0 for directories).
    /// </summary>
    public long Size { get; set; }

    /// <summary>
    /// Gets or sets the last modified date/time (UTC).
    /// </summary>
    public DateTime? LastModified { get; set; }

    /// <summary>
    /// Gets or sets the file permissions (Unix-style, e.g., 644, 755).
    /// May be null if not supported by the server.
    /// </summary>
    public int? Permissions { get; set; }

    /// <summary>
    /// Gets a formatted size string (e.g., "1.5 MB").
    /// </summary>
    public string FormattedSize {
        get {
            if(IsDirectory) return "<DIR>";

            const long KB = 1024;
            const long MB = KB * 1024;
            const long GB = MB * 1024;

            if(Size >= GB) return $"{Size / (double)GB:F2} GB";
            if(Size >= MB) return $"{Size / (double)MB:F2} MB";
            if(Size >= KB) return $"{Size / (double)KB:F2} KB";
            return $"{Size} B";
        }
    }

    /// <summary>
    /// Gets the file type description.
    /// </summary>
    public string FileType => IsDirectory ? "Directory" : "File";
}
