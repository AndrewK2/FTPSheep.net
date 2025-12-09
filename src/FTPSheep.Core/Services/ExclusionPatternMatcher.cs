using System.Text.RegularExpressions;

namespace FTPSheep.Core.Services;

/// <summary>
/// Service for matching file paths against exclusion patterns using glob syntax.
/// </summary>
public class ExclusionPatternMatcher {
    /// <summary>
    /// Default exclusion patterns for common folders and files that should not be deleted during cleanup.
    /// </summary>
    public static readonly string[] DefaultExclusionPatterns = new[] {
        "App_Data/**",           // User data folder
        "uploads/**",            // Common uploads folder
        "logs/**",               // Log files folder
        "*.log",                 // Log files anywhere
        ".git/**",               // Git repository
        ".vs/**",                // Visual Studio folder
        "node_modules/**",       // Node.js dependencies
        "appsettings.*.json",    // Environment-specific settings
        "web.config"             // IIS configuration (preserve transformations)
    };

    private readonly List<Regex> compiledPatterns;
    private readonly List<string> patterns;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExclusionPatternMatcher"/> class.
    /// </summary>
    /// <param name="patterns">The exclusion patterns to use (glob syntax).</param>
    public ExclusionPatternMatcher(IEnumerable<string> patterns) {
        if(patterns == null) {
            throw new ArgumentNullException(nameof(patterns));
        }

        this.patterns = patterns.ToList();
        this.compiledPatterns = this.patterns
            .Select(pattern => ConvertGlobToRegex(pattern))
            .ToList();
    }

    /// <summary>
    /// Initializes a new instance with default exclusion patterns.
    /// </summary>
    public ExclusionPatternMatcher()
        : this(DefaultExclusionPatterns) {
    }

    /// <summary>
    /// Gets the exclusion patterns.
    /// </summary>
    public IReadOnlyList<string> Patterns => patterns.AsReadOnly();

    /// <summary>
    /// Checks if a file path matches any exclusion pattern.
    /// </summary>
    /// <param name="relativePath">The relative file path to check (use forward slashes).</param>
    /// <returns>True if the path matches any exclusion pattern; otherwise, false.</returns>
    public bool IsExcluded(string relativePath) {
        if(string.IsNullOrWhiteSpace(relativePath)) {
            return false;
        }

        // Normalize path separators to forward slashes
        var normalizedPath = relativePath.Replace('\\', '/');

        // Check against all compiled patterns
        return compiledPatterns.Any(regex => regex.IsMatch(normalizedPath));
    }

    /// <summary>
    /// Filters a list of file paths, removing those that match exclusion patterns.
    /// </summary>
    /// <param name="paths">The file paths to filter.</param>
    /// <returns>The filtered list of paths that do not match any exclusion pattern.</returns>
    public IEnumerable<string> FilterExcluded(IEnumerable<string> paths) {
        if(paths == null) {
            throw new ArgumentNullException(nameof(paths));
        }

        return paths.Where(path => !IsExcluded(path));
    }

    /// <summary>
    /// Converts a glob pattern to a regular expression.
    /// </summary>
    /// <param name="globPattern">The glob pattern.</param>
    /// <returns>A compiled regular expression.</returns>
    private static Regex ConvertGlobToRegex(string globPattern) {
        if(string.IsNullOrWhiteSpace(globPattern)) {
            throw new ArgumentException("Glob pattern cannot be null or whitespace.", nameof(globPattern));
        }

        // Normalize path separators to forward slashes
        var pattern = globPattern.Replace('\\', '/');

        // Escape special regex characters except *, ?, and /
        var regexPattern = Regex.Escape(pattern)
            .Replace("\\*\\*/", "DOUBLE_STAR_SLASH")  // Temporarily replace **/ to preserve it
            .Replace("\\*\\*", "DOUBLE_STAR")         // Temporarily replace ** to preserve it
            .Replace("\\*", "[^/]*")                   // * matches any characters except /
            .Replace("\\?", "[^/]")                    // ? matches any single character except /
            .Replace("DOUBLE_STAR_SLASH", "(.*/)?")   // **/ matches zero or more directories
            .Replace("DOUBLE_STAR", ".*");             // ** matches any characters including /

        // Anchor the pattern to match the entire path
        regexPattern = "^" + regexPattern + "$";

        return new Regex(regexPattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
    }

    /// <summary>
    /// Creates a matcher with combined default and custom patterns.
    /// </summary>
    /// <param name="customPatterns">Additional custom patterns to include.</param>
    /// <returns>A new exclusion pattern matcher.</returns>
    public static ExclusionPatternMatcher CreateWithDefaults(IEnumerable<string>? customPatterns = null) {
        var allPatterns = DefaultExclusionPatterns.ToList();

        if(customPatterns != null) {
            allPatterns.AddRange(customPatterns);
        }

        return new ExclusionPatternMatcher(allPatterns);
    }

    /// <summary>
    /// Tests if a glob pattern is valid.
    /// </summary>
    /// <param name="pattern">The pattern to test.</param>
    /// <returns>True if the pattern is valid; otherwise, false.</returns>
    public static bool IsValidPattern(string pattern) {
        if(string.IsNullOrWhiteSpace(pattern)) {
            return false;
        }

        try {
            ConvertGlobToRegex(pattern);
            return true;
        } catch {
            return false;
        }
    }
}
