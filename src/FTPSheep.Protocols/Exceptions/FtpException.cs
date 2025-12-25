namespace FTPSheep.Protocols.Exceptions;

/// <summary>
/// Base exception for FTP-related errors.
/// </summary>
public class FtpException : Exception {
    /// <summary>
    /// Initializes a new instance of the <see cref="FtpException"/> class.
    /// </summary>
    public FtpException() {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FtpException"/> class with a message.
    /// </summary>
    /// <param name="message">The error message.</param>
    public FtpException(string message) : base(message) {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FtpException"/> class with a message and inner exception.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public FtpException(string message, Exception innerException) : base(message, innerException) {
    }
}
