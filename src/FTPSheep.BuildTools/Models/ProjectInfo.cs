namespace FTPSheep.BuildTools.Models;

/// <summary>
/// Represents information about a .NET project file.
/// </summary>
public class ProjectInfo
{
    /// <summary>
    /// Gets the full path to the project file.
    /// </summary>
    public string ProjectPath { get; init; } = string.Empty;

    /// <summary>
    /// Gets the project file extension (.csproj, .vbproj, .fsproj).
    /// </summary>
    public string FileExtension { get; init; } = string.Empty;

    /// <summary>
    /// Gets the project SDK (e.g., "Microsoft.NET.Sdk", "Microsoft.NET.Sdk.Web").
    /// </summary>
    public string? Sdk { get; init; }

    /// <summary>
    /// Gets the target framework(s) for the project.
    /// </summary>
    public IReadOnlyList<string> TargetFrameworks { get; init; } = new List<string>();

    /// <summary>
    /// Gets the output type (Exe, Library, WinExe).
    /// </summary>
    public string? OutputType { get; init; }

    /// <summary>
    /// Gets a value indicating whether this is an SDK-style project.
    /// </summary>
    public bool IsSdkStyle => !string.IsNullOrWhiteSpace(Sdk);

    /// <summary>
    /// Gets the project type (Web, Library, Console, etc.).
    /// </summary>
    public ProjectType ProjectType { get; init; }

    /// <summary>
    /// Gets the .NET project format (Legacy .NET Framework or SDK-style).
    /// </summary>
    public ProjectFormat Format { get; init; }

    /// <summary>
    /// Gets the primary target framework (first in the list).
    /// </summary>
    public string? PrimaryTargetFramework => TargetFrameworks.FirstOrDefault();

    /// <summary>
    /// Gets a value indicating whether this project targets multiple frameworks.
    /// </summary>
    public bool IsMultiTargeting => TargetFrameworks.Count > 1;
}

/// <summary>
/// Represents the format of a .NET project file.
/// </summary>
public enum ProjectFormat
{
    /// <summary>
    /// Unknown or unrecognized format.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// Legacy .NET Framework project format (verbose XML, uses packages.config).
    /// </summary>
    LegacyFramework = 1,

    /// <summary>
    /// Modern SDK-style project format (.NET Core, .NET 5+).
    /// </summary>
    SdkStyle = 2
}

/// <summary>
/// Represents the type of a .NET project.
/// </summary>
public enum ProjectType
{
    /// <summary>
    /// Unknown or unrecognized project type.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// Class library project.
    /// </summary>
    Library = 1,

    /// <summary>
    /// Console application.
    /// </summary>
    Console = 2,

    /// <summary>
    /// Windows application.
    /// </summary>
    WindowsApp = 3,

    /// <summary>
    /// ASP.NET Web Application (legacy .NET Framework).
    /// </summary>
    AspNetWebApp = 4,

    /// <summary>
    /// ASP.NET MVC application.
    /// </summary>
    AspNetMvc = 5,

    /// <summary>
    /// ASP.NET Web API.
    /// </summary>
    AspNetWebApi = 6,

    /// <summary>
    /// ASP.NET Core Web Application (modern).
    /// </summary>
    AspNetCore = 7,

    /// <summary>
    /// Blazor application (Server or WebAssembly).
    /// </summary>
    Blazor = 8,

    /// <summary>
    /// Razor Pages application.
    /// </summary>
    RazorPages = 9,

    /// <summary>
    /// Worker Service / Background Service.
    /// </summary>
    WorkerService = 10
}
