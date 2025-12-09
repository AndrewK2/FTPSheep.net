using FTPSheep.BuildTools.Models;

namespace FTPSheep.BuildTools.Services;

/// <summary>
/// High-level service for building .NET projects.
/// Automatically selects the appropriate build tool based on project type.
/// </summary>
public class BuildService {
    private readonly ProjectFileParser parser;
    private readonly ProjectTypeClassifier classifier;
    private readonly MsBuildExecutor msbuildExecutor;
    private readonly DotnetCliExecutor dotnetExecutor;

    /// <summary>
    /// Initializes a new instance of the <see cref="BuildService"/> class.
    /// </summary>
    public BuildService(
        ProjectFileParser? parser = null,
        ProjectTypeClassifier? classifier = null,
        MsBuildExecutor? msbuildExecutor = null,
        DotnetCliExecutor? dotnetExecutor = null) {
        this.parser = parser ?? new ProjectFileParser();
        this.classifier = classifier ?? new ProjectTypeClassifier();
        this.msbuildExecutor = msbuildExecutor ?? new MsBuildExecutor();
        this.dotnetExecutor = dotnetExecutor ?? new DotnetCliExecutor();
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
        var projectInfo = parser.ParseProject(projectPath);
        var buildTool = classifier.GetRecommendedBuildTool(projectInfo);

        if(buildTool == BuildTool.MsBuild) {
            var options = new MsBuildWrapper().CreateBuildOptions(projectPath, configuration);

            if(!string.IsNullOrWhiteSpace(outputPath)) {
                options.OutputPath = outputPath;
            }

            return await msbuildExecutor.BuildAsync(options, cancellationToken);
        } else {
            return await dotnetExecutor.BuildAsync(projectPath, configuration, outputPath, cancellationToken);
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
        var projectInfo = parser.ParseProject(projectPath);
        var buildTool = classifier.GetRecommendedBuildTool(projectInfo);

        if(buildTool == BuildTool.MsBuild) {
            // Use MSBuild for .NET Framework projects
            var options = new MsBuildWrapper().CreatePublishOptions(projectPath, outputPath, configuration);
            return await msbuildExecutor.PublishAsync(options, cancellationToken);
        } else {
            // Use dotnet CLI for .NET Core/.NET 5+ projects
            return await dotnetExecutor.PublishAsync(
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
        var projectInfo = parser.ParseProject(projectPath);

        if(classifier.IsDotNetFramework(projectInfo)) {
            throw new InvalidOperationException(
                "Cannot use PublishDotNetCoreAsync for .NET Framework projects. Use PublishAsync instead.");
        }

        return await dotnetExecutor.PublishAsync(
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
        var projectInfo = parser.ParseProject(projectPath);
        var buildTool = classifier.GetRecommendedBuildTool(projectInfo);

        if(buildTool == BuildTool.MsBuild) {
            var options = new MsBuildWrapper().CreateCleanOptions(projectPath, configuration);
            return await msbuildExecutor.CleanAsync(options, cancellationToken);
        } else {
            return await dotnetExecutor.CleanAsync(projectPath, configuration, cancellationToken);
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
        var projectInfo = parser.ParseProject(projectPath);
        var buildTool = classifier.GetRecommendedBuildTool(projectInfo);

        if(buildTool == BuildTool.MsBuild) {
            var options = new MsBuildWrapper().CreateBuildOptions(projectPath, configuration);
            options.Targets.Clear();
            options.Targets.Add("Rebuild");

            if(!string.IsNullOrWhiteSpace(outputPath)) {
                options.OutputPath = outputPath;
            }

            return await msbuildExecutor.RebuildAsync(options, cancellationToken);
        } else {
            // dotnet CLI doesn't have a rebuild command, so clean then build
            var cleanResult = await dotnetExecutor.CleanAsync(projectPath, configuration, cancellationToken);

            if(!cleanResult.Success) {
                return cleanResult;
            }

            return await dotnetExecutor.BuildAsync(projectPath, configuration, outputPath, cancellationToken);
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
        return await dotnetExecutor.RestoreAsync(projectPath, cancellationToken);
    }

    /// <summary>
    /// Gets information about a project.
    /// </summary>
    /// <param name="projectPath">The path to the project file.</param>
    /// <returns>The project information.</returns>
    public ProjectInfo GetProjectInfo(string projectPath) {
        return parser.ParseProject(projectPath);
    }

    /// <summary>
    /// Gets a description of the project type.
    /// </summary>
    /// <param name="projectPath">The path to the project file.</param>
    /// <returns>A human-readable description of the project.</returns>
    public string GetProjectDescription(string projectPath) {
        var projectInfo = parser.ParseProject(projectPath);
        return classifier.GetProjectDescription(projectInfo);
    }
}
