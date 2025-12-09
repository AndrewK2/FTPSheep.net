namespace FTPSheep.Core.Models;

/// <summary>
/// Represents FTP/SFTP server connection settings.
/// </summary>
public sealed class ServerConnection {
    /// <summary>
    /// Gets or sets the server hostname or IP address.
    /// </summary>
    public string Host { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the server port.
    /// </summary>
    /// <remarks>
    /// Default is 21 for FTP, 22 for SFTP.
    /// </remarks>
    public int Port { get; set; } = 21;

    /// <summary>
    /// Gets or sets the protocol to use (FTP or SFTP).
    /// </summary>
    public ProtocolType Protocol { get; set; } = ProtocolType.Ftp;

    /// <summary>
    /// Gets or sets the connection timeout in seconds.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Gets or sets the FTP connection mode (Active or Passive).
    /// </summary>
    /// <remarks>
    /// Only applicable for FTP protocol. Passive mode is recommended for most scenarios.
    /// </remarks>
    public FtpConnectionMode ConnectionMode { get; set; } = FtpConnectionMode.Passive;

    /// <summary>
    /// Gets or sets a value indicating whether to use SSL/TLS encryption.
    /// </summary>
    /// <remarks>
    /// Enables FTPS (FTP over SSL/TLS). Not applicable for SFTP which always uses encryption.
    /// </remarks>
    public bool UseSsl { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether to validate SSL certificates.
    /// </summary>
    /// <remarks>
    /// Set to false to accept self-signed certificates. Use with caution in production.
    /// </remarks>
    public bool ValidateSslCertificate { get; set; } = true;

    /// <summary>
    /// Creates a default ServerConnection instance.
    /// </summary>
    public ServerConnection() {
    }

    /// <summary>
    /// Creates a ServerConnection with the specified host.
    /// </summary>
    /// <param name="host">The server hostname or IP address.</param>
    public ServerConnection(string host) {
        Host = host ?? string.Empty;
    }

    /// <summary>
    /// Creates a ServerConnection with the specified host, port, and protocol.
    /// </summary>
    /// <param name="host">The server hostname or IP address.</param>
    /// <param name="port">The server port.</param>
    /// <param name="protocol">The protocol to use.</param>
    public ServerConnection(string host, int port, ProtocolType protocol) {
        Host = host ?? string.Empty;
        Port = port;
        Protocol = protocol;
    }

    /// <summary>
    /// Gets the connection string representation (for display purposes).
    /// </summary>
    /// <returns>A connection string in the format: protocol://host:port</returns>
    public string GetConnectionString() {
        var protocol = Protocol == ProtocolType.Sftp ? "sftp" : (UseSsl ? "ftps" : "ftp");
        return $"{protocol}://{Host}:{Port}";
    }

    /// <summary>
    /// Validates the server connection settings.
    /// </summary>
    /// <param name="errors">A list of validation error messages.</param>
    /// <returns>True if valid, otherwise false.</returns>
    public bool Validate(out List<string> errors) {
        errors = new List<string>();

        if(string.IsNullOrWhiteSpace(Host)) {
            errors.Add("Server host cannot be empty.");
        }

        if(Port <= 0 || Port > 65535) {
            errors.Add($"Port {Port} is invalid. Must be between 1 and 65535.");
        }

        if(TimeoutSeconds <= 0) {
            errors.Add($"Timeout {TimeoutSeconds} is invalid. Must be greater than 0.");
        }

        // Protocol-specific validation
        if(Protocol == ProtocolType.Ftp) {
            if(Port == 22) {
                errors.Add("Port 22 is typically used for SFTP, but protocol is set to FTP. Did you mean to use SFTP?");
            }
        } else if(Protocol == ProtocolType.Sftp) {
            if(Port == 21) {
                errors.Add("Port 21 is typically used for FTP, but protocol is set to SFTP. Did you mean to use FTP?");
            }
            if(UseSsl) {
                errors.Add("SSL/TLS settings are not applicable for SFTP (SFTP always uses SSH encryption).");
            }
        }

        return errors.Count == 0;
    }

    /// <summary>
    /// Sets default port based on protocol if port is at default value.
    /// </summary>
    public void NormalizePort() {
        if(Port == 21 && Protocol == ProtocolType.Sftp) {
            Port = 22;
        } else if(Port == 22 && Protocol == ProtocolType.Ftp) {
            Port = 21;
        }
    }
}

/// <summary>
/// Defines FTP connection modes.
/// </summary>
public enum FtpConnectionMode {
    /// <summary>
    /// Active mode (server connects to client for data transfer).
    /// </summary>
    Active,

    /// <summary>
    /// Passive mode (client connects to server for data transfer, recommended).
    /// </summary>
    Passive
}
