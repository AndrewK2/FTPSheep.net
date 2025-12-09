using FTPSheep.BuildTools.Models;

namespace FTPSheep.BuildTools.Services;

/// <summary>
/// Classifies .NET projects based on their characteristics.
/// </summary>
public class ProjectTypeClassifier {
    /// <summary>
    /// Determines if a project is a .NET Framework (4.x) project.
    /// </summary>
    /// <param name="projectInfo">The project information.</param>
    /// <returns>True if the project targets .NET Framework 4.x.</returns>
    public bool IsDotNetFramework(ProjectInfo projectInfo) {
        if(projectInfo == null) {
            throw new ArgumentNullException(nameof(projectInfo));
        }

        return projectInfo.TargetFrameworks.Any(tfm =>
            tfm.StartsWith("net4", StringComparison.OrdinalIgnoreCase) ||
            tfm.StartsWith("v4", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Determines if a project is a .NET Core project (.NET Core 1.0 - 3.1).
    /// </summary>
    /// <param name="projectInfo">The project information.</param>
    /// <returns>True if the project targets .NET Core.</returns>
    public bool IsDotNetCore(ProjectInfo projectInfo) {
        if(projectInfo == null) {
            throw new ArgumentNullException(nameof(projectInfo));
        }

        return projectInfo.TargetFrameworks.Any(tfm =>
            tfm.StartsWith("netcoreapp", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Determines if a project is a .NET 5+ project (.NET 5, 6, 7, 8, etc.).
    /// </summary>
    /// <param name="projectInfo">The project information.</param>
    /// <returns>True if the project targets .NET 5 or later.</returns>
    public bool IsDotNet5Plus(ProjectInfo projectInfo) {
        if(projectInfo == null) {
            throw new ArgumentNullException(nameof(projectInfo));
        }

        return projectInfo.TargetFrameworks.Any(tfm => {
            // Match patterns like "net5.0", "net6.0", "net7.0", "net8.0", etc.
            if(tfm.StartsWith("net", StringComparison.OrdinalIgnoreCase) && tfm.Contains('.')) {
                var versionPart = tfm.Substring(3, tfm.IndexOf('.') - 3);
                if(int.TryParse(versionPart, out var version)) {
                    return version >= 5;
                }
            }
            return false;
        });
    }

    /// <summary>
    /// Determines if a project is a .NET Standard library.
    /// </summary>
    /// <param name="projectInfo">The project information.</param>
    /// <returns>True if the project targets .NET Standard.</returns>
    public bool IsDotNetStandard(ProjectInfo projectInfo) {
        if(projectInfo == null) {
            throw new ArgumentNullException(nameof(projectInfo));
        }

        return projectInfo.TargetFrameworks.Any(tfm =>
            tfm.StartsWith("netstandard", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Determines if a project is an ASP.NET project (any type).
    /// </summary>
    /// <param name="projectInfo">The project information.</param>
    /// <returns>True if the project is an ASP.NET project.</returns>
    public bool IsAspNet(ProjectInfo projectInfo) {
        if(projectInfo == null) {
            throw new ArgumentNullException(nameof(projectInfo));
        }

        return projectInfo.ProjectType is
            ProjectType.AspNetWebApp or
            ProjectType.AspNetMvc or
            ProjectType.AspNetWebApi or
            ProjectType.AspNetCore or
            ProjectType.Blazor or
            ProjectType.RazorPages;
    }

    /// <summary>
    /// Determines if a project is a modern ASP.NET Core project.
    /// </summary>
    /// <param name="projectInfo">The project information.</param>
    /// <returns>True if the project is an ASP.NET Core project.</returns>
    public bool IsAspNetCore(ProjectInfo projectInfo) {
        if(projectInfo == null) {
            throw new ArgumentNullException(nameof(projectInfo));
        }

        return projectInfo.ProjectType is
            ProjectType.AspNetCore or
            ProjectType.Blazor or
            ProjectType.RazorPages;
    }

    /// <summary>
    /// Determines if a project requires a web server to run.
    /// </summary>
    /// <param name="projectInfo">The project information.</param>
    /// <returns>True if the project is a web application.</returns>
    public bool IsWebApplication(ProjectInfo projectInfo) {
        if(projectInfo == null) {
            throw new ArgumentNullException(nameof(projectInfo));
        }

        return IsAspNet(projectInfo) || projectInfo.ProjectType == ProjectType.WorkerService;
    }

    /// <summary>
    /// Gets a human-readable description of the project.
    /// </summary>
    /// <param name="projectInfo">The project information.</param>
    /// <returns>A description of the project type and target framework.</returns>
    public string GetProjectDescription(ProjectInfo projectInfo) {
        if(projectInfo == null) {
            throw new ArgumentNullException(nameof(projectInfo));
        }

        var typeDescription = projectInfo.ProjectType switch {
            ProjectType.Library => "Class Library",
            ProjectType.Console => "Console Application",
            ProjectType.WindowsApp => "Windows Application",
            ProjectType.AspNetWebApp => "ASP.NET Web Application",
            ProjectType.AspNetMvc => "ASP.NET MVC",
            ProjectType.AspNetWebApi => "ASP.NET Web API",
            ProjectType.AspNetCore => "ASP.NET Core Web Application",
            ProjectType.Blazor => "Blazor Application",
            ProjectType.RazorPages => "Razor Pages Application",
            ProjectType.WorkerService => "Worker Service",
            _ => "Unknown Project Type"
        };

        var framework = projectInfo.PrimaryTargetFramework ?? "Unknown Framework";
        var multiTarget = projectInfo.IsMultiTargeting ? $" (multi-targeting: {string.Join(", ", projectInfo.TargetFrameworks)})" : "";

        return $"{typeDescription} targeting {framework}{multiTarget}";
    }

    /// <summary>
    /// Gets the recommended build tool for the project.
    /// </summary>
    /// <param name="projectInfo">The project information.</param>
    /// <returns>The recommended build tool.</returns>
    public BuildTool GetRecommendedBuildTool(ProjectInfo projectInfo) {
        if(projectInfo == null) {
            throw new ArgumentNullException(nameof(projectInfo));
        }

        // SDK-style projects should use dotnet CLI
        if(projectInfo.IsSdkStyle) {
            return BuildTool.DotnetCli;
        }

        // Legacy .NET Framework projects should use MSBuild
        if(IsDotNetFramework(projectInfo)) {
            return BuildTool.MsBuild;
        }

        // Default to dotnet CLI for modern projects
        return BuildTool.DotnetCli;
    }
}

/// <summary>
/// Represents the build tool to use for a project.
/// </summary>
public enum BuildTool {
    /// <summary>
    /// Unknown or unspecified build tool.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// MSBuild (msbuild.exe) - typically used for .NET Framework projects.
    /// </summary>
    MsBuild = 1,

    /// <summary>
    /// .NET CLI (dotnet.exe) - used for .NET Core, .NET 5+ projects.
    /// </summary>
    DotnetCli = 2
}
