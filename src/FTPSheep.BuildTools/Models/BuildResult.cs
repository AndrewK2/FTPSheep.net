namespace FTPSheep.BuildTools.Models;

/// <summary>
/// Represents the result of a build operation.
/// </summary>
public class BuildResult {
    /// <summary>
    /// Gets or sets a value indicating whether the build was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the exit code from the build process.
    /// </summary>
    public int ExitCode { get; set; }

    /// <summary>
    /// Gets or sets the full output from the build process (stdout).
    /// </summary>
    public string Output { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the error output from the build process (stderr).
    /// </summary>
    public string ErrorOutput { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the list of build errors parsed from the output.
    /// </summary>
    public List<string> Errors { get; set; } = new();

    /// <summary>
    /// Gets or sets the list of build warnings parsed from the output.
    /// </summary>
    public List<string> Warnings { get; set; } = new();

    /// <summary>
    /// Gets or sets the duration of the build operation.
    /// </summary>
    public TimeSpan Duration { get; set; }

    /// <summary>
    /// Gets or sets the path to the output directory (for publish operations).
    /// </summary>
    public string? OutputPath { get; set; }

    /// <summary>
    /// Gets a value indicating whether the build completed with warnings.
    /// </summary>
    public bool HasWarnings => Warnings.Count > 0;

    /// <summary>
    /// Gets a value indicating whether the build completed with errors.
    /// </summary>
    public bool HasErrors => Errors.Count > 0;

    /// <summary>
    /// Creates a successful build result.
    /// </summary>
    public static BuildResult Successful(string output, TimeSpan duration, string? outputPath = null) {
        return new BuildResult {
            Success = true,
            ExitCode = 0,
            Output = output,
            Duration = duration,
            OutputPath = outputPath
        };
    }

    /// <summary>
    /// Creates a failed build result.
    /// </summary>
    public static BuildResult Failed(int exitCode, string output, string errorOutput, List<string> errors, TimeSpan duration) {
        return new BuildResult {
            Success = false,
            ExitCode = exitCode,
            Output = output,
            ErrorOutput = errorOutput,
            Errors = errors,
            Duration = duration
        };
    }
}
