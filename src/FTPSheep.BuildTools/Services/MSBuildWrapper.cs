using System.Text;
using FTPSheep.BuildTools.Models;


namespace FTPSheep.BuildTools.Services;

/// <summary>
/// Wrapper for MSBuild operations that builds command-line arguments.
/// </summary>
public class MsBuildWrapper {
    private readonly BuildToolLocator toolLocator;

    /// <summary>
    /// Initializes a new instance of the <see cref="MsBuildWrapper"/> class.
    /// </summary>
    /// <param name="toolLocator">The build tool locator.</param>
    public MsBuildWrapper(BuildToolLocator? toolLocator = null) {
        this.toolLocator = toolLocator ?? new BuildToolLocator();
    }

    /// <summary>
    /// Gets the path to the MSBuild executable.
    /// </summary>
    /// <returns>The full path to MSBuild.exe.</returns>
    public string GetMsBuildPath() {
        return toolLocator.LocateMsBuild();
    }

    /// <summary>
    /// Builds the command-line arguments for MSBuild based on the provided options.
    /// </summary>
    /// <param name="options">The MSBuild options.</param>
    /// <returns>The command-line arguments string.</returns>
    public string BuildArguments(MsBuildOptions options) {
        if(options == null) {
            throw new ArgumentNullException(nameof(options));
        }

        if(string.IsNullOrWhiteSpace(options.ProjectPath)) {
            throw new ArgumentException("ProjectPath is required.", nameof(options));
        }

        var args = new StringBuilder();

        // Add project path (quoted in case of spaces)
        args.Append($"\"{options.ProjectPath}\"");

        // Add targets
        if(options.Targets.Count > 0) {
            args.Append($" /t:{string.Join(";", options.Targets)}");
        }

        // Add configuration
        args.Append($" /p:Configuration={options.Configuration}");

        // Add platform if specified
        if(!string.IsNullOrWhiteSpace(options.Platform)) {
            args.Append($" /p:Platform=\"{options.Platform}\"");
        }

        // Add output path if specified
        if(!string.IsNullOrWhiteSpace(options.OutputPath)) {
            args.Append($" /p:OutputPath=\"{options.OutputPath}\"");
        }

        // Add target framework if specified
        if(!string.IsNullOrWhiteSpace(options.TargetFramework)) {
            args.Append($" /p:TargetFramework={options.TargetFramework}");
        }

        // Add publish profile if specified
        if(!string.IsNullOrWhiteSpace(options.PublishProfile)) {
            args.Append($" /p:PublishProfile={options.PublishProfile}");
        }

        // Add custom properties
        foreach(var prop in options.Properties) {
            args.Append($" /p:{prop.Key}={EscapePropertyValue(prop.Value)}");
        }

        // Add verbosity
        var verbosity = options.Verbosity switch {
            MsBuildVerbosity.Quiet => "quiet",
            MsBuildVerbosity.Minimal => "minimal",
            MsBuildVerbosity.Normal => "normal",
            MsBuildVerbosity.Detailed => "detailed",
            MsBuildVerbosity.Diagnostic => "diagnostic",
            _ => "minimal"
        };
        args.Append($" /v:{verbosity}");

        // Add max CPU count
        if(options.MaxCpuCount.HasValue) {
            args.Append($" /m:{options.MaxCpuCount.Value}");
        } else {
            // Use parallel build by default
            args.Append(" /m");
        }

        // Add restore if enabled
        if(options.RestorePackages) {
            args.Append(" /restore");
        }

        // Add warnings as errors
        if(options.TreatWarningsAsErrors) {
            args.Append(" /p:TreatWarningsAsErrors=true");
        }

        // No logo and console logger
        args.Append(" /nologo");
        args.Append(" /consoleloggerparameters:NoSummary");

        return args.ToString();
    }

    /// <summary>
    /// Creates MSBuild options for a build operation.
    /// </summary>
    /// <param name="projectPath">The path to the project file.</param>
    /// <param name="configuration">The build configuration.</param>
    /// <returns>MSBuild options configured for building.</returns>
    public MsBuildOptions CreateBuildOptions(string projectPath, string configuration = "Release") {
        return new MsBuildOptions {
            ProjectPath = projectPath,
            Configuration = configuration,
            Targets = ["Build"],
            RestorePackages = true
        };
    }

    /// <summary>
    /// Creates MSBuild options for a publish operation.
    /// </summary>
    /// <param name="projectPath">The path to the project file.</param>
    /// <param name="outputPath">The output path for published files.</param>
    /// <param name="configuration">The build configuration.</param>
    /// <returns>MSBuild options configured for publishing.</returns>
    public MsBuildOptions CreatePublishOptions(string projectPath, string outputPath, string configuration = "Release") {
        return new MsBuildOptions {
            ProjectPath = projectPath,
            Configuration = configuration,
            OutputPath = outputPath,
            Targets = ["Build", "Publish"],
            RestorePackages = true,
            Properties = new Dictionary<string, string>
            {
                { "DeployOnBuild", "true" },
                { "PublishUrl", outputPath },
                { "WebPublishMethod", "FileSystem" },
                { "DeleteExistingFiles", "false" },
                { "SkipInvalidConfigurations", "true" }
            }
        };
    }

    /// <summary>
    /// Creates MSBuild options for a clean operation.
    /// </summary>
    /// <param name="projectPath">The path to the project file.</param>
    /// <param name="configuration">The build configuration.</param>
    /// <returns>MSBuild options configured for cleaning.</returns>
    public MsBuildOptions CreateCleanOptions(string projectPath, string configuration = "Release") {
        return new MsBuildOptions {
            ProjectPath = projectPath,
            Configuration = configuration,
            Targets = ["Clean"],
            RestorePackages = false
        };
    }

    private string EscapePropertyValue(string value) {
        // Escape special characters in property values
        if(value.Contains(' ') || value.Contains(';') || value.Contains(',')) {
            return $"\"{value}\"";
        }
        return value;
    }
}
