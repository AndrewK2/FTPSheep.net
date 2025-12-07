namespace FTPSheep.Core.Models;

/// <summary>
/// Defines the verbosity levels for console and log output.
/// </summary>
public enum LogVerbosity
{
    /// <summary>
    /// Minimal output - only critical messages and final results.
    /// Suitable for CI/CD and automated scenarios.
    /// </summary>
    Minimal = 0,

    /// <summary>
    /// Normal output - standard progress and summaries (default).
    /// Shows key deployment stages and results.
    /// </summary>
    Normal = 1,

    /// <summary>
    /// Verbose output - detailed file-level operations.
    /// Shows individual file uploads and detailed progress.
    /// </summary>
    Verbose = 2,

    /// <summary>
    /// Debug output - all internal operations and decisions.
    /// Maximum detail for troubleshooting.
    /// </summary>
    Debug = 3
}
