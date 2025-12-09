namespace FTPSheep.BuildTools.Models;

/// <summary>
/// Represents metadata for a file in the build output.
/// </summary>
public class FileMetadata {
    /// <summary>
    /// Gets or sets the absolute path to the file on disk.
    /// </summary>
    public string AbsolutePath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the relative path from the publish root.
    /// </summary>
    public string RelativePath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the file size in bytes.
    /// </summary>
    public long Size { get; set; }

    /// <summary>
    /// Gets or sets the last modified timestamp (UTC).
    /// </summary>
    public DateTime LastModified { get; set; }

    /// <summary>
    /// Gets or sets the file name.
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the file extension (including the dot).
    /// </summary>
    public string Extension { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether this file is a directory.
    /// </summary>
    public bool IsDirectory { get; set; }

    /// <summary>
    /// Gets a value indicating whether this is a web configuration file.
    /// </summary>
    public bool IsWebConfig =>
        FileName.Equals("web.config", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a value indicating whether this is an assembly file.
    /// </summary>
    public bool IsAssembly =>
        Extension.Equals(".dll", StringComparison.OrdinalIgnoreCase) ||
        Extension.Equals(".exe", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a value indicating whether this is a static web file.
    /// </summary>
    public bool IsStaticWebFile {
        get {
            var ext = Extension.ToLowerInvariant();
            return ext == ".html" || ext == ".htm" || ext == ".css" ||
                   ext == ".js" || ext == ".json" || ext == ".xml" ||
                   ext == ".png" || ext == ".jpg" || ext == ".jpeg" ||
                   ext == ".gif" || ext == ".svg" || ext == ".ico" ||
                   ext == ".woff" || ext == ".woff2" || ext == ".ttf" || ext == ".eot";
        }
    }

    /// <summary>
    /// Gets a human-readable file size string.
    /// </summary>
    public string FormattedSize => FormatBytes(Size);

    /// <summary>
    /// Formats bytes into a human-readable string.
    /// </summary>
    private static string FormatBytes(long bytes) {
        string[] sizes = ["B", "KB", "MB", "GB", "TB"];
        double len = bytes;
        var order = 0;

        while(len >= 1024 && order < sizes.Length - 1) {
            order++;
            len /= 1024;
        }

        return $"{len:0.##} {sizes[order]}";
    }
}
