using System.Security.Cryptography;
using FTPSheep.Core.Services;
using Xunit;

namespace FTPSheep.Tests.Services;

public class DpapiEncryptionServiceTests
{
    private readonly DpapiEncryptionService _encryptionService;

    public DpapiEncryptionServiceTests()
    {
        _encryptionService = new DpapiEncryptionService();
    }

    [Fact]
    public void Encrypt_WithValidPlainText_ReturnsBase64String()
    {
        // Arrange
        var plainText = "MySecretPassword123";

        // Act
        var encrypted = _encryptionService.Encrypt(plainText);

        // Assert
        Assert.NotNull(encrypted);
        Assert.NotEmpty(encrypted);
        Assert.NotEqual(plainText, encrypted);

        // Verify it's valid Base64
        var bytes = Convert.FromBase64String(encrypted);
        Assert.NotEmpty(bytes);
    }

    [Fact]
    public void Decrypt_WithValidEncryptedText_ReturnsOriginalPlainText()
    {
        // Arrange
        var plainText = "MySecretPassword123";
        var encrypted = _encryptionService.Encrypt(plainText);

        // Act
        var decrypted = _encryptionService.Decrypt(encrypted);

        // Assert
        Assert.Equal(plainText, decrypted);
    }

    [Theory]
    [InlineData("password")]
    [InlineData("P@ssw0rd!@#$%^&*()")]
    [InlineData("very long password with many characters 1234567890")]
    [InlineData("🔒🔑")]  // Unicode emoji
    public void EncryptDecrypt_RoundTrip_PreservesOriginalValue(string plainText)
    {
        // Act
        var encrypted = _encryptionService.Encrypt(plainText);
        var decrypted = _encryptionService.Decrypt(encrypted);

        // Assert
        Assert.Equal(plainText, decrypted);
    }

    [Fact]
    public void Encrypt_WithNullPlainText_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => _encryptionService.Encrypt(null!));
        Assert.Contains("Plain text cannot be null or empty", exception.Message);
    }

    [Fact]
    public void Encrypt_WithEmptyPlainText_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => _encryptionService.Encrypt(string.Empty));
        Assert.Contains("Plain text cannot be null or empty", exception.Message);
    }

    [Fact]
    public void Decrypt_WithNullEncryptedText_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => _encryptionService.Decrypt(null!));
        Assert.Contains("Encrypted text cannot be null or empty", exception.Message);
    }

    [Fact]
    public void Decrypt_WithEmptyEncryptedText_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => _encryptionService.Decrypt(string.Empty));
        Assert.Contains("Encrypted text cannot be null or empty", exception.Message);
    }

    [Fact]
    public void Decrypt_WithInvalidBase64_ThrowsCryptographicException()
    {
        // Arrange
        var invalidBase64 = "This is not valid Base64!@#$";

        // Act & Assert
        var exception = Assert.Throws<CryptographicException>(() => _encryptionService.Decrypt(invalidBase64));
        Assert.Contains("not a valid Base64 string", exception.Message);
    }

    [Fact]
    public void Decrypt_WithValidBase64ButInvalidCiphertext_ThrowsCryptographicException()
    {
        // Arrange
        // This is valid Base64 but not encrypted with DPAPI
        var invalidCiphertext = Convert.ToBase64String(new byte[] { 1, 2, 3, 4, 5 });

        // Act & Assert
        var exception = Assert.Throws<CryptographicException>(() => _encryptionService.Decrypt(invalidCiphertext));
        Assert.Contains("Failed to decrypt data using DPAPI", exception.Message);
    }

    [Fact]
    public void Encrypt_SamePlainTextMultipleTimes_ProducesDifferentCiphertext()
    {
        // Arrange
        var plainText = "MySecretPassword";

        // Act
        var encrypted1 = _encryptionService.Encrypt(plainText);
        var encrypted2 = _encryptionService.Encrypt(plainText);

        // Assert - DPAPI should produce different ciphertext each time (due to random IV)
        Assert.NotEqual(encrypted1, encrypted2);

        // But both should decrypt to the same plaintext
        Assert.Equal(plainText, _encryptionService.Decrypt(encrypted1));
        Assert.Equal(plainText, _encryptionService.Decrypt(encrypted2));
    }

    [Fact]
    public void IsAvailable_OnWindows_ReturnsTrue()
    {
        // Act
        var isAvailable = DpapiEncryptionService.IsAvailable();

        // Assert
        Assert.Equal(OperatingSystem.IsWindows(), isAvailable);
    }
}
