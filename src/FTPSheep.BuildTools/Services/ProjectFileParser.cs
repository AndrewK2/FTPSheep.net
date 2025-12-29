using System.Xml.Linq;
using FTPSheep.BuildTools.Exceptions;
using FTPSheep.BuildTools.Models;
#if NET48
using FTPSheep.BuildTools.Compatibility;
#endif

namespace FTPSheep.BuildTools.Services;

/// <summary>
/// Parses .NET project files (.csproj, .vbproj, .fsproj) to extract project information.
/// </summary>
public class ProjectFileParser {
    /// <summary>
    /// Parses a project file and extracts project information.
    /// </summary>
    /// <param name="projectPath">The path to the project file.</param>
    /// <returns>A <see cref="ProjectInfo"/> object containing project details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when projectPath is null or empty.</exception>
    /// <exception cref="FileNotFoundException">Thrown when the project file does not exist.</exception>
    /// <exception cref="ProjectParseException">Thrown when the project file cannot be parsed.</exception>
    public ProjectInfo ParseProject(string projectPath) {
        if(string.IsNullOrWhiteSpace(projectPath)) {
            throw new ArgumentNullException(nameof(projectPath));
        }

        if(!File.Exists(projectPath)) {
            throw new FileNotFoundException($"Project file not found: {projectPath}", projectPath);
        }

        try {
            var doc = XDocument.Load(projectPath);
            var root = doc.Root;

            if(root == null || root.Name.LocalName != "Project") {
                throw new ProjectParseException($"Invalid project file format: {projectPath}", projectPath);
            }

            // Determine if this is an SDK-style project
            var sdk = root.Attribute("Sdk")?.Value;
            var isSdkStyle = !string.IsNullOrWhiteSpace(sdk);
            var format = isSdkStyle ? ProjectFormat.SdkStyle : ProjectFormat.LegacyFramework;

            // Extract target frameworks
            var targetFrameworks = ExtractTargetFrameworks(root);

            // Extract output type
            var outputType = root.Descendants()
                .Where(e => e.Name.LocalName == "OutputType")
                .FirstOrDefault()?.Value;

            // Determine project type
            var projectType = DetermineProjectType(sdk, outputType, root, projectPath);

            return new ProjectInfo {
                ProjectPath = Path.GetFullPath(projectPath),
                FileExtension = Path.GetExtension(projectPath),
                Sdk = sdk,
                TargetFrameworks = targetFrameworks,
                OutputType = outputType,
                ProjectType = projectType,
                Format = format
            };
        } catch(Exception ex) when(ex is not ProjectParseException) {
            throw new ProjectParseException($"Failed to parse project file: {ex.Message}", projectPath, ex);
        }
    }

    /// <summary>
    /// Asynchronously parses a project file and extracts project information.
    /// </summary>
    /// <param name="projectPath">The path to the project file.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A <see cref="ProjectInfo"/> object containing project details.</returns>
    public async Task<ProjectInfo> ParseProjectAsync(string projectPath, CancellationToken cancellationToken = default) {
        return await Task.Run(() => ParseProject(projectPath), cancellationToken);
    }

    private List<string> ExtractTargetFrameworks(XElement root) {
        var frameworks = new List<string>();

        // Check for TargetFramework (single)
        var targetFramework = root.Descendants()
            .Where(e => e.Name.LocalName == "TargetFramework")
            .FirstOrDefault()?.Value;

        if(!string.IsNullOrWhiteSpace(targetFramework)) {
            frameworks.Add(targetFramework);
            return frameworks;
        }

        // Check for TargetFrameworks (multiple, semicolon-separated)
        var targetFrameworks = root.Descendants()
            .Where(e => e.Name.LocalName == "TargetFrameworks")
            .FirstOrDefault()?.Value;

        if(!string.IsNullOrWhiteSpace(targetFrameworks)) {
#if NET48
            frameworks.AddRange(targetFrameworks.SplitAndTrim(';'));
#else
            frameworks.AddRange(targetFrameworks.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
#endif
            return frameworks;
        }

        // Legacy .NET Framework projects use TargetFrameworkVersion
        var targetFrameworkVersion = root.Descendants()
            .Where(e => e.Name.LocalName == "TargetFrameworkVersion")
            .FirstOrDefault()?.Value;

        if(!string.IsNullOrWhiteSpace(targetFrameworkVersion)) {
            // Convert version like "v4.7.2" to "net472"
            var version = targetFrameworkVersion.TrimStart('v').Replace(".", "");
            frameworks.Add($"net{version}");
        }

        return frameworks;
    }

    private ProjectType DetermineProjectType(string? sdk, string? outputType, XElement root, string projectPath) {
        // Check SDK type first (most reliable for modern projects)
        if(!string.IsNullOrWhiteSpace(sdk)) {
            if(sdk.Contains("Microsoft.NET.Sdk.Web", StringComparison.OrdinalIgnoreCase)) {
                // Check for Blazor
                if(HasPackageReference(root, "Microsoft.AspNetCore.Components.WebAssembly") ||
                    HasPackageReference(root, "Microsoft.AspNetCore.Components.WebAssembly.Server")) {
                    return ProjectType.Blazor;
                }

                // Check for Razor Pages
                if(HasPackageReference(root, "Microsoft.AspNetCore.Mvc.RazorPages") ||
                    FileExistsInProjectDirectory(projectPath, "Pages")) {
                    return ProjectType.RazorPages;
                }

                return ProjectType.AspNetCore;
            }

            if(sdk.Contains("Microsoft.NET.Sdk.Worker", StringComparison.OrdinalIgnoreCase)) {
                return ProjectType.WorkerService;
            }

            if(sdk.Contains("Microsoft.NET.Sdk.Razor", StringComparison.OrdinalIgnoreCase)) {
                return ProjectType.Blazor;
            }
        }

        // Check output type
        if(string.Equals(outputType, "Library", StringComparison.OrdinalIgnoreCase)) {
            return ProjectType.Library;
        }

        if(string.Equals(outputType, "WinExe", StringComparison.OrdinalIgnoreCase)) {
            return ProjectType.WindowsApp;
        }

        if(string.Equals(outputType, "Exe", StringComparison.OrdinalIgnoreCase)) {
            return ProjectType.Console;
        }

        // Check for legacy ASP.NET projects
        if(HasPackageReference(root, "Microsoft.AspNet.Mvc") ||
            root.Descendants().Any(e => e.Name.LocalName == "MvcBuildViews")) {
            return ProjectType.AspNetMvc;
        }

        if(HasPackageReference(root, "Microsoft.AspNet.WebApi")) {
            return ProjectType.AspNetWebApi;
        }

        // Check for ProjectTypeGuids (legacy .NET Framework)
        var projectTypeGuids = root.Descendants()
            .Where(e => e.Name.LocalName == "ProjectTypeGuids")
            .FirstOrDefault()?.Value;

        if(!string.IsNullOrWhiteSpace(projectTypeGuids)) {
            var guids = projectTypeGuids.ToUpperInvariant();

            // ASP.NET MVC GUID
            if(guids.Contains("{E3E379DF-F4C6-4180-9B81-6769533ABE47}")) {
                return ProjectType.AspNetMvc;
            }

            // ASP.NET Web Application GUID
            if(guids.Contains("{349C5851-65DF-11DA-9384-00065B846F21}")) {
                return ProjectType.AspNetWebApp;
            }
        }

        return ProjectType.Unknown;
    }

    private bool HasPackageReference(XElement root, string packageId) {
        return root.Descendants()
            .Where(e => e.Name.LocalName == "PackageReference")
            .Any(e => string.Equals(e.Attribute("Include")?.Value, packageId, StringComparison.OrdinalIgnoreCase));
    }

    private bool FileExistsInProjectDirectory(string projectPath, string directoryName) {
        var projectDir = Path.GetDirectoryName(projectPath);
        if(string.IsNullOrEmpty(projectDir)) {
            return false;
        }

        var targetDir = Path.Combine(projectDir, directoryName);
        return Directory.Exists(targetDir);
    }
}
