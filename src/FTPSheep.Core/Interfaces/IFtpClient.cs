namespace FTPSheep.Core.Interfaces;

/// <summary>
/// Defines the contract for FTP/SFTP client operations.
/// </summary>
public interface IFtpClient : IDisposable {
    /// <summary>
    /// Connects to the FTP server.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ConnectAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Disconnects from the FTP server.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task DisconnectAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Tests the connection to the FTP server.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if connection is successful, false otherwise.</returns>
    Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Uploads a file to the FTP server.
    /// </summary>
    /// <param name="localPath">The local file path.</param>
    /// <param name="remotePath">The remote file path.</param>
    /// <param name="progress">Optional progress reporter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task UploadFileAsync(
        string localPath,
        string remotePath,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Downloads a file from the FTP server.
    /// </summary>
    /// <param name="remotePath">The remote file path.</param>
    /// <param name="localPath">The local file path.</param>
    /// <param name="progress">Optional progress reporter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task DownloadFileAsync(
        string remotePath,
        string localPath,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a file from the FTP server.
    /// </summary>
    /// <param name="remotePath">The remote file path.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task DeleteFileAsync(string remotePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a directory on the FTP server.
    /// </summary>
    /// <param name="remotePath">The remote directory path.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task CreateDirectoryAsync(string remotePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a directory from the FTP server.
    /// </summary>
    /// <param name="remotePath">The remote directory path.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task DeleteDirectoryAsync(string remotePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists files in a remote directory.
    /// </summary>
    /// <param name="remotePath">The remote directory path.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of file paths.</returns>
    Task<IReadOnlyList<string>> ListFilesAsync(
        string remotePath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a file exists on the FTP server.
    /// </summary>
    /// <param name="remotePath">The remote file path.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the file exists, false otherwise.</returns>
    Task<bool> FileExistsAsync(string remotePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a directory exists on the FTP server.
    /// </summary>
    /// <param name="remotePath">The remote directory path.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the directory exists, false otherwise.</returns>
    Task<bool> DirectoryExistsAsync(string remotePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets server information.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Server information string.</returns>
    Task<string> GetServerInfoAsync(CancellationToken cancellationToken = default);
}
