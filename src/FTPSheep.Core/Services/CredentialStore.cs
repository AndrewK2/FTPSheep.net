using System.Security.Cryptography;
using System.Text.Json;
using FTPSheep.Core.Exceptions;
using FTPSheep.Core.Interfaces;
using FTPSheep.Core.Utils;

namespace FTPSheep.Core.Services;

/// <summary>
/// Provides secure credential storage and retrieval with DPAPI encryption.
/// Also supports loading credentials from environment variables.
/// </summary>
public sealed class CredentialStore : ICredentialStore {
    private readonly DpapiEncryptionService encryptionService;

    private const string EnvironmentUsernameKey = "FTP_USERNAME";
    private const string EnvironmentPasswordKey = "FTP_PASSWORD";

    /// <summary>
    /// Initializes a new instance of the <see cref="CredentialStore"/> class.
    /// </summary>
    public CredentialStore()
        : this(new DpapiEncryptionService()) {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CredentialStore"/> class.
    /// </summary>
    /// <param name="encryptionService">The encryption service.</param>
    internal CredentialStore(DpapiEncryptionService encryptionService) {
        this.encryptionService = encryptionService ?? throw new ArgumentNullException(nameof(encryptionService));
    }

    /// <inheritdoc />
    public async Task SaveCredentialsAsync(
        string profileName,
        string username,
        string password,
        CancellationToken cancellationToken = default) {
        if(string.IsNullOrWhiteSpace(profileName)) {
            throw new ArgumentException("Profile name cannot be null or empty.", nameof(profileName));
        }

        if(string.IsNullOrWhiteSpace(username)) {
            throw new ArgumentException("Username cannot be null or empty.", nameof(username));
        }

        if(string.IsNullOrWhiteSpace(password)) {
            throw new ArgumentException("Password cannot be null or empty.", nameof(password));
        }

        // Validate DPAPI is available
        if(!DpapiEncryptionService.IsAvailable()) {
            throw new PlatformNotSupportedException(
                "DPAPI encryption is only available on Windows. Cannot save credentials securely on this platform.");
        }

        try {
            var credentialFilePath = GetCredentialFilePath(profileName);
            var directory = Path.GetDirectoryName(credentialFilePath);

            if(!string.IsNullOrEmpty(directory) && !Directory.Exists(directory)) {
                Directory.CreateDirectory(directory);
            }

            // Encrypt the password
            var encryptedPassword = encryptionService.Encrypt(password);

            // Create the credential data
            var credentialData = new CredentialData {
                Username = username,
                EncryptedPassword = encryptedPassword,
                CreatedAt = DateTime.UtcNow
            };

            // Serialize and save
            var json = JsonSerializer.Serialize(credentialData, new JsonSerializerOptions {
                WriteIndented = true
            });

            await File.WriteAllTextAsync(credentialFilePath, json, cancellationToken);
        } catch(CryptographicException ex) {
            throw new ConfigurationException(
                $"Failed to encrypt credentials for profile '{profileName}': {ex.Message}", ex);
        } catch(IOException ex) {
            throw new ConfigurationException(
                $"Failed to save credentials for profile '{profileName}': {ex.Message}", ex);
        }
    }

    /// <inheritdoc />
    public async Task<Credentials?> LoadCredentialsAsync(
        string profileName,
        CancellationToken cancellationToken = default) {
        if(string.IsNullOrWhiteSpace(profileName)) {
            throw new ArgumentException("Profile name cannot be null or empty.", nameof(profileName));
        }

        // First, check environment variables (they override stored credentials)
        var envCredentials = LoadCredentialsFromEnvironment();
        if(envCredentials != null) {
            return envCredentials;
        }

        // Then check stored credentials
        var credentialFilePath = GetCredentialFilePath(profileName);
        if(!File.Exists(credentialFilePath)) {
            return null;
        }

        try {
            var json = await File.ReadAllTextAsync(credentialFilePath, cancellationToken);
            var credentialData = JsonSerializer.Deserialize<CredentialData>(json);

            if(credentialData == null) {
                return null;
            }

            // Decrypt the password
            var decryptedPassword = encryptionService.Decrypt(credentialData.EncryptedPassword);

            return new Credentials(credentialData.Username, decryptedPassword);
        } catch(CryptographicException ex) {
            throw new ConfigurationException(
                $"Failed to decrypt credentials for profile '{profileName}'. " +
                "The credentials may have been encrypted by a different user or on a different machine.", ex);
        } catch(JsonException ex) {
            throw new ConfigurationException(
                $"Failed to parse credentials file for profile '{profileName}': {ex.Message}", ex);
        } catch(IOException ex) {
            throw new ConfigurationException(
                $"Failed to load credentials for profile '{profileName}': {ex.Message}", ex);
        }
    }

    /// <inheritdoc />
    public Task DeleteCredentialsAsync(
        string profileName,
        CancellationToken cancellationToken = default) {
        if(string.IsNullOrWhiteSpace(profileName)) {
            throw new ArgumentException("Profile name cannot be null or empty.", nameof(profileName));
        }

        var credentialFilePath = GetCredentialFilePath(profileName);
        if(File.Exists(credentialFilePath)) {
            try {
                File.Delete(credentialFilePath);
            } catch(IOException ex) {
                throw new ConfigurationException(
                    $"Failed to delete credentials for profile '{profileName}': {ex.Message}", ex);
            }
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<bool> HasCredentialsAsync(
        string profileName,
        CancellationToken cancellationToken = default) {
        if(string.IsNullOrWhiteSpace(profileName)) {
            throw new ArgumentException("Profile name cannot be null or empty.", nameof(profileName));
        }

        // Check environment variables first
        var hasEnvCredentials = HasCredentialsInEnvironment();
        if(hasEnvCredentials) {
            return Task.FromResult(true);
        }

        // Check stored credentials
        var credentialFilePath = GetCredentialFilePath(profileName);
        return Task.FromResult(File.Exists(credentialFilePath));
    }

    /// <inheritdoc />
    public string EncryptPassword(string plainText) {
        if(string.IsNullOrEmpty(plainText)) {
            throw new ArgumentException("Plain text cannot be null or empty.", nameof(plainText));
        }

        if(!DpapiEncryptionService.IsAvailable()) {
            throw new PlatformNotSupportedException(
                "DPAPI encryption is only available on Windows.");
        }

        return encryptionService.Encrypt(plainText);
    }

    /// <inheritdoc />
    public string DecryptPassword(string encryptedText) {
        if(string.IsNullOrEmpty(encryptedText)) {
            throw new ArgumentException("Encrypted text cannot be null or empty.", nameof(encryptedText));
        }

        return encryptionService.Decrypt(encryptedText);
    }

    private static string GetCredentialFilePath(string profileName) {
        var credentialsDirectory = PathResolver.GetCredentialsDirectoryPath();
        return Path.Combine(credentialsDirectory, $"{profileName}.cred.json");
    }

    private static Credentials? LoadCredentialsFromEnvironment() {
        var username = Environment.GetEnvironmentVariable(EnvironmentUsernameKey);
        var password = Environment.GetEnvironmentVariable(EnvironmentPasswordKey);

        if(!string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(password)) {
            return new Credentials(username, password);
        }

        return null;
    }

    private static bool HasCredentialsInEnvironment() {
        var username = Environment.GetEnvironmentVariable(EnvironmentUsernameKey);
        var password = Environment.GetEnvironmentVariable(EnvironmentPasswordKey);

        return !string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(password);
    }

    /// <summary>
    /// Internal model for credential data storage.
    /// </summary>
    private sealed class CredentialData {
        public string Username { get; set; } = string.Empty;
        public string EncryptedPassword { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
