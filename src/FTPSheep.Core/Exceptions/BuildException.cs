namespace FTPSheep.Core.Exceptions;

/// <summary>
/// Base exception for all build-related errors.
/// </summary>
public class BuildException : Exception
{
    /// <summary>
    /// Gets the project path associated with this exception, if applicable.
    /// </summary>
    public string? ProjectPath { get; }

    /// <summary>
    /// Gets the build configuration (e.g., Debug, Release) associated with this exception, if applicable.
    /// </summary>
    public string? BuildConfiguration { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="BuildException"/> class.
    /// </summary>
    public BuildException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BuildException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public BuildException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BuildException"/> class with a specified error message and inner exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public BuildException(string message, Exception innerException) : base(message, innerException)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BuildException"/> class with a specified error message and project path.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="projectPath">The project path associated with this exception.</param>
    /// <param name="buildConfiguration">The build configuration associated with this exception.</param>
    public BuildException(string message, string projectPath, string? buildConfiguration = null)
        : base(message)
    {
        ProjectPath = projectPath;
        BuildConfiguration = buildConfiguration;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BuildException"/> class with a specified error message, project path, and inner exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="projectPath">The project path associated with this exception.</param>
    /// <param name="buildConfiguration">The build configuration associated with this exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public BuildException(string message, string projectPath, string? buildConfiguration, Exception innerException)
        : base(message, innerException)
    {
        ProjectPath = projectPath;
        BuildConfiguration = buildConfiguration;
    }
}

/// <summary>
/// Exception thrown when a build compilation fails.
/// </summary>
public class BuildCompilationException : BuildException
{
    /// <summary>
    /// Gets the build output/error messages.
    /// </summary>
    public IReadOnlyList<string> BuildErrors { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="BuildCompilationException"/> class.
    /// </summary>
    public BuildCompilationException()
    {
        BuildErrors = new List<string>();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BuildCompilationException"/> class with build errors.
    /// </summary>
    /// <param name="errors">The build errors that occurred.</param>
    public BuildCompilationException(IEnumerable<string> errors)
        : base($"Build compilation failed with {errors.Count()} error(s).")
    {
        BuildErrors = errors.ToList();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BuildCompilationException"/> class with a project path and build errors.
    /// </summary>
    /// <param name="projectPath">The project path.</param>
    /// <param name="errors">The build errors that occurred.</param>
    public BuildCompilationException(string projectPath, IEnumerable<string> errors)
        : base($"Build compilation failed for '{projectPath}' with {errors.Count()} error(s).", projectPath)
    {
        BuildErrors = errors.ToList();
    }
}

/// <summary>
/// Exception thrown when the build tools (MSBuild, dotnet CLI) cannot be found.
/// </summary>
public class BuildToolNotFoundException : BuildException
{
    /// <summary>
    /// Gets the name of the build tool that was not found.
    /// </summary>
    public string? ToolName { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="BuildToolNotFoundException"/> class.
    /// </summary>
    public BuildToolNotFoundException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BuildToolNotFoundException"/> class with a tool name.
    /// </summary>
    /// <param name="toolName">The name of the build tool that was not found.</param>
    public BuildToolNotFoundException(string toolName)
        : base($"Build tool '{toolName}' was not found. Please ensure it is installed and available in PATH.")
    {
        ToolName = toolName;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BuildToolNotFoundException"/> class with a tool name and suggestion.
    /// </summary>
    /// <param name="toolName">The name of the build tool that was not found.</param>
    /// <param name="suggestion">A suggestion for resolving the issue.</param>
    public BuildToolNotFoundException(string toolName, string suggestion)
        : base($"Build tool '{toolName}' was not found. {suggestion}")
    {
        ToolName = toolName;
    }
}
