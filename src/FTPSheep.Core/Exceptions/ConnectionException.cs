namespace FTPSheep.Core.Exceptions;

/// <summary>
/// Base exception for all connection-related errors.
/// </summary>
public class ConnectionException : Exception {
    /// <summary>
    /// Gets the server host associated with this exception.
    /// </summary>
    public string? Host { get; }

    /// <summary>
    /// Gets the port associated with this exception.
    /// </summary>
    public int? Port { get; }

    /// <summary>
    /// Gets a value indicating whether this is a transient error that may succeed on retry.
    /// </summary>
    public bool IsTransient { get; init; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConnectionException"/> class.
    /// </summary>
    public ConnectionException() {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConnectionException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public ConnectionException(string message) : base(message) {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConnectionException"/> class with a specified error message and inner exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public ConnectionException(string message, Exception innerException) : base(message, innerException) {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConnectionException"/> class with host and port.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="host">The server host.</param>
    /// <param name="port">The server port.</param>
    /// <param name="isTransient">Whether this is a transient error.</param>
    public ConnectionException(string message, string host, int port, bool isTransient = false)
        : base(message) {
        Host = host;
        Port = port;
        IsTransient = isTransient;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConnectionException"/> class with host, port, and inner exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="host">The server host.</param>
    /// <param name="port">The server port.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    /// <param name="isTransient">Whether this is a transient error.</param>
    public ConnectionException(string message, string host, int port, Exception innerException, bool isTransient = false)
        : base(message, innerException) {
        Host = host;
        Port = port;
        IsTransient = isTransient;
    }
}

/// <summary>
/// Exception thrown when a connection timeout occurs.
/// </summary>
public class ConnectionTimeoutException : ConnectionException {
    /// <summary>
    /// Gets the timeout duration that was exceeded.
    /// </summary>
    public TimeSpan Timeout { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConnectionTimeoutException"/> class.
    /// </summary>
    /// <param name="timeout">The timeout duration.</param>
    public ConnectionTimeoutException(TimeSpan timeout)
        : base($"Connection timed out after {timeout.TotalSeconds:F1} seconds.") {
        Timeout = timeout;
        IsTransient = true; // Timeouts are often transient
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConnectionTimeoutException"/> class with host and port.
    /// </summary>
    /// <param name="host">The server host.</param>
    /// <param name="port">The server port.</param>
    /// <param name="timeout">The timeout duration.</param>
    public ConnectionTimeoutException(string host, int port, TimeSpan timeout)
        : base($"Connection to {host}:{port} timed out after {timeout.TotalSeconds:F1} seconds.", host, port, isTransient: true) {
        Timeout = timeout;
    }
}

/// <summary>
/// Exception thrown when the server refuses the connection.
/// </summary>
public class ConnectionRefusedException : ConnectionException {
    /// <summary>
    /// Initializes a new instance of the <see cref="ConnectionRefusedException"/> class.
    /// </summary>
    public ConnectionRefusedException() {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConnectionRefusedException"/> class with host and port.
    /// </summary>
    /// <param name="host">The server host.</param>
    /// <param name="port">The server port.</param>
    public ConnectionRefusedException(string host, int port)
        : base($"Connection to {host}:{port} was refused. Ensure the server is running and accessible.", host, port, isTransient: false) {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConnectionRefusedException"/> class with host, port, and inner exception.
    /// </summary>
    /// <param name="host">The server host.</param>
    /// <param name="port">The server port.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public ConnectionRefusedException(string host, int port, Exception innerException)
        : base($"Connection to {host}:{port} was refused. Ensure the server is running and accessible.", host, port, innerException, isTransient: false) {
    }
}

/// <summary>
/// Exception thrown when SSL/TLS certificate validation fails.
/// </summary>
public class SslCertificateException : ConnectionException {
    /// <summary>
    /// Gets the certificate validation error.
    /// </summary>
    public string? ValidationError { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SslCertificateException"/> class.
    /// </summary>
    public SslCertificateException() {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SslCertificateException"/> class with a validation error.
    /// </summary>
    /// <param name="validationError">The certificate validation error.</param>
    public SslCertificateException(string validationError)
        : base($"SSL certificate validation failed: {validationError}") {
        ValidationError = validationError;
        IsTransient = false; // Certificate errors are not transient
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SslCertificateException"/> class with host and validation error.
    /// </summary>
    /// <param name="host">The server host.</param>
    /// <param name="validationError">The certificate validation error.</param>
    public SslCertificateException(string host, string validationError)
        : base($"SSL certificate validation failed for {host}: {validationError}", host, 0, isTransient: false) {
        ValidationError = validationError;
    }
}
