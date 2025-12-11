using FTPSheep.Protocols.Models;

namespace FTPSheep.Protocols.Interfaces;

/// <summary>
/// Protocol-agnostic interface for FTP/SFTP client operations.
/// Abstracts FTP and SFTP implementations behind a common interface.
/// </summary>
public interface IFtpClient : IDisposable {
    /// <summary>
    /// Gets a value indicating whether the client is connected to the server.
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    /// Connects to the FTP/SFTP server.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ConnectAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Disconnects from the FTP/SFTP server.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task DisconnectAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Uploads a file to the server.
    /// </summary>
    /// <param name="localPath">The local file path.</param>
    /// <param name="remotePath">The remote file path.</param>
    /// <param name="overwrite">Whether to overwrite existing files.</param>
    /// <param name="createRemoteDir">Whether to create remote directories if they don't exist.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the file was uploaded successfully; otherwise, false.</returns>
    Task<bool> UploadFileAsync(
        string localPath,
        string remotePath,
        bool overwrite = true,
        bool createRemoteDir = true,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a directory on the server.
    /// </summary>
    /// <param name="remotePath">The remote directory path.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task CreateDirectoryAsync(string remotePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a directory exists on the server.
    /// </summary>
    /// <param name="remotePath">The remote directory path.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the directory exists; otherwise, false.</returns>
    Task<bool> DirectoryExistsAsync(string remotePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists files and directories in a remote directory.
    /// </summary>
    /// <param name="remotePath">The remote directory path.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Array of remote file information.</returns>
    Task<RemoteFileInfo[]> ListDirectoryAsync(
        string remotePath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a file from the server.
    /// </summary>
    /// <param name="remotePath">The remote file path.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task DeleteFileAsync(string remotePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a directory from the server.
    /// </summary>
    /// <param name="remotePath">The remote directory path.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task DeleteDirectoryAsync(string remotePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a file exists on the server.
    /// </summary>
    /// <param name="remotePath">The remote file path.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the file exists; otherwise, false.</returns>
    Task<bool> FileExistsAsync(string remotePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the size of a remote file in bytes.
    /// </summary>
    /// <param name="remotePath">The remote file path.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The file size in bytes.</returns>
    Task<long> GetFileSizeAsync(string remotePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets file permissions on the server (if supported by the protocol).
    /// </summary>
    /// <param name="remotePath">The remote file path.</param>
    /// <param name="permissions">The permissions value (Unix-style, e.g., 644, 755).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SetFilePermissionsAsync(
        string remotePath,
        int permissions,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Tests the connection to the server and validates write permissions.
    /// </summary>
    /// <param name="testPath">Optional path to test write permissions (defaults to root).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the connection is valid and write permissions are confirmed; otherwise, false.</returns>
    Task<bool> TestConnectionAsync(string? testPath = null, CancellationToken cancellationToken = default);
}
