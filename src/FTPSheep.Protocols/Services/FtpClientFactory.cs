using FluentFTP;
using FTPSheep.Protocols.Models;
using Microsoft.Extensions.Logging;
using IFtpClient = FTPSheep.Protocols.Interfaces.IFtpClient;

namespace FTPSheep.Protocols.Services;

/// <summary>
/// Factory for creating FTP/SFTP clients based on protocol configuration.
/// Implements the factory pattern for protocol abstraction.
/// </summary>
public class FtpClientFactory(ILoggerFactory loggerFactory) {
    /// <summary>
    /// Creates an FTP client based on the specified configuration.
    /// </summary>
    /// <param name="config">The FTP connection configuration.</param>
    /// <returns>An instance of IFtpClient.</returns>
    /// <exception cref="ArgumentNullException">Thrown when config is null.</exception>
    public IFtpClient CreateClient(FtpConnectionConfig config) {
        ArgumentNullException.ThrowIfNull(config);

        // FtpClientService handles both FTP and FTPS via EncryptionMode
        // Future: Add SFTP support when SSH.NET integration is implemented
        return new FtpClientService(config, loggerFactory.CreateLogger<IFtpClient>());
    }

    /// <summary>
    /// Creates an FTP client with the specified connection parameters.
    /// </summary>
    /// <param name="host">The FTP server host.</param>
    /// <param name="port">The FTP server port.</param>
    /// <param name="username">The username.</param>
    /// <param name="password">The password.</param>
    /// <param name="encryptionMode">The encryption mode (None, Explicit, Implicit).</param>
    /// <param name="remoteRootPath">Optional remote root path.</param>
    /// <returns>An instance of IFtpClient.</returns>
    public IFtpClient CreateClient(
        string host,
        int port,
        string username,
        string password,
        FtpEncryptionMode encryptionMode = FtpEncryptionMode.None,
        string? remoteRootPath = null) {
        var config = new FtpConnectionConfig {
            Host = host,
            Port = port,
            Username = username,
            Password = password,
            EncryptionMode = encryptionMode,
            RemoteRootPath = remoteRootPath
        };

        return CreateClient(config);
    }

    /// <summary>
    /// Checks if FTP/FTPS is supported (always true for now).
    /// Future: Will check for SFTP support as well.
    /// </summary>
    /// <param name="encryptionMode">The encryption mode.</param>
    /// <returns>True if the encryption mode is supported.</returns>
    public static bool IsEncryptionModeSupported(FtpEncryptionMode encryptionMode) {
        // All FtpEncryptionMode values are supported (None, Explicit, Implicit)
        return true;
    }

    /// <summary>
    /// Gets a list of supported encryption modes.
    /// </summary>
    /// <returns>Array of supported encryption mode names.</returns>
    public static string[] GetSupportedEncryptionModes() {
        return new[] { "None (FTP)", "Explicit (FTPS)", "Implicit (FTPS)" };
    }
}
