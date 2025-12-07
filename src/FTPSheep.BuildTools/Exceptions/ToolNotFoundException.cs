namespace FTPSheep.BuildTools.Exceptions;

/// <summary>
/// Exception thrown when a required build tool cannot be found on the system.
/// </summary>
public class ToolNotFoundException : Exception
{
    /// <summary>
    /// Gets the name of the tool that was not found.
    /// </summary>
    public string? ToolName { get; init; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ToolNotFoundException"/> class.
    /// </summary>
    public ToolNotFoundException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ToolNotFoundException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public ToolNotFoundException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ToolNotFoundException"/> class with a specified error message and inner exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public ToolNotFoundException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
