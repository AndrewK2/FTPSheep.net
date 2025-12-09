using FTPSheep.BuildTools.Models;

namespace FTPSheep.BuildTools.Services;

/// <summary>
/// High-level service for building .NET projects.
/// Automatically selects the appropriate build tool based on project type.
/// </summary>
public class BuildService {
    private readonly ProjectFileParser _parser;
    private readonly ProjectTypeClassifier _classifier;
    private readonly MSBuildExecutor _msbuildExecutor;
    private readonly DotnetCliExecutor _dotnetExecutor;

    /// <summary>
    /// Initializes a new instance of the <see cref="BuildService"/> class.
    /// </summary>
    public BuildService(
        ProjectFileParser? parser = null,
        ProjectTypeClassifier? classifier = null,
        MSBuildExecutor? msbuildExecutor = null,
        DotnetCliExecutor? dotnetExecutor = null) {
        _parser = parser ?? new ProjectFileParser();
        _classifier = classifier ?? new ProjectTypeClassifier();
        _msbuildExecutor = msbuildExecutor ?? new MSBuildExecutor();
        _dotnetExecutor = dotnetExecutor ?? new DotnetCliExecutor();
    }

    /// <summary>
    /// Builds a .NET project.
    /// </summary>
    /// <param name="projectPath">The path to the project file.</param>
    /// <param name="configuration">The build configuration (Debug, Release).</param>
    /// <param name="outputPath">Optional output path.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The build result.</returns>
    public async Task<BuildResult> BuildAsync(
        string projectPath,
        string configuration = "Release",
        string? outputPath = null,
        CancellationToken cancellationToken = default) {
        var projectInfo = _parser.ParseProject(projectPath);
        var buildTool = _classifier.GetRecommendedBuildTool(projectInfo);

        if(buildTool == BuildTool.MSBuild) {
            var options = new MSBuildWrapper().CreateBuildOptions(projectPath, configuration);

            if(!string.IsNullOrWhiteSpace(outputPath)) {
                options.OutputPath = outputPath;
            }

            return await _msbuildExecutor.BuildAsync(options, cancellationToken);
        } else {
            return await _dotnetExecutor.BuildAsync(projectPath, configuration, outputPath, cancellationToken);
        }
    }

    /// <summary>
    /// Publishes a .NET project.
    /// </summary>
    /// <param name="projectPath">The path to the project file.</param>
    /// <param name="outputPath">The output path for published files.</param>
    /// <param name="configuration">The build configuration (Debug, Release).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The build result.</returns>
    public async Task<BuildResult> PublishAsync(
        string projectPath,
        string outputPath,
        string configuration = "Release",
        CancellationToken cancellationToken = default) {
        var projectInfo = _parser.ParseProject(projectPath);
        var buildTool = _classifier.GetRecommendedBuildTool(projectInfo);

        if(buildTool == BuildTool.MSBuild) {
            // Use MSBuild for .NET Framework projects
            var options = new MSBuildWrapper().CreatePublishOptions(projectPath, outputPath, configuration);
            return await _msbuildExecutor.PublishAsync(options, cancellationToken);
        } else {
            // Use dotnet CLI for .NET Core/.NET 5+ projects
            return await _dotnetExecutor.PublishAsync(
                projectPath,
                outputPath,
                configuration,
                runtime: null,
                selfContained: null,
                cancellationToken);
        }
    }

    /// <summary>
    /// Publishes a .NET Core or .NET 5+ project with advanced options.
    /// </summary>
    /// <param name="projectPath">The path to the project file.</param>
    /// <param name="outputPath">The output path for published files.</param>
    /// <param name="configuration">The build configuration (Debug, Release).</param>
    /// <param name="runtime">The runtime identifier (win-x64, linux-x64, etc.).</param>
    /// <param name="selfContained">Whether to publish as self-contained.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The build result.</returns>
    public async Task<BuildResult> PublishDotNetCoreAsync(
        string projectPath,
        string outputPath,
        string configuration = "Release",
        string? runtime = null,
        bool? selfContained = null,
        CancellationToken cancellationToken = default) {
        var projectInfo = _parser.ParseProject(projectPath);

        if(_classifier.IsDotNetFramework(projectInfo)) {
            throw new InvalidOperationException(
                "Cannot use PublishDotNetCoreAsync for .NET Framework projects. Use PublishAsync instead.");
        }

        return await _dotnetExecutor.PublishAsync(
            projectPath,
            outputPath,
            configuration,
            runtime,
            selfContained,
            cancellationToken);
    }

    /// <summary>
    /// Cleans a .NET project.
    /// </summary>
    /// <param name="projectPath">The path to the project file.</param>
    /// <param name="configuration">The build configuration (Debug, Release).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The build result.</returns>
    public async Task<BuildResult> CleanAsync(
        string projectPath,
        string configuration = "Release",
        CancellationToken cancellationToken = default) {
        var projectInfo = _parser.ParseProject(projectPath);
        var buildTool = _classifier.GetRecommendedBuildTool(projectInfo);

        if(buildTool == BuildTool.MSBuild) {
            var options = new MSBuildWrapper().CreateCleanOptions(projectPath, configuration);
            return await _msbuildExecutor.CleanAsync(options, cancellationToken);
        } else {
            return await _dotnetExecutor.CleanAsync(projectPath, configuration, cancellationToken);
        }
    }

    /// <summary>
    /// Rebuilds a .NET project (clean + build).
    /// </summary>
    /// <param name="projectPath">The path to the project file.</param>
    /// <param name="configuration">The build configuration (Debug, Release).</param>
    /// <param name="outputPath">Optional output path.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The build result.</returns>
    public async Task<BuildResult> RebuildAsync(
        string projectPath,
        string configuration = "Release",
        string? outputPath = null,
        CancellationToken cancellationToken = default) {
        var projectInfo = _parser.ParseProject(projectPath);
        var buildTool = _classifier.GetRecommendedBuildTool(projectInfo);

        if(buildTool == BuildTool.MSBuild) {
            var options = new MSBuildWrapper().CreateBuildOptions(projectPath, configuration);
            options.Targets.Clear();
            options.Targets.Add("Rebuild");

            if(!string.IsNullOrWhiteSpace(outputPath)) {
                options.OutputPath = outputPath;
            }

            return await _msbuildExecutor.RebuildAsync(options, cancellationToken);
        } else {
            // dotnet CLI doesn't have a rebuild command, so clean then build
            var cleanResult = await _dotnetExecutor.CleanAsync(projectPath, configuration, cancellationToken);

            if(!cleanResult.Success) {
                return cleanResult;
            }

            return await _dotnetExecutor.BuildAsync(projectPath, configuration, outputPath, cancellationToken);
        }
    }

    /// <summary>
    /// Restores NuGet packages for a .NET project.
    /// </summary>
    /// <param name="projectPath">The path to the project file.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The build result.</returns>
    public async Task<BuildResult> RestoreAsync(
        string projectPath,
        CancellationToken cancellationToken = default) {
        // Both .NET Framework and .NET Core can use dotnet restore
        return await _dotnetExecutor.RestoreAsync(projectPath, cancellationToken);
    }

    /// <summary>
    /// Gets information about a project.
    /// </summary>
    /// <param name="projectPath">The path to the project file.</param>
    /// <returns>The project information.</returns>
    public ProjectInfo GetProjectInfo(string projectPath) {
        return _parser.ParseProject(projectPath);
    }

    /// <summary>
    /// Gets a description of the project type.
    /// </summary>
    /// <param name="projectPath">The path to the project file.</param>
    /// <returns>A human-readable description of the project.</returns>
    public string GetProjectDescription(string projectPath) {
        var projectInfo = _parser.ParseProject(projectPath);
        return _classifier.GetProjectDescription(projectInfo);
    }
}
