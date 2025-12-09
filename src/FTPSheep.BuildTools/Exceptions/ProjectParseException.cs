namespace FTPSheep.BuildTools.Exceptions;

/// <summary>
/// Exception thrown when a project file cannot be parsed.
/// </summary>
public class ProjectParseException : Exception {
    /// <summary>
    /// Gets the path to the project file that failed to parse.
    /// </summary>
    public string? ProjectPath { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ProjectParseException"/> class.
    /// </summary>
    public ProjectParseException() {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ProjectParseException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public ProjectParseException(string message) : base(message) {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ProjectParseException"/> class with a specified error message and inner exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public ProjectParseException(string message, Exception innerException) : base(message, innerException) {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ProjectParseException"/> class with a project path.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="projectPath">The path to the project file.</param>
    public ProjectParseException(string message, string projectPath) : base(message) {
        ProjectPath = projectPath;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ProjectParseException"/> class with a project path and inner exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="projectPath">The path to the project file.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public ProjectParseException(string message, string projectPath, Exception innerException) : base(message, innerException) {
        ProjectPath = projectPath;
    }
}
