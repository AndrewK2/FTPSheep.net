namespace FTPSheep.Core.Models;

/// <summary>
/// Represents build and publish configuration settings for a .NET project.
/// </summary>
public sealed class BuildConfiguration {
    /// <summary>
    /// Gets or sets the build configuration name (Debug, Release, etc.).
    /// </summary>
    public string Configuration { get; set; } = "Release";

    /// <summary>
    /// Gets or sets the target framework for publishing (e.g., net8.0, net6.0).
    /// </summary>
    /// <remarks>
    /// If null, the build tool will use the project's default target framework.
    /// For multi-targeted projects, this must be specified.
    /// </remarks>
    public string? TargetFramework { get; set; }

    /// <summary>
    /// Gets or sets the runtime identifier for publishing (e.g., win-x64, linux-x64).
    /// </summary>
    /// <remarks>
    /// If null, a framework-dependent deployment is created.
    /// Specify a runtime identifier for self-contained deployments.
    /// </remarks>
    public string? RuntimeIdentifier { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to create a self-contained deployment.
    /// </summary>
    /// <remarks>
    /// Only applies when RuntimeIdentifier is specified.
    /// Default is true when RuntimeIdentifier is set.
    /// </remarks>
    public bool? SelfContained { get; set; }

    /// <summary>
    /// Gets or sets additional MSBuild properties to pass during publish.
    /// </summary>
    /// <remarks>
    /// Example properties: PublishTrimmed=true, PublishSingleFile=true, etc.
    /// </remarks>
    public Dictionary<string, string> AdditionalProperties { get; set; } = new();

    /// <summary>
    /// Creates a default BuildConfiguration instance.
    /// </summary>
    public BuildConfiguration() {
    }

    /// <summary>
    /// Creates a BuildConfiguration with the specified configuration name.
    /// </summary>
    /// <param name="configuration">The build configuration name.</param>
    public BuildConfiguration(string configuration) {
        Configuration = configuration ?? "Release";
    }

    /// <summary>
    /// Validates the build configuration settings.
    /// </summary>
    /// <param name="errors">A list of validation error messages.</param>
    /// <returns>True if valid, otherwise false.</returns>
    public bool Validate(out List<string> errors) {
        errors = new List<string>();

        if(string.IsNullOrWhiteSpace(Configuration)) {
            errors.Add("Configuration name cannot be empty.");
        }

        // RuntimeIdentifier format validation (basic)
        if(!string.IsNullOrWhiteSpace(RuntimeIdentifier)) {
            if(!RuntimeIdentifier.Contains('-')) {
                errors.Add($"Runtime identifier '{RuntimeIdentifier}' appears invalid. Expected format: os-arch (e.g., win-x64).");
            }
        }

        return errors.Count == 0;
    }
}
