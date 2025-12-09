using System.Security.Cryptography;
using System.Text;

namespace FTPSheep.Core.Services;

/// <summary>
/// Provides encryption and decryption services using Windows Data Protection API (DPAPI).
/// </summary>
/// <remarks>
/// DPAPI encrypts data using the current Windows user's credentials, ensuring that
/// only the same user on the same machine can decrypt the data.
/// This is Windows-specific and provides user-scoped encryption.
/// </remarks>
public sealed class DpapiEncryptionService {
    private static readonly byte[] sEntropy = "FTPSheep.Credential.Salt.V1"u8.ToArray();

    /// <summary>
    /// Encrypts a plain text string using DPAPI.
    /// </summary>
    /// <param name="plainText">The plain text to encrypt.</param>
    /// <returns>The encrypted string encoded as Base64.</returns>
    /// <exception cref="ArgumentException">Thrown when plainText is null or empty.</exception>
    /// <exception cref="CryptographicException">Thrown when encryption fails.</exception>
    public string Encrypt(string plainText) {
        if(string.IsNullOrEmpty(plainText)) {
            throw new ArgumentException("Plain text cannot be null or empty.", nameof(plainText));
        }

        try {
            // Convert the plain text to bytes
            var plainBytes = Encoding.UTF8.GetBytes(plainText);

            // Encrypt using DPAPI with CurrentUser scope
            var encryptedBytes = ProtectedData.Protect(plainBytes,
                sEntropy,
                DataProtectionScope.CurrentUser);

            // Convert to Base64 for storage
            return Convert.ToBase64String(encryptedBytes);
        } catch(CryptographicException ex) {
            throw new CryptographicException("Failed to encrypt data using DPAPI. Ensure you are running on Windows.", ex);
        }
    }

    /// <summary>
    /// Decrypts an encrypted string using DPAPI.
    /// </summary>
    /// <param name="encryptedText">The encrypted Base64-encoded string.</param>
    /// <returns>The decrypted plain text string.</returns>
    /// <exception cref="ArgumentException">Thrown when encryptedText is null or empty.</exception>
    /// <exception cref="CryptographicException">Thrown when decryption fails.</exception>
    public string Decrypt(string encryptedText) {
        if(string.IsNullOrEmpty(encryptedText)) {
            throw new ArgumentException("Encrypted text cannot be null or empty.", nameof(encryptedText));
        }

        try {
            // Convert from Base64
            var encryptedBytes = Convert.FromBase64String(encryptedText);

            // Decrypt using DPAPI with CurrentUser scope
            var decryptedBytes = ProtectedData.Unprotect(encryptedBytes,
                sEntropy,
                DataProtectionScope.CurrentUser);

            // Convert back to string
            return Encoding.UTF8.GetString(decryptedBytes);
        } catch(FormatException ex) {
            throw new CryptographicException("The encrypted text is not a valid Base64 string.", ex);
        } catch(CryptographicException ex) {
            throw new CryptographicException("Failed to decrypt data using DPAPI. The data may have been encrypted by a different user or on a different machine.", ex);
        }
    }

    /// <summary>
    /// Checks if DPAPI encryption is available on the current platform.
    /// </summary>
    /// <returns>True if DPAPI is available (Windows only), false otherwise.</returns>
    public static bool IsAvailable() {
        return OperatingSystem.IsWindows();
    }
}