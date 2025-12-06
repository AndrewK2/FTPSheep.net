namespace FTPSheep.Core.Interfaces;

/// <summary>
/// Defines the contract for secure credential storage and retrieval.
/// </summary>
public interface ICredentialStore
{
    /// <summary>
    /// Saves credentials securely with encryption.
    /// </summary>
    /// <param name="profileName">The profile name to associate with the credentials.</param>
    /// <param name="username">The username to save.</param>
    /// <param name="password">The password to save (will be encrypted).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SaveCredentialsAsync(
        string profileName,
        string username,
        string password,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads credentials securely and decrypts them.
    /// </summary>
    /// <param name="profileName">The profile name to load credentials for.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The credentials, or null if not found.</returns>
    Task<Credentials?> LoadCredentialsAsync(
        string profileName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes saved credentials for a profile.
    /// </summary>
    /// <param name="profileName">The profile name to delete credentials for.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task DeleteCredentialsAsync(
        string profileName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if credentials exist for a profile.
    /// </summary>
    /// <param name="profileName">The profile name to check.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if credentials exist, false otherwise.</returns>
    Task<bool> HasCredentialsAsync(
        string profileName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Encrypts a password string.
    /// </summary>
    /// <param name="plainText">The plain text password.</param>
    /// <returns>The encrypted password string.</returns>
    string EncryptPassword(string plainText);

    /// <summary>
    /// Decrypts an encrypted password string.
    /// </summary>
    /// <param name="encryptedText">The encrypted password.</param>
    /// <returns>The decrypted plain text password.</returns>
    string DecryptPassword(string encryptedText);
}

/// <summary>
/// Represents a set of credentials.
/// </summary>
public sealed class Credentials
{
    /// <summary>
    /// Gets or sets the username.
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the password (decrypted).
    /// </summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Initializes a new instance of the <see cref="Credentials"/> class.
    /// </summary>
    public Credentials()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Credentials"/> class.
    /// </summary>
    /// <param name="username">The username.</param>
    /// <param name="password">The password.</param>
    public Credentials(string username, string password)
    {
        Username = username;
        Password = password;
    }

    /// <summary>
    /// Clears the password from memory.
    /// </summary>
    public void Clear()
    {
        Password = string.Empty;
    }
}
