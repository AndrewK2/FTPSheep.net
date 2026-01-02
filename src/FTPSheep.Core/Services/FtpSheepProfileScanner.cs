using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace FTPSheep.Core.Services;

/// <summary>
/// Scans directories for .ftpsheep profile files with safety checks and limits.
/// </summary>
public class FtpSheepProfileScanner {
    private readonly ILogger<FtpSheepProfileScanner> _logger;

    public FtpSheepProfileScanner(ILogger<FtpSheepProfileScanner>? logger = null) {
        _logger = logger ?? NullLogger<FtpSheepProfileScanner>.Instance;
    }

    /// <summary>
    /// Discovers .ftpsheep profile files in the specified directory and subdirectories.
    /// </summary>
    /// <param name="searchPath">The directory to start searching from</param>
    /// <param name="maxDepth">Maximum directory depth to search (default: 10)</param>
    /// <param name="maxFiles">Maximum number of .ftpsheep files to find (default: 500)</param>
    /// <returns>List of absolute paths to .ftpsheep files</returns>
    public List<string> DiscoverProfiles(string searchPath, int maxDepth = 10, int maxFiles = 500) {
        if(string.IsNullOrWhiteSpace(searchPath)) {
            throw new ArgumentException("Search path cannot be null or empty", nameof(searchPath));
        }

        if(!Directory.Exists(searchPath)) {
            _logger.LogDebug("Search path does not exist: {SearchPath}", searchPath);
            return new List<string>();
        }

        var results = new List<string>();
        var filesFound = 0;

        _logger.LogDebug("Starting profile discovery in {SearchPath} (maxDepth: {MaxDepth}, maxFiles: {MaxFiles})",
            searchPath, maxDepth, maxFiles);

        ScanDirectory(searchPath, results, 0, maxDepth, maxFiles, ref filesFound);

        _logger.LogInformation("Profile discovery completed. Found {Count} .ftpsheep files", results.Count);

        return results;
    }

    /// <summary>
    /// Checks if the specified path is a Windows system directory.
    /// </summary>
    /// <param name="path">The path to check</param>
    /// <param name="warningMessage">Output parameter containing the warning message if it's a system directory</param>
    /// <returns>True if the path is a system directory, false otherwise</returns>
    public bool IsSystemDirectory(string path, out string? warningMessage) {
        if(string.IsNullOrWhiteSpace(path)) {
            warningMessage = null;
            return false;
        }

        try {
            var fullPath = Path.GetFullPath(path);

            // Check if filesystem root
            if(IsFileSystemRoot(fullPath)) {
                warningMessage = $"You are at the root of a drive ({fullPath})";
                return true;
            }

            // Check known system paths using Environment.SpecialFolder
            var systemPaths = new Dictionary<string, string> {
                { Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Windows system directory" },
                { Environment.GetFolderPath(Environment.SpecialFolder.System), "Windows System32 directory" },
                { Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Program Files directory" },
                { Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Program Files (x86) directory" },
                { Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "ProgramData directory" }
            };

            foreach(var (sysPath, description) in systemPaths) {
                if(string.IsNullOrEmpty(sysPath)) {
                    continue;
                }

                if(fullPath.Equals(sysPath, StringComparison.OrdinalIgnoreCase) ||
                   fullPath.StartsWith(sysPath + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)) {
                    warningMessage = $"This is a {description}: {sysPath}";
                    return true;
                }
            }

            // Check if at C:\Users root (but allow subdirectories like C:\Users\Andrew\Projects)
            var userProfileRoot = Path.GetDirectoryName(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));

            if(!string.IsNullOrEmpty(userProfileRoot) &&
               fullPath.Equals(userProfileRoot, StringComparison.OrdinalIgnoreCase)) {
                warningMessage = "This is the Users root directory";
                return true;
            }

            warningMessage = null;
            return false;
        } catch(Exception ex) {
            _logger.LogWarning(ex, "Failed to check if path is system directory: {Path}", path);
            warningMessage = null;
            return false;
        }
    }

    /// <summary>
    /// Checks if the specified path is a filesystem root (e.g., C:\, D:\).
    /// </summary>
    /// <param name="path">The path to check</param>
    /// <returns>True if the path is a filesystem root, false otherwise</returns>
    public bool IsFileSystemRoot(string path) {
        if(string.IsNullOrWhiteSpace(path)) {
            return false;
        }

        try {
            var fullPath = Path.GetFullPath(path);

            // Check if it matches the pattern of a drive root (e.g., "C:\", "D:\")
            // Path should be exactly 3 characters: drive letter + colon + separator
            if(fullPath.Length == 3 &&
               char.IsLetter(fullPath[0]) &&
               fullPath[1] == ':' &&
               fullPath[2] == Path.DirectorySeparatorChar) {
                return true;
            }

            // Also check using Path.GetPathRoot
            var pathRoot = Path.GetPathRoot(fullPath);

            return !string.IsNullOrEmpty(pathRoot) && fullPath.Equals(pathRoot, StringComparison.OrdinalIgnoreCase);
        } catch(Exception ex) {
            _logger.LogWarning(ex, "Failed to check if path is filesystem root: {Path}", path);
            return false;
        }
    }

    /// <summary>
    /// Recursively scans a directory for .ftpsheep files with depth and count limits.
    /// </summary>
    private void ScanDirectory(
        string directory,
        List<string> results,
        int currentDepth,
        int maxDepth,
        int maxFiles,
        ref int filesFound) {
        // Stop if max depth reached
        if(currentDepth > maxDepth) {
            _logger.LogDebug("Max depth ({MaxDepth}) reached at {Directory}", maxDepth, directory);
            return;
        }

        // Stop if max files found
        if(filesFound >= maxFiles) {
            _logger.LogDebug("Max files ({MaxFiles}) limit reached", maxFiles);
            return;
        }

        try {
            // Find .ftpsheep files in current directory
            var files = Directory.GetFiles(directory, "*.ftpsheep", SearchOption.TopDirectoryOnly);

            foreach(var file in files) {
                results.Add(file);
                filesFound++;

                _logger.LogDebug("Found .ftpsheep file: {FilePath}", file);

                if(filesFound >= maxFiles) {
                    _logger.LogInformation("Reached max files limit ({MaxFiles})", maxFiles);
                    return;
                }
            }

            // Recurse into subdirectories
            var subdirs = Directory.GetDirectories(directory);

            foreach(var subdir in subdirs) {
                // Skip hidden/system directories
                try {
                    var dirInfo = new DirectoryInfo(subdir);

                    if(dirInfo.Attributes.HasFlag(FileAttributes.Hidden)) {
                        _logger.LogDebug("Skipping hidden directory: {Directory}", subdir);
                        continue;
                    }

                    if(dirInfo.Attributes.HasFlag(FileAttributes.System)) {
                        _logger.LogDebug("Skipping system directory: {Directory}", subdir);
                        continue;
                    }
                } catch(Exception ex) {
                    _logger.LogDebug(ex, "Error checking directory attributes for {Directory}, skipping", subdir);
                    continue;
                }

                // Recurse into subdirectory
                ScanDirectory(subdir, results, currentDepth + 1, maxDepth, maxFiles, ref filesFound);

                if(filesFound >= maxFiles) {
                    return;
                }
            }
        } catch(UnauthorizedAccessException ex) {
            _logger.LogDebug(ex, "Access denied to directory: {Directory}", directory);
        } catch(PathTooLongException ex) {
            _logger.LogDebug(ex, "Path too long: {Directory}", directory);
        } catch(Exception ex) {
            _logger.LogWarning(ex, "Error scanning directory: {Directory}", directory);
        }
    }
}
