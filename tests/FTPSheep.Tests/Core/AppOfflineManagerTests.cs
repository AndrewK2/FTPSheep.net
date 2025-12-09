using FTPSheep.Core.Services;
using Xunit;

namespace FTPSheep.Tests.Core;

public class AppOfflineManagerTests {
    #region Constructor Tests

    [Fact]
    public void Constructor_Default_InitializesCorrectly() {
        // Act
        var manager = new AppOfflineManager();

        // Assert
        Assert.NotNull(manager);
    }

    [Fact]
    public void Constructor_WithCustomTemplate_InitializesWithTemplate() {
        // Arrange
        var customTemplate = "<html><body>Custom maintenance page</body></html>";

        // Act
        var manager = new AppOfflineManager(customTemplate);

        // Assert
        Assert.NotNull(manager);
    }

    #endregion

    #region GenerateAppOfflineContent Tests

    [Fact]
    public void GenerateAppOfflineContent_WithDefaultManager_ReturnsDefaultContent() {
        // Arrange
        var manager = new AppOfflineManager();

        // Act
        var content = manager.GenerateAppOfflineContent();

        // Assert
        Assert.NotNull(content);
        Assert.Contains("<html>", content);
        Assert.Contains("Application Offline for Maintenance", content);
        Assert.Contains("FTPSheep.NET", content);
    }

    [Fact]
    public void GenerateAppOfflineContent_WithCustomTemplate_ReturnsCustomContent() {
        // Arrange
        var customTemplate = "<html><body>Custom maintenance page</body></html>";
        var manager = new AppOfflineManager(customTemplate);

        // Act
        var content = manager.GenerateAppOfflineContent();

        // Assert
        Assert.Equal(customTemplate, content);
    }

    [Fact]
    public void GenerateAppOfflineContent_DefaultContent_ContainsRequiredElements() {
        // Arrange
        var manager = new AppOfflineManager();

        // Act
        var content = manager.GenerateAppOfflineContent();

        // Assert
        Assert.Contains("<!DOCTYPE html>", content);
        Assert.Contains("<head>", content);
        Assert.Contains("<body>", content);
        Assert.Contains("<style>", content);
        Assert.Contains("</html>", content);
    }

    #endregion

    #region GenerateErrorAppOfflineContent Tests

    [Fact]
    public void GenerateErrorAppOfflineContent_WithNoMessage_UsesDefaultErrorMessage() {
        // Arrange
        var manager = new AppOfflineManager();

        // Act
        var content = manager.GenerateErrorAppOfflineContent();

        // Assert
        Assert.NotNull(content);
        Assert.Contains("Deployment Failed", content);
        Assert.Contains("An unknown error occurred", content);
    }

    [Fact]
    public void GenerateErrorAppOfflineContent_WithErrorMessage_IncludesMessage() {
        // Arrange
        var manager = new AppOfflineManager();
        var errorMessage = "Connection to FTP server failed";

        // Act
        var content = manager.GenerateErrorAppOfflineContent(errorMessage);

        // Assert
        Assert.Contains(errorMessage, content);
        Assert.Contains("Deployment Failed", content);
    }

    [Fact]
    public void GenerateErrorAppOfflineContent_WithHtmlInMessage_SanitizesHtml() {
        // Arrange
        var manager = new AppOfflineManager();
        var errorMessage = "<script>alert('xss')</script>";

        // Act
        var content = manager.GenerateErrorAppOfflineContent(errorMessage);

        // Assert
        Assert.DoesNotContain("<script>", content);
        Assert.Contains("&lt;script&gt;", content);
    }

    [Fact]
    public void GenerateErrorAppOfflineContent_WithSpecialCharacters_EscapesCorrectly() {
        // Arrange
        var manager = new AppOfflineManager();
        var errorMessage = "Error: <tag> & \"quotes\" & 'apostrophes'";

        // Act
        var content = manager.GenerateErrorAppOfflineContent(errorMessage);

        // Assert
        Assert.Contains("&lt;tag&gt;", content);
        Assert.Contains("&amp;", content);
        Assert.Contains("&quot;", content);
        Assert.Contains("&#39;", content);
    }

    #endregion

    #region CreateAppOfflineFileAsync Tests

    [Fact]
    public async Task CreateAppOfflineFileAsync_CreatesFileInDirectory() {
        // Arrange
        var manager = new AppOfflineManager();
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        try {
            // Act
            var filePath = await manager.CreateAppOfflineFileAsync(tempDir);

            // Assert
            Assert.True(File.Exists(filePath));
            Assert.Equal(Path.Combine(tempDir, "app_offline.htm"), filePath);

            var content = await File.ReadAllTextAsync(filePath);
            Assert.Contains("Application Offline", content);
        } finally {
            // Cleanup
            if(Directory.Exists(tempDir)) {
                Directory.Delete(tempDir, true);
            }
        }
    }

    [Fact]
    public async Task CreateAppOfflineFileAsync_WithErrorFlag_CreatesErrorContent() {
        // Arrange
        var manager = new AppOfflineManager();
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        try {
            // Act
            var filePath = await manager.CreateAppOfflineFileAsync(tempDir, isError: true, errorMessage: "Test error");

            // Assert
            Assert.True(File.Exists(filePath));

            var content = await File.ReadAllTextAsync(filePath);
            Assert.Contains("Deployment Failed", content);
            Assert.Contains("Test error", content);
        } finally {
            // Cleanup
            if(Directory.Exists(tempDir)) {
                Directory.Delete(tempDir, true);
            }
        }
    }

    [Fact]
    public async Task CreateAppOfflineFileAsync_WithNullDirectory_ThrowsArgumentException() {
        // Arrange
        var manager = new AppOfflineManager();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            manager.CreateAppOfflineFileAsync(null!));
    }

    [Fact]
    public async Task CreateAppOfflineFileAsync_WithEmptyDirectory_ThrowsArgumentException() {
        // Arrange
        var manager = new AppOfflineManager();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            manager.CreateAppOfflineFileAsync(string.Empty));
    }

    [Fact]
    public async Task CreateAppOfflineFileAsync_WithNonExistentDirectory_ThrowsDirectoryNotFoundException() {
        // Arrange
        var manager = new AppOfflineManager();
        var nonExistentDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        // Act & Assert
        await Assert.ThrowsAsync<DirectoryNotFoundException>(() =>
            manager.CreateAppOfflineFileAsync(nonExistentDir));
    }

    #endregion

    #region ValidateAppOfflineFileAsync Tests

    [Fact]
    public async Task ValidateAppOfflineFileAsync_WithValidFile_ReturnsTrue() {
        // Arrange
        var manager = new AppOfflineManager();
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        try {
            var filePath = await manager.CreateAppOfflineFileAsync(tempDir);

            // Act
            var isValid = await manager.ValidateAppOfflineFileAsync(filePath);

            // Assert
            Assert.True(isValid);
        } finally {
            // Cleanup
            if(Directory.Exists(tempDir)) {
                Directory.Delete(tempDir, true);
            }
        }
    }

    [Fact]
    public async Task ValidateAppOfflineFileAsync_WithNonExistentFile_ReturnsFalse() {
        // Arrange
        var manager = new AppOfflineManager();
        var nonExistentFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString(), "app_offline.htm");

        // Act
        var isValid = await manager.ValidateAppOfflineFileAsync(nonExistentFile);

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public async Task ValidateAppOfflineFileAsync_WithEmptyFile_ReturnsFalse() {
        // Arrange
        var manager = new AppOfflineManager();
        var tempFile = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.htm");
        await File.WriteAllTextAsync(tempFile, string.Empty);

        try {
            // Act
            var isValid = await manager.ValidateAppOfflineFileAsync(tempFile);

            // Assert
            Assert.False(isValid);
        } finally {
            // Cleanup
            if(File.Exists(tempFile)) {
                File.Delete(tempFile);
            }
        }
    }

    [Fact]
    public async Task ValidateAppOfflineFileAsync_WithNonHtmlContent_ReturnsFalse() {
        // Arrange
        var manager = new AppOfflineManager();
        var tempFile = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.htm");
        await File.WriteAllTextAsync(tempFile, "This is not HTML content");

        try {
            // Act
            var isValid = await manager.ValidateAppOfflineFileAsync(tempFile);

            // Assert
            Assert.False(isValid);
        } finally {
            // Cleanup
            if(File.Exists(tempFile)) {
                File.Delete(tempFile);
            }
        }
    }

    [Fact]
    public async Task ValidateAppOfflineFileAsync_WithNullPath_ReturnsFalse() {
        // Arrange
        var manager = new AppOfflineManager();

        // Act
        var isValid = await manager.ValidateAppOfflineFileAsync(null!);

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public async Task ValidateAppOfflineFileAsync_WithEmptyPath_ReturnsFalse() {
        // Arrange
        var manager = new AppOfflineManager();

        // Act
        var isValid = await manager.ValidateAppOfflineFileAsync(string.Empty);

        // Assert
        Assert.False(isValid);
    }

    #endregion

    #region FileName Property Tests

    [Fact]
    public void FileName_ReturnsCorrectValue() {
        // Act
        var fileName = AppOfflineManager.FileName;

        // Assert
        Assert.Equal("app_offline.htm", fileName);
    }

    #endregion

    #region DefaultContent Property Tests

    [Fact]
    public void DefaultContent_ReturnsValidHtml() {
        // Act
        var content = AppOfflineManager.DefaultContent;

        // Assert
        Assert.NotNull(content);
        Assert.Contains("<!DOCTYPE html>", content);
        Assert.Contains("<html>", content);
        Assert.Contains("</html>", content);
        Assert.Contains("Application Offline", content);
    }

    #endregion
}
