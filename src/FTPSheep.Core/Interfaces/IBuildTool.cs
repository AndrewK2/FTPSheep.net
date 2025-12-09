namespace FTPSheep.Core.Interfaces;

/// <summary>
/// Defines the contract for .NET build tools (MSBuild or dotnet CLI).
/// </summary>
public interface IBuildTool {
    /// <summary>
    /// Builds a .NET project.
    /// </summary>
    /// <param name="projectPath">The path to the project file or directory.</param>
    /// <param name="configuration">The build configuration (Debug, Release, etc.).</param>
    /// <param name="outputPath">Optional output path for build artifacts.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A build result containing success status and output.</returns>
    Task<BuildResult> BuildAsync(
        string projectPath,
        string configuration = "Release",
        string? outputPath = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Publishes a .NET project for deployment.
    /// </summary>
    /// <param name="projectPath">The path to the project file or directory.</param>
    /// <param name="configuration">The build configuration (Debug, Release, etc.).</param>
    /// <param name="outputPath">The output path for published files.</param>
    /// <param name="targetFramework">Optional target framework (e.g., net8.0).</param>
    /// <param name="runtime">Optional runtime identifier (e.g., win-x64).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A build result containing success status and output path.</returns>
    Task<BuildResult> PublishAsync(
        string projectPath,
        string configuration,
        string outputPath,
        string? targetFramework = null,
        string? runtime = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets information about a .NET project.
    /// </summary>
    /// <param name="projectPath">The path to the project file or directory.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Project information including target frameworks, output type, etc.</returns>
    Task<ProjectInfo> GetProjectInfoAsync(
        string projectPath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Cleans build output for a project.
    /// </summary>
    /// <param name="projectPath">The path to the project file or directory.</param>
    /// <param name="configuration">The build configuration to clean.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task CleanAsync(
        string projectPath,
        string configuration = "Release",
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents the result of a build or publish operation.
/// </summary>
public sealed class BuildResult {
    /// <summary>
    /// Gets or sets a value indicating whether the build was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the output path where build artifacts were written.
    /// </summary>
    public string? OutputPath { get; set; }

    /// <summary>
    /// Gets or sets the build output messages.
    /// </summary>
    public List<string> OutputMessages { get; set; } = [];

    /// <summary>
    /// Gets or sets the build error messages.
    /// </summary>
    public List<string> ErrorMessages { get; set; } = [];

    /// <summary>
    /// Gets or sets the build warning messages.
    /// </summary>
    public List<string> WarningMessages { get; set; } = [];

    /// <summary>
    /// Gets or sets the build duration.
    /// </summary>
    public TimeSpan Duration { get; set; }

    /// <summary>
    /// Gets or sets the exit code from the build process.
    /// </summary>
    public int ExitCode { get; set; }
}

/// <summary>
/// Represents information about a .NET project.
/// </summary>
public sealed class ProjectInfo {
    /// <summary>
    /// Gets or sets the project file path.
    /// </summary>
    public string ProjectPath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the project name.
    /// </summary>
    public string ProjectName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the target framework(s).
    /// </summary>
    public List<string> TargetFrameworks { get; set; } = [];

    /// <summary>
    /// Gets or sets the output type (Exe, Library, etc.).
    /// </summary>
    public string? OutputType { get; set; }

    /// <summary>
    /// Gets or sets the default namespace.
    /// </summary>
    public string? DefaultNamespace { get; set; }

    /// <summary>
    /// Gets or sets whether this is a web project.
    /// </summary>
    public bool IsWebProject { get; set; }
}
