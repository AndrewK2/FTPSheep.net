using FTPSheep.BuildTools.Models;

namespace FTPSheep.Core.Services;

/// <summary>
/// Service for comparing local and remote file sets to identify obsolete files for cleanup.
/// </summary>
public class FileComparisonService {
    private readonly ExclusionPatternMatcher exclusionMatcher;

    /// <summary>
    /// Initializes a new instance of the <see cref="FileComparisonService"/> class.
    /// </summary>
    /// <param name="exclusionMatcher">The exclusion pattern matcher to use.</param>
    public FileComparisonService(ExclusionPatternMatcher? exclusionMatcher = null) {
        this.exclusionMatcher = exclusionMatcher ?? new ExclusionPatternMatcher();
    }

    /// <summary>
    /// Compares local and remote file sets to identify obsolete remote files.
    /// </summary>
    /// <param name="localFiles">The local file metadata collection.</param>
    /// <param name="remoteFiles">The remote file paths (relative to deployment root).</param>
    /// <returns>A comparison result containing obsolete files and statistics.</returns>
    public FileComparisonResult CompareFiles(
        IEnumerable<FileMetadata> localFiles,
        IEnumerable<string> remoteFiles) {
        if(localFiles == null) {
            throw new ArgumentNullException(nameof(localFiles));
        }

        if(remoteFiles == null) {
            throw new ArgumentNullException(nameof(remoteFiles));
        }

        var localPaths = new HashSet<string>(
            localFiles.Select(f => f.RelativePath),
            StringComparer.OrdinalIgnoreCase);

        var remotePaths = remoteFiles
            .Select(NormalizePath)
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .ToList();

        var obsoleteFiles = new List<string>();
        var excludedFiles = new List<string>();

        foreach(var remotePath in remotePaths) {
            // Skip if file exists in local set
            if(localPaths.Contains(remotePath)) {
                continue;
            }

            // Check if file is excluded
            if(exclusionMatcher.IsExcluded(remotePath)) {
                excludedFiles.Add(remotePath);
                continue;
            }

            // File is obsolete (exists on server but not in local publish output)
            obsoleteFiles.Add(remotePath);
        }

        return new FileComparisonResult {
            ObsoleteFiles = obsoleteFiles,
            ExcludedFiles = excludedFiles,
            TotalLocalFiles = localPaths.Count,
            TotalRemoteFiles = remotePaths.Count,
            ObsoleteFileCount = obsoleteFiles.Count,
            ExcludedFileCount = excludedFiles.Count
        };
    }

    /// <summary>
    /// Identifies directories that would become empty after deleting obsolete files.
    /// </summary>
    /// <param name="obsoleteFiles">The list of obsolete file paths.</param>
    /// <param name="allRemoteFiles">All remote file paths.</param>
    /// <returns>A list of directory paths that would become empty.</returns>
    public List<string> IdentifyEmptyDirectories(
        IEnumerable<string> obsoleteFiles,
        IEnumerable<string> allRemoteFiles) {
        if(obsoleteFiles == null) {
            throw new ArgumentNullException(nameof(obsoleteFiles));
        }

        if(allRemoteFiles == null) {
            throw new ArgumentNullException(nameof(allRemoteFiles));
        }

        var obsoleteSet = new HashSet<string>(
            obsoleteFiles.Select(NormalizePath),
            StringComparer.OrdinalIgnoreCase);

        var allRemoteSet = new HashSet<string>(
            allRemoteFiles.Select(NormalizePath),
            StringComparer.OrdinalIgnoreCase);

        // Get all directories from obsolete files
        var directories = obsoleteFiles
            .Select(f => Path.GetDirectoryName(NormalizePath(f)))
            .Where(d => !string.IsNullOrWhiteSpace(d))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Cast<string>()
            .ToList();

        var emptyDirectories = new List<string>();

        foreach(var directory in directories) {
            // Get all files in this directory
            var filesInDirectory = allRemoteSet
                .Where(f => {
                    var fileDir = Path.GetDirectoryName(NormalizePath(f));
                    return string.Equals(fileDir, directory, StringComparison.OrdinalIgnoreCase);
                })
                .ToList();

            // If all files in directory are obsolete, directory will be empty
            if(filesInDirectory.All(f => obsoleteSet.Contains(f))) {
                emptyDirectories.Add(NormalizePath(directory));
            }
        }

        return emptyDirectories;
    }

    /// <summary>
    /// Normalizes a file path for comparison (forward slashes, trimmed).
    /// </summary>
    /// <param name="path">The path to normalize.</param>
    /// <returns>The normalized path.</returns>
    public static string NormalizePath(string path) {
        if(string.IsNullOrWhiteSpace(path)) {
            return string.Empty;
        }

        return path.Replace('\\', '/').Trim();
    }
}

/// <summary>
/// Result of a file comparison operation.
/// </summary>
public class FileComparisonResult {
    /// <summary>
    /// Gets or sets the list of obsolete file paths (exist on server but not in local publish).
    /// </summary>
    public List<string> ObsoleteFiles { get; set; } = new();

    /// <summary>
    /// Gets or sets the list of excluded file paths (matched exclusion patterns).
    /// </summary>
    public List<string> ExcludedFiles { get; set; } = new();

    /// <summary>
    /// Gets or sets the total number of local files.
    /// </summary>
    public int TotalLocalFiles { get; set; }

    /// <summary>
    /// Gets or sets the total number of remote files.
    /// </summary>
    public int TotalRemoteFiles { get; set; }

    /// <summary>
    /// Gets or sets the number of obsolete files.
    /// </summary>
    public int ObsoleteFileCount { get; set; }

    /// <summary>
    /// Gets or sets the number of excluded files.
    /// </summary>
    public int ExcludedFileCount { get; set; }

    /// <summary>
    /// Gets a value indicating whether there are obsolete files to clean up.
    /// </summary>
    public bool HasObsoleteFiles => ObsoleteFileCount > 0;

    /// <summary>
    /// Gets a value indicating whether any files were excluded.
    /// </summary>
    public bool HasExcludedFiles => ExcludedFileCount > 0;
}
