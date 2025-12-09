namespace FTPSheep.Core.Models;

/// <summary>
/// Defines exit codes for the FTPSheep application.
/// </summary>
public static class ExitCodes {
    /// <summary>
    /// Success - the operation completed successfully.
    /// </summary>
    public const int Success = 0;

    /// <summary>
    /// General error - an unspecified error occurred.
    /// </summary>
    public const int GeneralError = 1;

    /// <summary>
    /// Build failure - the project build/publish failed.
    /// </summary>
    public const int BuildFailure = 2;

    /// <summary>
    /// Connection failure - failed to connect to the remote server.
    /// </summary>
    public const int ConnectionFailure = 3;

    /// <summary>
    /// Authentication failure - authentication with the server failed.
    /// </summary>
    public const int AuthenticationFailure = 4;

    /// <summary>
    /// Deployment failure - the deployment operation failed.
    /// </summary>
    public const int DeploymentFailure = 5;

    /// <summary>
    /// Configuration error - invalid configuration or profile settings.
    /// </summary>
    public const int ConfigurationError = 6;

    /// <summary>
    /// Profile not found - the specified profile does not exist.
    /// </summary>
    public const int ProfileNotFound = 7;

    /// <summary>
    /// Invalid arguments - the command-line arguments are invalid.
    /// </summary>
    public const int InvalidArguments = 8;

    /// <summary>
    /// Operation cancelled - the operation was cancelled by the user.
    /// </summary>
    public const int OperationCancelled = 9;

    /// <summary>
    /// Determines the appropriate exit code for an exception.
    /// </summary>
    /// <param name="exception">The exception that occurred.</param>
    /// <returns>The exit code corresponding to the exception type.</returns>
    public static int FromException(Exception exception) {
        return exception switch {
            Exceptions.BuildException => BuildFailure,
            Exceptions.ConnectionException => ConnectionFailure,
            Exceptions.AuthenticationException => AuthenticationFailure,
            Exceptions.DeploymentException => DeploymentFailure,
            Exceptions.ConfigurationException => ConfigurationError,
            Exceptions.ProfileNotFoundException => ProfileNotFound,
            Exceptions.ProfileValidationException => ConfigurationError,
            OperationCanceledException => OperationCancelled,
            ArgumentException => InvalidArguments,
            _ => GeneralError
        };
    }

    /// <summary>
    /// Gets a human-readable description of an exit code.
    /// </summary>
    /// <param name="exitCode">The exit code.</param>
    /// <returns>A description of the exit code.</returns>
    public static string GetDescription(int exitCode) {
        return exitCode switch {
            Success => "Success",
            GeneralError => "General Error",
            BuildFailure => "Build Failure",
            ConnectionFailure => "Connection Failure",
            AuthenticationFailure => "Authentication Failure",
            DeploymentFailure => "Deployment Failure",
            ConfigurationError => "Configuration Error",
            ProfileNotFound => "Profile Not Found",
            InvalidArguments => "Invalid Arguments",
            OperationCancelled => "Operation Cancelled",
            _ => $"Unknown Error ({exitCode})"
        };
    }
}
