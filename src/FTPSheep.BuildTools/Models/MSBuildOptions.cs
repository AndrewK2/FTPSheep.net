namespace FTPSheep.BuildTools.Models;

/// <summary>
/// Configuration options for MSBuild operations.
/// </summary>
public class MsBuildOptions {
    /// <summary>
    /// Gets or sets the path to the project or solution file.
    /// </summary>
    public string ProjectPath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the build configuration (Debug, Release, etc.).
    /// </summary>
    public string Configuration { get; set; } = "Release";

    /// <summary>
    /// Gets or sets the target platform (Any CPU, x86, x64, ARM, etc.).
    /// </summary>
    public string? Platform { get; set; }

    /// <summary>
    /// Gets or sets the output path for the build.
    /// </summary>
    public string? OutputPath { get; set; }

    /// <summary>
    /// Gets or sets the target framework moniker (e.g., "net472").
    /// </summary>
    public string? TargetFramework { get; set; }

    /// <summary>
    /// Gets or sets additional MSBuild properties as key-value pairs.
    /// </summary>
    public Dictionary<string, string> Properties { get; set; } = new();

    /// <summary>
    /// Gets or sets the MSBuild targets to execute (Build, Rebuild, Clean, Publish, etc.).
    /// </summary>
    public List<string> Targets { get; set; } = ["Build"];

    /// <summary>
    /// Gets or sets the MSBuild verbosity level.
    /// </summary>
    public MsBuildVerbosity Verbosity { get; set; } = MsBuildVerbosity.Minimal;

    /// <summary>
    /// Gets or sets the maximum number of concurrent processes to use during build.
    /// </summary>
    public int? MaxCpuCount { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to restore NuGet packages before building.
    /// </summary>
    public bool RestorePackages { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to treat warnings as errors.
    /// </summary>
    public bool TreatWarningsAsErrors { get; set; } = false;

    /// <summary>
    /// Gets or sets the publish profile name for web deployments.
    /// </summary>
    public string? PublishProfile { get; set; }
}

/// <summary>
/// MSBuild verbosity levels.
/// </summary>
public enum MsBuildVerbosity {
    /// <summary>
    /// Quiet - minimal output.
    /// </summary>
    Quiet = 0,

    /// <summary>
    /// Minimal - essential information only.
    /// </summary>
    Minimal = 1,

    /// <summary>
    /// Normal - standard build output.
    /// </summary>
    Normal = 2,

    /// <summary>
    /// Detailed - detailed build information.
    /// </summary>
    Detailed = 3,

    /// <summary>
    /// Diagnostic - maximum verbosity for troubleshooting.
    /// </summary>
    Diagnostic = 4
}
