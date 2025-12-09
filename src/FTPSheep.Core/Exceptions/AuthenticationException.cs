namespace FTPSheep.Core.Exceptions;

/// <summary>
/// Exception thrown when authentication fails.
/// </summary>
public class AuthenticationException : Exception {
    /// <summary>
    /// Gets the username associated with this exception.
    /// </summary>
    public string? Username { get; }

    /// <summary>
    /// Gets the server host associated with this exception.
    /// </summary>
    public string? Host { get; }

    /// <summary>
    /// Gets a value indicating whether this failure might be due to incorrect credentials.
    /// </summary>
    public bool IsCredentialError { get; init; }

    /// <summary>
    /// Initializes a new instance of the <see cref="AuthenticationException"/> class.
    /// </summary>
    public AuthenticationException() {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AuthenticationException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public AuthenticationException(string message) : base(message) {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AuthenticationException"/> class with a specified error message and inner exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public AuthenticationException(string message, Exception innerException) : base(message, innerException) {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AuthenticationException"/> class with username and host.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="username">The username that failed authentication.</param>
    /// <param name="host">The server host.</param>
    /// <param name="isCredentialError">Whether this is likely a credential error.</param>
    public AuthenticationException(string message, string username, string host, bool isCredentialError = true)
        : base(message) {
        Username = username;
        Host = host;
        IsCredentialError = isCredentialError;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AuthenticationException"/> class with username, host, and inner exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="username">The username that failed authentication.</param>
    /// <param name="host">The server host.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    /// <param name="isCredentialError">Whether this is likely a credential error.</param>
    public AuthenticationException(string message, string username, string host, Exception innerException, bool isCredentialError = true)
        : base(message, innerException) {
        Username = username;
        Host = host;
        IsCredentialError = isCredentialError;
    }
}

/// <summary>
/// Exception thrown when authentication fails due to invalid credentials.
/// </summary>
public class InvalidCredentialsException : AuthenticationException {
    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidCredentialsException"/> class.
    /// </summary>
    public InvalidCredentialsException()
        : base("Authentication failed due to invalid credentials.") {
        IsCredentialError = true;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidCredentialsException"/> class with username and host.
    /// </summary>
    /// <param name="username">The username that failed authentication.</param>
    /// <param name="host">The server host.</param>
    public InvalidCredentialsException(string username, string host)
        : base($"Authentication failed for user '{username}' on {host}. Please verify your credentials.", username, host, isCredentialError: true) {
    }
}

/// <summary>
/// Exception thrown when authentication fails due to missing or expired permissions.
/// </summary>
public class InsufficientPermissionsException : AuthenticationException {
    /// <summary>
    /// Gets the required permission that is missing.
    /// </summary>
    public string? RequiredPermission { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="InsufficientPermissionsException"/> class.
    /// </summary>
    public InsufficientPermissionsException()
        : base("The authenticated user does not have sufficient permissions.") {
        IsCredentialError = false; // Not a credential error, but a permission issue
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="InsufficientPermissionsException"/> class with a required permission.
    /// </summary>
    /// <param name="requiredPermission">The required permission that is missing.</param>
    public InsufficientPermissionsException(string requiredPermission)
        : base($"The authenticated user does not have the required '{requiredPermission}' permission.") {
        RequiredPermission = requiredPermission;
        IsCredentialError = false;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="InsufficientPermissionsException"/> class with username, host, and required permission.
    /// </summary>
    /// <param name="username">The username.</param>
    /// <param name="host">The server host.</param>
    /// <param name="requiredPermission">The required permission that is missing.</param>
    public InsufficientPermissionsException(string username, string host, string requiredPermission)
        : base($"User '{username}' on {host} does not have the required '{requiredPermission}' permission.", username, host, isCredentialError: false) {
        RequiredPermission = requiredPermission;
    }
}
