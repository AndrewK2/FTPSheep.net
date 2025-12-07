using FluentFTP;

namespace FTPSheep.Protocols.Models;

/// <summary>
/// Configuration for FTP connection.
/// </summary>
public class FtpConnectionConfig
{
    /// <summary>
    /// Gets or sets the FTP server hostname or IP address.
    /// </summary>
    public string Host { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the FTP server port (default: 21).
    /// </summary>
    public int Port { get; set; } = 21;

    /// <summary>
    /// Gets or sets the username for authentication.
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the password for authentication.
    /// </summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the FTP data connection mode (Passive or Active).
    /// </summary>
    public FtpDataConnectionType DataConnectionMode { get; set; } = FtpDataConnectionType.AutoPassive;

    /// <summary>
    /// Gets or sets the encryption mode (None, Explicit, Implicit).
    /// </summary>
    public FtpEncryptionMode EncryptionMode { get; set; } = FtpEncryptionMode.None;

    /// <summary>
    /// Gets or sets the connection timeout in seconds (default: 30).
    /// </summary>
    public int ConnectionTimeout { get; set; } = 30;

    /// <summary>
    /// Gets or sets the data connection timeout in seconds (default: 120).
    /// </summary>
    public int DataConnectionTimeout { get; set; } = 120;

    /// <summary>
    /// Gets or sets the number of retry attempts for transient failures (default: 3).
    /// </summary>
    public int RetryAttempts { get; set; } = 3;

    /// <summary>
    /// Gets or sets whether to use SSL/TLS certificate validation.
    /// </summary>
    public bool ValidateCertificate { get; set; } = true;

    /// <summary>
    /// Gets or sets the remote root directory path (optional).
    /// </summary>
    public string? RemoteRootPath { get; set; }

    /// <summary>
    /// Gets or sets whether to keep the connection alive.
    /// </summary>
    public bool KeepAlive { get; set; } = true;

    /// <summary>
    /// Gets or sets the encoding to use for file names and paths.
    /// </summary>
    public string Encoding { get; set; } = "UTF-8";
}
