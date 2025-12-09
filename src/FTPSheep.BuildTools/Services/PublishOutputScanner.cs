using System.Text.RegularExpressions;
using FTPSheep.BuildTools.Models;

namespace FTPSheep.BuildTools.Services;

/// <summary>
/// Scans publish output folders and collects file metadata.
/// </summary>
public class PublishOutputScanner {
    private readonly List<string> defaultExclusionPatterns = [
        "*.pdb", // Debug symbols
        "*.xml", // XML documentation files
        "*.map", // Source maps
        ".git/**", // Git repository
        ".vs/**", // Visual Studio cache
        "obj/**", // Build intermediate files
        "*.vshost.*", // VS hosting process
        "*.manifest"
    ];

    /// <summary>
    /// Scans a publish output folder and collects file metadata.
    /// </summary>
    /// <param name="publishPath">The path to the publish output folder.</param>
    /// <param name="exclusionPatterns">Optional exclusion patterns (globs). If null, uses default patterns.</param>
    /// <param name="validateOutput">Whether to validate the output for common issues.</param>
    /// <returns>The publish output with file metadata.</returns>
    public PublishOutput ScanPublishOutput(
        string publishPath,
        List<string>? exclusionPatterns = null,
        bool validateOutput = true) {
        if(string.IsNullOrWhiteSpace(publishPath)) {
            throw new ArgumentNullException(nameof(publishPath));
        }

        if(!Directory.Exists(publishPath)) {
            throw new DirectoryNotFoundException($"Publish output directory not found: {publishPath}");
        }

        var output = new PublishOutput {
            RootPath = Path.GetFullPath(publishPath)
        };

        var patterns = exclusionPatterns ?? defaultExclusionPatterns;

        // Enumerate all files in the directory
        var allFiles = Directory.EnumerateFiles(publishPath, "*", SearchOption.AllDirectories);

        foreach(var file in allFiles) {
            var relativePath = Path.GetRelativePath(publishPath, file);

            // Check if file matches any exclusion pattern
            if(IsExcluded(relativePath, patterns)) {
                continue;
            }

            var fileInfo = new FileInfo(file);
            var metadata = new FileMetadata {
                AbsolutePath = file,
                RelativePath = relativePath,
                FileName = fileInfo.Name,
                Extension = fileInfo.Extension,
                Size = fileInfo.Length,
                LastModified = fileInfo.LastWriteTimeUtc,
                IsDirectory = false
            };

            output.Files.Add(metadata);
        }

        // Validate output if requested
        if(validateOutput) {
            ValidatePublishOutput(output);
        }

        return output;
    }

    /// <summary>
    /// Scans a publish output folder asynchronously.
    /// </summary>
    public async Task<PublishOutput> ScanPublishOutputAsync(
        string publishPath,
        List<string>? exclusionPatterns = null,
        bool validateOutput = true,
        CancellationToken cancellationToken = default) {
        return await Task.Run(() =>
            ScanPublishOutput(publishPath, exclusionPatterns, validateOutput),
            cancellationToken);
    }

    /// <summary>
    /// Checks if a file path matches any exclusion pattern.
    /// </summary>
    private bool IsExcluded(string relativePath, List<string> patterns) {
        foreach(var pattern in patterns) {
            if(MatchesGlobPattern(relativePath, pattern)) {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Matches a file path against a glob pattern.
    /// </summary>
    private bool MatchesGlobPattern(string path, string pattern) {
        // Convert glob pattern to regex
        // Simple implementation for common patterns
        var regexPattern = "^" + Regex.Escape(pattern)
            .Replace(@"\*\*\/", ".*")  // **/ matches any number of directories
            .Replace(@"\*", "[^/\\\\]*")  // * matches anything except path separators
            .Replace(@"\?", ".")       // ? matches single character
            + "$";

        var regex = new Regex(regexPattern, RegexOptions.IgnoreCase);

        // Normalize path separators for matching
        var normalizedPath = path.Replace('\\', '/');
        var normalizedPattern = pattern.Replace('\\', '/');

        // Try direct regex match
        if(regex.IsMatch(normalizedPath)) {
            return true;
        }

        // Also check with the original pattern escaping
        regexPattern = "^" + Regex.Escape(normalizedPattern)
            .Replace(@"\*\*/", ".*")
            .Replace(@"\*", "[^/]*")
            .Replace(@"\?", ".")
            + "$";

        regex = new Regex(regexPattern, RegexOptions.IgnoreCase);
        return regex.IsMatch(normalizedPath);
    }

    /// <summary>
    /// Validates the publish output for common issues.
    /// </summary>
    private void ValidatePublishOutput(PublishOutput output) {
        // Check if there are any files at all
        if(output.FileCount == 0) {
            output.Errors.Add("No files found in publish output. The build may have failed.");
            return;
        }

        // Check for web.config in web applications
        var hasWebConfig = output.Files.Any(f => f.IsWebConfig);
        var hasAssemblies = output.Files.Any(f => f.IsAssembly);

        // If we have assemblies but no web.config, it might be a web app issue
        if(hasAssemblies && !hasWebConfig) {
            // Check if any HTML files exist (suggesting it's a web app)
            var hasHtmlFiles = output.Files.Any(f =>
                f.Extension.Equals(".html", StringComparison.OrdinalIgnoreCase) ||
                f.Extension.Equals(".htm", StringComparison.OrdinalIgnoreCase));

            if(hasHtmlFiles) {
                output.Warnings.Add(
                    "Web application detected but web.config is missing. " +
                    "This may cause deployment issues on IIS.");
            }
        }

        // Check for suspiciously large files
        var largeFiles = output.Files.Where(f => f.Size > 100 * 1024 * 1024).ToList(); // > 100 MB
        if(largeFiles.Any()) {
            foreach(var file in largeFiles) {
                output.Warnings.Add(
                    $"Large file detected: {file.RelativePath} ({file.FormattedSize}). " +
                    "This may slow down deployment.");
            }
        }

        // Check for development files that shouldn't be deployed
        var devFiles = output.Files.Where(f =>
            f.FileName.Equals("appsettings.Development.json", StringComparison.OrdinalIgnoreCase) ||
            f.FileName.Equals("launchSettings.json", StringComparison.OrdinalIgnoreCase))
            .ToList();

        if(devFiles.Any()) {
            foreach(var file in devFiles) {
                output.Warnings.Add(
                    $"Development file detected: {file.RelativePath}. " +
                    "Consider excluding this from production deployments.");
            }
        }

        // Check for missing assemblies (no DLL or EXE files)
        if(!hasAssemblies) {
            output.Warnings.Add(
                "No assemblies (.dll or .exe) found in publish output. " +
                "This may not be a complete build.");
        }

        // Check total size
        if(output.TotalSize == 0) {
            output.Errors.Add("Total size is 0 bytes. The publish output appears to be empty.");
        }
    }
}
