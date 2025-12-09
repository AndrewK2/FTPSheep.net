using FluentFTP;
using FTPSheep.Protocols.Exceptions;
using FTPSheep.Protocols.Models;

namespace FTPSheep.Protocols.Services;

/// <summary>
/// Service for FTP client operations using FluentFTP.
/// </summary>
public class FtpClientService : IDisposable {
    private readonly FtpConnectionConfig config;
    private AsyncFtpClient? client;
    private bool disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="FtpClientService"/> class.
    /// </summary>
    /// <param name="config">The FTP connection configuration.</param>
    public FtpClientService(FtpConnectionConfig config) {
        this.config = config ?? throw new ArgumentNullException(nameof(config));
        ValidateConfig(config);
    }

    /// <summary>
    /// Gets a value indicating whether the client is connected.
    /// </summary>
    public bool IsConnected => client?.IsConnected ?? false;

    /// <summary>
    /// Connects to the FTP server.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task ConnectAsync(CancellationToken cancellationToken = default) {
        try {
            client = new AsyncFtpClient(
                config.Host,
                config.Username,
                config.Password,
                config.Port);

            // Configure client
            client.Config.DataConnectionType = config.DataConnectionMode;
            client.Config.EncryptionMode = config.EncryptionMode;
            client.Config.ConnectTimeout = config.ConnectionTimeout * 1000; // Convert to milliseconds
            client.Config.DataConnectionConnectTimeout = config.DataConnectionTimeout * 1000;
            client.Config.RetryAttempts = config.RetryAttempts;
            client.Config.ValidateAnyCertificate = !config.ValidateCertificate;
            client.Config.SocketKeepAlive = config.KeepAlive;

            await client.Connect(cancellationToken);

            // Change to root directory if specified
            if(!string.IsNullOrWhiteSpace(config.RemoteRootPath)) {
                await client.SetWorkingDirectory(config.RemoteRootPath, cancellationToken);
            }
        } catch(Exception ex) when(ex is not FtpException) {
            throw new FtpException(
                $"Failed to connect to FTP server {config.Host}:{config.Port}",
                ex) {
                Host = config.Host,
                Port = config.Port,
                IsTransient = IsTransientError(ex)
            };
        }
    }

    /// <summary>
    /// Disconnects from the FTP server.
    /// </summary>
    public async Task DisconnectAsync(CancellationToken cancellationToken = default) {
        if(client != null && client.IsConnected) {
            await client.Disconnect(cancellationToken);
        }
    }

    /// <summary>
    /// Uploads a file to the FTP server.
    /// </summary>
    /// <param name="localPath">The local file path.</param>
    /// <param name="remotePath">The remote file path.</param>
    /// <param name="overwrite">Whether to overwrite existing files.</param>
    /// <param name="createRemoteDir">Whether to create remote directories if they don't exist.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The upload result.</returns>
    public async Task<FtpStatus> UploadFileAsync(
        string localPath,
        string remotePath,
        bool overwrite = true,
        bool createRemoteDir = true,
        CancellationToken cancellationToken = default) {
        EnsureConnected();

        try {
            var existsMode = overwrite ? FtpRemoteExists.Overwrite : FtpRemoteExists.Skip;

            var result = await client!.UploadFile(
                localPath,
                remotePath,
                existsMode,
                createRemoteDir,
                token: cancellationToken);

            return result;
        } catch(Exception ex) {
            throw new FtpException(
                $"Failed to upload file {localPath} to {remotePath}",
                ex);
        }
    }

    /// <summary>
    /// Creates a directory on the FTP server.
    /// </summary>
    /// <param name="remotePath">The remote directory path.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task CreateDirectoryAsync(string remotePath, CancellationToken cancellationToken = default) {
        EnsureConnected();

        try {
            await client!.CreateDirectory(remotePath, cancellationToken);
        } catch(Exception ex) {
            throw new FtpException(
                $"Failed to create directory {remotePath}",
                ex);
        }
    }

    /// <summary>
    /// Checks if a directory exists on the FTP server.
    /// </summary>
    /// <param name="remotePath">The remote directory path.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the directory exists; otherwise, false.</returns>
    public async Task<bool> DirectoryExistsAsync(string remotePath, CancellationToken cancellationToken = default) {
        EnsureConnected();

        try {
            return await client!.DirectoryExists(remotePath, cancellationToken);
        } catch(Exception ex) {
            throw new FtpException(
                $"Failed to check if directory exists: {remotePath}",
                ex);
        }
    }

    /// <summary>
    /// Lists files in a remote directory.
    /// </summary>
    /// <param name="remotePath">The remote directory path.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Array of file listings.</returns>
    public async Task<FtpListItem[]> ListDirectoryAsync(
        string remotePath,
        CancellationToken cancellationToken = default) {
        EnsureConnected();

        try {
            return await client!.GetListing(remotePath, cancellationToken);
        } catch(Exception ex) {
            throw new FtpException(
                $"Failed to list directory: {remotePath}",
                ex);
        }
    }

    /// <summary>
    /// Deletes a file from the FTP server.
    /// </summary>
    /// <param name="remotePath">The remote file path.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task DeleteFileAsync(string remotePath, CancellationToken cancellationToken = default) {
        EnsureConnected();

        try {
            await client!.DeleteFile(remotePath, cancellationToken);
        } catch(Exception ex) {
            throw new FtpException(
                $"Failed to delete file: {remotePath}",
                ex);
        }
    }

    /// <summary>
    /// Deletes a directory from the FTP server.
    /// </summary>
    /// <param name="remotePath">The remote directory path.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task DeleteDirectoryAsync(string remotePath, CancellationToken cancellationToken = default) {
        EnsureConnected();

        try {
            await client!.DeleteDirectory(remotePath, cancellationToken);
        } catch(Exception ex) {
            throw new FtpException(
                $"Failed to delete directory: {remotePath}",
                ex);
        }
    }

    /// <summary>
    /// Checks if a file exists on the FTP server.
    /// </summary>
    /// <param name="remotePath">The remote file path.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the file exists; otherwise, false.</returns>
    public async Task<bool> FileExistsAsync(string remotePath, CancellationToken cancellationToken = default) {
        EnsureConnected();

        try {
            return await client!.FileExists(remotePath, cancellationToken);
        } catch(Exception ex) {
            throw new FtpException(
                $"Failed to check if file exists: {remotePath}",
                ex);
        }
    }

    /// <summary>
    /// Gets the size of a remote file.
    /// </summary>
    /// <param name="remotePath">The remote file path.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The file size in bytes.</returns>
    public async Task<long> GetFileSizeAsync(string remotePath, CancellationToken cancellationToken = default) {
        EnsureConnected();

        try {
            return await client!.GetFileSize(remotePath, -1, cancellationToken);
        } catch(Exception ex) {
            throw new FtpException(
                $"Failed to get file size: {remotePath}",
                ex);
        }
    }

    /// <summary>
    /// Sets file permissions on the FTP server (if supported).
    /// </summary>
    /// <param name="remotePath">The remote file path.</param>
    /// <param name="permissions">The permissions value.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task SetFilePermissionsAsync(
        string remotePath,
        int permissions,
        CancellationToken cancellationToken = default) {
        EnsureConnected();

        try {
            await client!.SetFilePermissions(remotePath, permissions, cancellationToken);
        } catch(Exception ex) {
            // Don't throw - permissions may not be supported
            Console.WriteLine($"Warning: Failed to set permissions on {remotePath}: {ex.Message}");
        }
    }

    /// <summary>
    /// Validates the FTP connection configuration.
    /// </summary>
    private static void ValidateConfig(FtpConnectionConfig config) {
        if(string.IsNullOrWhiteSpace(config.Host)) {
            throw new ArgumentException("Host is required.", nameof(config));
        }

        if(config.Port <= 0 || config.Port > 65535) {
            throw new ArgumentException("Port must be between 1 and 65535.", nameof(config));
        }

        if(string.IsNullOrWhiteSpace(config.Username)) {
            throw new ArgumentException("Username is required.", nameof(config));
        }
    }

    /// <summary>
    /// Ensures the client is connected.
    /// </summary>
    private void EnsureConnected() {
        if(client == null || !client.IsConnected) {
            throw new InvalidOperationException(
                "FTP client is not connected. Call ConnectAsync first.");
        }
    }

    /// <summary>
    /// Determines if an error is transient and can be retried.
    /// </summary>
    private static bool IsTransientError(Exception ex) {
        var message = ex.Message.ToLowerInvariant();
        return message.Contains("timeout") ||
               message.Contains("connection") ||
               message.Contains("network") ||
               message.Contains("temporarily unavailable");
    }

    /// <summary>
    /// Disposes the FTP client.
    /// </summary>
    public void Dispose() {
        if(!disposed) {
            client?.Dispose();
            disposed = true;
        }
        GC.SuppressFinalize(this);
    }
}
