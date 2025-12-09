using FTPSheep.Protocols.Exceptions;
using FTPSheep.Protocols.Models;
using FTPSheep.Protocols.Services;

namespace FTPSheep.Tests.Protocols;

public class FtpClientServiceTests {
    [Fact]
    public void Constructor_WithNullConfig_ThrowsArgumentNullException() {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new FtpClientService(null!));
    }

    [Fact]
    public void Constructor_WithEmptyHost_ThrowsArgumentException() {
        // Arrange
        var config = new FtpConnectionConfig {
            Host = "",
            Username = "user",
            Password = "pass"
        };

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => new FtpClientService(config));
        Assert.Contains("Host is required", ex.Message);
    }

    [Fact]
    public void Constructor_WithInvalidPort_ThrowsArgumentException() {
        // Arrange
        var config = new FtpConnectionConfig {
            Host = "ftp.example.com",
            Port = 0,
            Username = "user",
            Password = "pass"
        };

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => new FtpClientService(config));
        Assert.Contains("Port must be between 1 and 65535", ex.Message);
    }

    [Fact]
    public void Constructor_WithPortTooHigh_ThrowsArgumentException() {
        // Arrange
        var config = new FtpConnectionConfig {
            Host = "ftp.example.com",
            Port = 70000,
            Username = "user",
            Password = "pass"
        };

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => new FtpClientService(config));
        Assert.Contains("Port must be between 1 and 65535", ex.Message);
    }

    [Fact]
    public void Constructor_WithEmptyUsername_ThrowsArgumentException() {
        // Arrange
        var config = new FtpConnectionConfig {
            Host = "ftp.example.com",
            Username = "",
            Password = "pass"
        };

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => new FtpClientService(config));
        Assert.Contains("Username is required", ex.Message);
    }

    [Fact]
    public void Constructor_WithValidConfig_CreatesInstance() {
        // Arrange
        var config = new FtpConnectionConfig {
            Host = "ftp.example.com",
            Port = 21,
            Username = "user",
            Password = "pass"
        };

        // Act
        using var service = new FtpClientService(config);

        // Assert
        Assert.NotNull(service);
        Assert.False(service.IsConnected);
    }

    [Fact]
    public void IsConnected_BeforeConnect_ReturnsFalse() {
        // Arrange
        var config = new FtpConnectionConfig {
            Host = "ftp.example.com",
            Username = "user",
            Password = "pass"
        };

        // Act
        using var service = new FtpClientService(config);

        // Assert
        Assert.False(service.IsConnected);
    }

    [Fact]
    public async Task UploadFileAsync_WhenNotConnected_ThrowsInvalidOperationException() {
        // Arrange
        var config = new FtpConnectionConfig {
            Host = "ftp.example.com",
            Username = "user",
            Password = "pass"
        };

        using var service = new FtpClientService(config);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.UploadFileAsync("local.txt", "remote.txt"));
        Assert.Contains("not connected", ex.Message);
    }

    [Fact]
    public async Task CreateDirectoryAsync_WhenNotConnected_ThrowsInvalidOperationException() {
        // Arrange
        var config = new FtpConnectionConfig {
            Host = "ftp.example.com",
            Username = "user",
            Password = "pass"
        };

        using var service = new FtpClientService(config);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateDirectoryAsync("/remote/dir"));
        Assert.Contains("not connected", ex.Message);
    }

    [Fact]
    public async Task DirectoryExistsAsync_WhenNotConnected_ThrowsInvalidOperationException() {
        // Arrange
        var config = new FtpConnectionConfig {
            Host = "ftp.example.com",
            Username = "user",
            Password = "pass"
        };

        using var service = new FtpClientService(config);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.DirectoryExistsAsync("/remote/dir"));
        Assert.Contains("not connected", ex.Message);
    }

    [Fact]
    public async Task ListDirectoryAsync_WhenNotConnected_ThrowsInvalidOperationException() {
        // Arrange
        var config = new FtpConnectionConfig {
            Host = "ftp.example.com",
            Username = "user",
            Password = "pass"
        };

        using var service = new FtpClientService(config);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.ListDirectoryAsync("/remote/dir"));
        Assert.Contains("not connected", ex.Message);
    }

    [Fact]
    public async Task DeleteFileAsync_WhenNotConnected_ThrowsInvalidOperationException() {
        // Arrange
        var config = new FtpConnectionConfig {
            Host = "ftp.example.com",
            Username = "user",
            Password = "pass"
        };

        using var service = new FtpClientService(config);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.DeleteFileAsync("/remote/file.txt"));
        Assert.Contains("not connected", ex.Message);
    }

    [Fact]
    public async Task DeleteDirectoryAsync_WhenNotConnected_ThrowsInvalidOperationException() {
        // Arrange
        var config = new FtpConnectionConfig {
            Host = "ftp.example.com",
            Username = "user",
            Password = "pass"
        };

        using var service = new FtpClientService(config);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.DeleteDirectoryAsync("/remote/dir"));
        Assert.Contains("not connected", ex.Message);
    }

    [Fact]
    public async Task FileExistsAsync_WhenNotConnected_ThrowsInvalidOperationException() {
        // Arrange
        var config = new FtpConnectionConfig {
            Host = "ftp.example.com",
            Username = "user",
            Password = "pass"
        };

        using var service = new FtpClientService(config);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.FileExistsAsync("/remote/file.txt"));
        Assert.Contains("not connected", ex.Message);
    }

    [Fact]
    public async Task GetFileSizeAsync_WhenNotConnected_ThrowsInvalidOperationException() {
        // Arrange
        var config = new FtpConnectionConfig {
            Host = "ftp.example.com",
            Username = "user",
            Password = "pass"
        };

        using var service = new FtpClientService(config);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.GetFileSizeAsync("/remote/file.txt"));
        Assert.Contains("not connected", ex.Message);
    }

    [Fact]
    public async Task SetFilePermissionsAsync_WhenNotConnected_ThrowsInvalidOperationException() {
        // Arrange
        var config = new FtpConnectionConfig {
            Host = "ftp.example.com",
            Username = "user",
            Password = "pass"
        };

        using var service = new FtpClientService(config);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.SetFilePermissionsAsync("/remote/file.txt", 644));
        Assert.Contains("not connected", ex.Message);
    }

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes() {
        // Arrange
        var config = new FtpConnectionConfig {
            Host = "ftp.example.com",
            Username = "user",
            Password = "pass"
        };

        var service = new FtpClientService(config);

        // Act & Assert - should not throw
        service.Dispose();
        service.Dispose();
        service.Dispose();
    }

    [Fact]
    public void FtpConnectionConfig_DefaultValues_AreCorrect() {
        // Arrange & Act
        var config = new FtpConnectionConfig();

        // Assert
        Assert.Equal(string.Empty, config.Host);
        Assert.Equal(21, config.Port);
        Assert.Equal(string.Empty, config.Username);
        Assert.Equal(string.Empty, config.Password);
        Assert.Equal(30, config.ConnectionTimeout);
        Assert.Equal(120, config.DataConnectionTimeout);
        Assert.Equal(3, config.RetryAttempts);
        Assert.True(config.ValidateCertificate);
        Assert.True(config.KeepAlive);
        Assert.Equal("UTF-8", config.Encoding);
    }

    [Fact]
    public void FtpException_CanBeCreated_WithMessage() {
        // Arrange
        var message = "Test error message";

        // Act
        var exception = new FtpException(message);

        // Assert
        Assert.Equal(message, exception.Message);
        Assert.Null(exception.Host);
        Assert.Null(exception.Port);
        Assert.False(exception.IsTransient);
    }

    [Fact]
    public void FtpException_CanBeCreated_WithMessageAndInnerException() {
        // Arrange
        var message = "Test error message";
        var innerException = new InvalidOperationException("Inner error");

        // Act
        var exception = new FtpException(message, innerException);

        // Assert
        Assert.Equal(message, exception.Message);
        Assert.Same(innerException, exception.InnerException);
    }

    [Fact]
    public void FtpException_Properties_CanBeSet() {
        // Arrange
        var exception = new FtpException("Test error");

        // Act
        exception.Host = "ftp.example.com";
        exception.Port = 21;
        exception.IsTransient = true;

        // Assert
        Assert.Equal("ftp.example.com", exception.Host);
        Assert.Equal(21, exception.Port);
        Assert.True(exception.IsTransient);
    }
}
