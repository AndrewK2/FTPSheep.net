namespace FTPSheep.BuildTools.Models;

/// <summary>
/// Represents the output of a publish operation with file metadata.
/// </summary>
public class PublishOutput {
    /// <summary>
    /// Gets or sets the root path of the publish output.
    /// </summary>
    public string RootPath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the list of files in the publish output.
    /// </summary>
    public List<FileMetadata> Files { get; set; } = [];

    /// <summary>
    /// Gets or sets the list of validation warnings.
    /// </summary>
    public List<string> Warnings { get; set; } = [];

    /// <summary>
    /// Gets or sets the list of validation errors.
    /// </summary>
    public List<string> Errors { get; set; } = [];

    /// <summary>
    /// Gets the total number of files.
    /// </summary>
    public int FileCount => Files.Count;

    /// <summary>
    /// Gets the total size of all files in bytes.
    /// </summary>
    public long TotalSize => Files.Sum(f => f.Size);

    /// <summary>
    /// Gets a human-readable total size string.
    /// </summary>
    public string FormattedTotalSize => FormatBytes(TotalSize);

    /// <summary>
    /// Gets a value indicating whether the output has warnings.
    /// </summary>
    public bool HasWarnings => Warnings.Count > 0;

    /// <summary>
    /// Gets a value indicating whether the output has errors.
    /// </summary>
    public bool HasErrors => Errors.Count > 0;

    /// <summary>
    /// Gets a value indicating whether the output is valid.
    /// </summary>
    public bool IsValid => !HasErrors;

    /// <summary>
    /// Gets the files sorted by size (small files first for optimal upload).
    /// </summary>
    public IEnumerable<FileMetadata> FilesSortedBySize =>
        Files.OrderBy(f => f.Size);

    /// <summary>
    /// Gets the files sorted by path for display purposes.
    /// </summary>
    public IEnumerable<FileMetadata> FilesSortedByPath =>
        Files.OrderBy(f => f.RelativePath);

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
