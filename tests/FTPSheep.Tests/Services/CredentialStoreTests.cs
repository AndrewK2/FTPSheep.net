using FTPSheep.Core.Interfaces;
using FTPSheep.Core.Services;
using FTPSheep.Core.Utils;

namespace FTPSheep.Tests.Services;

public class CredentialStoreTests : IDisposable {
    private readonly CredentialStore _credentialStore;
    private readonly string _testProfileName;

    public CredentialStoreTests() {
        _credentialStore = new CredentialStore();
        _testProfileName = "test-profile";
    }

    public void Dispose() {
        // Clean up any credentials created during tests
        var credentialFile = GetTestCredentialFilePath();

        if(File.Exists(credentialFile)) {
            try {
                File.Delete(credentialFile);
            } catch {
                // Ignore cleanup errors
            }
        }

        // Clean up environment variables
        Environment.SetEnvironmentVariable("FTP_USERNAME", null);
        Environment.SetEnvironmentVariable("FTP_PASSWORD", null);
    }

    private string GetTestCredentialFilePath() {
        var credentialsDirectory = PathResolver.GetCredentialsDirectoryPath();
        return Path.Combine(credentialsDirectory, $"{_testProfileName}.cred.json");
    }

    [Fact]
    public async Task SaveCredentialsAsync_WithValidInputs_SavesEncryptedCredentials() {
        // Arrange
        var username = "testuser";
        var password = "testpassword";

        // Ensure DPAPI is available (Windows only)
        if(!DpapiEncryptionService.IsAvailable()) {
            return; // Skip test on non-Windows platforms
        }

        try {
            // Act
            await _credentialStore.SaveCredentialsAsync(_testProfileName, username, password);

            // Assert
            var credentialFile = GetTestCredentialFilePath();
            Assert.True(File.Exists(credentialFile));

            // Verify the file contains encrypted password
            var fileContent = await File.ReadAllTextAsync(credentialFile);
            Assert.Contains(username, fileContent);
            Assert.DoesNotContain(password, fileContent); // Password should be encrypted, not plain text
        } finally {
            await _credentialStore.DeleteCredentialsAsync(_testProfileName);
        }
    }

    [Fact]
    public async Task SaveCredentialsAsync_WithNullProfileName_ThrowsArgumentException() {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _credentialStore.SaveCredentialsAsync(null!, "user", "pass"));
    }

    [Fact]
    public async Task SaveCredentialsAsync_WithEmptyUsername_ThrowsArgumentException() {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _credentialStore.SaveCredentialsAsync(_testProfileName, "", "pass"));
    }

    [Fact]
    public async Task SaveCredentialsAsync_WithEmptyPassword_ThrowsArgumentException() {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _credentialStore.SaveCredentialsAsync(_testProfileName, "user", ""));
    }

    [Fact]
    public async Task LoadCredentialsAsync_WhenCredentialsExist_ReturnsDecryptedCredentials() {
        // Arrange
        var username = "testuser";
        var password = "testpassword";

        if(!DpapiEncryptionService.IsAvailable()) {
            return; // Skip test on non-Windows platforms
        }

        try {
            await _credentialStore.SaveCredentialsAsync(_testProfileName, username, password);

            // Act
            var credentials = await _credentialStore.LoadCredentialsAsync(_testProfileName);

            // Assert
            Assert.NotNull(credentials);
            Assert.Equal(username, credentials.Username);
            Assert.Equal(password, credentials.Password);
        } finally {
            await _credentialStore.DeleteCredentialsAsync(_testProfileName);
        }
    }

    [Fact]
    public async Task LoadCredentialsAsync_WhenCredentialsDoNotExist_ReturnsNull() {
        // Arrange
        var nonExistentProfile = "non-existent-profile";

        // Act
        var credentials = await _credentialStore.LoadCredentialsAsync(nonExistentProfile);

        // Assert
        Assert.Null(credentials);
    }

    [Fact]
    public async Task LoadCredentialsAsync_WithEnvironmentVariables_ReturnsEnvironmentCredentials() {
        // Arrange
        var envUsername = "env-user";
        var envPassword = "env-password";

        Environment.SetEnvironmentVariable("FTP_USERNAME", envUsername);
        Environment.SetEnvironmentVariable("FTP_PASSWORD", envPassword);

        try {
            // Act
            var credentials = await _credentialStore.LoadCredentialsAsync(_testProfileName);

            // Assert
            Assert.NotNull(credentials);
            Assert.Equal(envUsername, credentials.Username);
            Assert.Equal(envPassword, credentials.Password);
        } finally {
            // Clean up environment variables
            Environment.SetEnvironmentVariable("FTP_USERNAME", null);
            Environment.SetEnvironmentVariable("FTP_PASSWORD", null);
        }
    }

    [Fact]
    public async Task LoadCredentialsAsync_EnvironmentVariablesOverrideStoredCredentials() {
        // Arrange
        var storedUsername = "stored-user";
        var storedPassword = "stored-password";
        var envUsername = "env-user";
        var envPassword = "env-password";

        if(!DpapiEncryptionService.IsAvailable()) {
            return; // Skip test on non-Windows platforms
        }

        try {
            // Save credentials to file
            await _credentialStore.SaveCredentialsAsync(_testProfileName, storedUsername, storedPassword);

            // Set environment variables
            Environment.SetEnvironmentVariable("FTP_USERNAME", envUsername);
            Environment.SetEnvironmentVariable("FTP_PASSWORD", envPassword);

            // Act
            var credentials = await _credentialStore.LoadCredentialsAsync(_testProfileName);

            // Assert - Environment variables should take precedence
            Assert.NotNull(credentials);
            Assert.Equal(envUsername, credentials.Username);
            Assert.Equal(envPassword, credentials.Password);
        } finally {
            await _credentialStore.DeleteCredentialsAsync(_testProfileName);
            Environment.SetEnvironmentVariable("FTP_USERNAME", null);
            Environment.SetEnvironmentVariable("FTP_PASSWORD", null);
        }
    }

    [Fact]
    public async Task DeleteCredentialsAsync_WhenCredentialsExist_DeletesFile() {
        // Arrange
        if(!DpapiEncryptionService.IsAvailable()) {
            return; // Skip test on non-Windows platforms
        }

        await _credentialStore.SaveCredentialsAsync(_testProfileName, "user", "pass");
        var credentialFile = GetTestCredentialFilePath();
        Assert.True(File.Exists(credentialFile));

        // Act
        await _credentialStore.DeleteCredentialsAsync(_testProfileName);

        // Assert
        Assert.False(File.Exists(credentialFile));
    }

    [Fact]
    public async Task DeleteCredentialsAsync_WhenCredentialsDoNotExist_DoesNotThrow() {
        // Arrange
        var nonExistentProfile = "non-existent-profile";

        // Act & Assert - Should not throw
        await _credentialStore.DeleteCredentialsAsync(nonExistentProfile);
    }

    [Fact]
    public async Task HasCredentialsAsync_WhenCredentialsExist_ReturnsTrue() {
        // Arrange
        if(!DpapiEncryptionService.IsAvailable()) {
            return; // Skip test on non-Windows platforms
        }

        try {
            await _credentialStore.SaveCredentialsAsync(_testProfileName, "user", "pass");

            // Act
            var hasCredentials = await _credentialStore.HasCredentialsAsync(_testProfileName);

            // Assert
            Assert.True(hasCredentials);
        } finally {
            await _credentialStore.DeleteCredentialsAsync(_testProfileName);
        }
    }

    [Fact]
    public async Task HasCredentialsAsync_WhenCredentialsDoNotExist_ReturnsFalse() {
        // Arrange
        var nonExistentProfile = "non-existent-profile";

        // Act
        var hasCredentials = await _credentialStore.HasCredentialsAsync(nonExistentProfile);

        // Assert
        Assert.False(hasCredentials);
    }

    [Fact]
    public async Task HasCredentialsAsync_WithEnvironmentVariables_ReturnsTrue() {
        // Arrange
        Environment.SetEnvironmentVariable("FTP_USERNAME", "user");
        Environment.SetEnvironmentVariable("FTP_PASSWORD", "pass");

        try {
            // Act
            var hasCredentials = await _credentialStore.HasCredentialsAsync(_testProfileName);

            // Assert
            Assert.True(hasCredentials);
        } finally {
            Environment.SetEnvironmentVariable("FTP_USERNAME", null);
            Environment.SetEnvironmentVariable("FTP_PASSWORD", null);
        }
    }

    [Fact]
    public void EncryptPassword_WithValidPlainText_ReturnsEncryptedString() {
        // Arrange
        if(!DpapiEncryptionService.IsAvailable()) {
            return; // Skip test on non-Windows platforms
        }

        var plainText = "password123";

        // Act
        var encrypted = _credentialStore.EncryptPassword(plainText);

        // Assert
        Assert.NotNull(encrypted);
        Assert.NotEqual(plainText, encrypted);
    }

    [Fact]
    public void DecryptPassword_WithValidEncryptedText_ReturnsPlainText() {
        // Arrange
        if(!DpapiEncryptionService.IsAvailable()) {
            return; // Skip test on non-Windows platforms
        }

        var plainText = "password123";
        var encrypted = _credentialStore.EncryptPassword(plainText);

        // Act
        var decrypted = _credentialStore.DecryptPassword(encrypted);

        // Assert
        Assert.Equal(plainText, decrypted);
    }

    [Fact]
    public void Credentials_Clear_ClearsPassword() {
        // Arrange
        var credentials = new Credentials("user", "password");

        // Act
        credentials.Clear();

        // Assert
        Assert.Equal(string.Empty, credentials.Password);
        Assert.Equal("user", credentials.Username); // Username should not be cleared
    }

    [Fact]
    public async Task SaveAndLoad_RoundTrip_WithSpecialCharactersInPassword() {
        // Arrange
        if(!DpapiEncryptionService.IsAvailable()) {
            return; // Skip test on non-Windows platforms
        }

        var username = "testuser";
        var password = "P@ssw0rd!@#$%^&*()_+-={}[]|\\:\"<>?,./";

        try {
            // Act
            await _credentialStore.SaveCredentialsAsync(_testProfileName, username, password);
            var credentials = await _credentialStore.LoadCredentialsAsync(_testProfileName);

            // Assert
            Assert.NotNull(credentials);
            Assert.Equal(username, credentials.Username);
            Assert.Equal(password, credentials.Password);
        } finally {
            await _credentialStore.DeleteCredentialsAsync(_testProfileName);
        }
    }

    [Fact]
    public async Task SaveAndLoad_RoundTrip_WithUnicodeCharacters() {
        // Arrange
        if(!DpapiEncryptionService.IsAvailable()) {
            return; // Skip test on non-Windows platforms
        }

        var username = "用户名"; // Chinese characters
        var password = "密碼🔒🔑"; // Chinese characters and emojis

        try {
            // Act
            await _credentialStore.SaveCredentialsAsync(_testProfileName, username, password);
            var credentials = await _credentialStore.LoadCredentialsAsync(_testProfileName);

            // Assert
            Assert.NotNull(credentials);
            Assert.Equal(username, credentials.Username);
            Assert.Equal(password, credentials.Password);
        } finally {
            await _credentialStore.DeleteCredentialsAsync(_testProfileName);
        }
    }
}