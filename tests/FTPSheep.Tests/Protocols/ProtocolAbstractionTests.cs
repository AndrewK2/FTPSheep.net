using FluentFTP;
using FTPSheep.Protocols.Models;
using FTPSheep.Protocols.Services;

namespace FTPSheep.Tests.Protocols;

#region RemoteFileInfo Tests

public class RemoteFileInfoTests {
    [Fact]
    public void RemoteFileInfo_DefaultConstructor_InitializesProperties() {
        // Act
        var fileInfo = new RemoteFileInfo();

        // Assert
        Assert.Equal(string.Empty, fileInfo.FullPath);
        Assert.Equal(string.Empty, fileInfo.Name);
        Assert.False(fileInfo.IsDirectory);
        Assert.Equal(0, fileInfo.Size);
        Assert.Null(fileInfo.LastModified);
        Assert.Null(fileInfo.Permissions);
    }

    [Fact]
    public void RemoteFileInfo_Properties_CanBeSet() {
        // Arrange
        var now = DateTime.UtcNow;

        // Act
        var fileInfo = new RemoteFileInfo {
            FullPath = "/remote/path/file.txt",
            Name = "file.txt",
            IsDirectory = false,
            Size = 1024,
            LastModified = now,
            Permissions = 644
        };

        // Assert
        Assert.Equal("/remote/path/file.txt", fileInfo.FullPath);
        Assert.Equal("file.txt", fileInfo.Name);
        Assert.False(fileInfo.IsDirectory);
        Assert.Equal(1024, fileInfo.Size);
        Assert.Equal(now, fileInfo.LastModified);
        Assert.Equal(644, fileInfo.Permissions);
    }

    [Fact]
    public void RemoteFileInfo_FormattedSize_ForDirectory_ReturnsDir() {
        // Arrange
        var fileInfo = new RemoteFileInfo {
            IsDirectory = true,
            Size = 0
        };

        // Act
        var formattedSize = fileInfo.FormattedSize;

        // Assert
        Assert.Equal("<DIR>", formattedSize);
    }

    [Fact]
    public void RemoteFileInfo_FormattedSize_ForSmallFile_ReturnsBytes() {
        // Arrange
        var fileInfo = new RemoteFileInfo {
            IsDirectory = false,
            Size = 512
        };

        // Act
        var formattedSize = fileInfo.FormattedSize;

        // Assert
        Assert.Equal("512 B", formattedSize);
    }

    [Fact]
    public void RemoteFileInfo_FormattedSize_ForKilobytes_ReturnsKB() {
        // Arrange
        var fileInfo = new RemoteFileInfo {
            IsDirectory = false,
            Size = 1536 // 1.5 KB
        };

        // Act
        var formattedSize = fileInfo.FormattedSize;

        // Assert
        Assert.Equal("1.50 KB", formattedSize);
    }

    [Fact]
    public void RemoteFileInfo_FormattedSize_ForMegabytes_ReturnsMB() {
        // Arrange
        var fileInfo = new RemoteFileInfo {
            IsDirectory = false,
            Size = 1572864 // 1.5 MB
        };

        // Act
        var formattedSize = fileInfo.FormattedSize;

        // Assert
        Assert.Equal("1.50 MB", formattedSize);
    }

    [Fact]
    public void RemoteFileInfo_FormattedSize_ForGigabytes_ReturnsGB() {
        // Arrange
        var fileInfo = new RemoteFileInfo {
            IsDirectory = false,
            Size = 1610612736 // 1.5 GB
        };

        // Act
        var formattedSize = fileInfo.FormattedSize;

        // Assert
        Assert.Equal("1.50 GB", formattedSize);
    }

    [Fact]
    public void RemoteFileInfo_FileType_ForFile_ReturnsFile() {
        // Arrange
        var fileInfo = new RemoteFileInfo {
            IsDirectory = false
        };

        // Act
        var fileType = fileInfo.FileType;

        // Assert
        Assert.Equal("File", fileType);
    }

    [Fact]
    public void RemoteFileInfo_FileType_ForDirectory_ReturnsDirectory() {
        // Arrange
        var fileInfo = new RemoteFileInfo {
            IsDirectory = true
        };

        // Act
        var fileType = fileInfo.FileType;

        // Assert
        Assert.Equal("Directory", fileType);
    }

    [Fact]
    public void RemoteFileInfo_WithNullPermissions_ReturnsNull() {
        // Arrange
        var fileInfo = new RemoteFileInfo {
            Permissions = null
        };

        // Assert
        Assert.Null(fileInfo.Permissions);
    }
}

#endregion

#region FtpClientFactory Tests

public class FtpClientFactoryTests {
    [Fact]
    public void CreateClient_WithNullConfig_ThrowsArgumentNullException() {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            FtpClientFactory.CreateClient((FtpConnectionConfig)null!));
    }

    [Fact]
    public void CreateClient_WithValidConfig_ReturnsIFtpClient() {
        // Arrange
        var config = new FtpConnectionConfig {
            Host = "ftp.example.com",
            Port = 21,
            Username = "user",
            Password = "pass"
        };

        // Act
        using var client = FtpClientFactory.CreateClient(config);

        // Assert
        Assert.NotNull(client);
        Assert.False(client.IsConnected);
    }

    [Fact]
    public void CreateClient_WithParameters_ReturnsIFtpClient() {
        // Act
        using var client = FtpClientFactory.CreateClient(
            "ftp.example.com",
            21,
            "user",
            "pass",
            FtpEncryptionMode.None);

        // Assert
        Assert.NotNull(client);
        Assert.False(client.IsConnected);
    }

    [Fact]
    public void CreateClient_WithFtpEncryption_ReturnsIFtpClient() {
        // Arrange
        var config = new FtpConnectionConfig {
            Host = "ftp.example.com",
            Port = 21,
            Username = "user",
            Password = "pass",
            EncryptionMode = FtpEncryptionMode.None
        };

        // Act
        using var client = FtpClientFactory.CreateClient(config);

        // Assert
        Assert.NotNull(client);
    }

    [Fact]
    public void CreateClient_WithFtpsExplicit_ReturnsIFtpClient() {
        // Arrange
        var config = new FtpConnectionConfig {
            Host = "ftp.example.com",
            Port = 21,
            Username = "user",
            Password = "pass",
            EncryptionMode = FtpEncryptionMode.Explicit
        };

        // Act
        using var client = FtpClientFactory.CreateClient(config);

        // Assert
        Assert.NotNull(client);
    }

    [Fact]
    public void CreateClient_WithFtpsImplicit_ReturnsIFtpClient() {
        // Arrange
        var config = new FtpConnectionConfig {
            Host = "ftp.example.com",
            Port = 990,
            Username = "user",
            Password = "pass",
            EncryptionMode = FtpEncryptionMode.Implicit
        };

        // Act
        using var client = FtpClientFactory.CreateClient(config);

        // Assert
        Assert.NotNull(client);
    }

    [Fact]
    public void CreateClient_WithRemoteRootPath_ReturnsIFtpClient() {
        // Act
        using var client = FtpClientFactory.CreateClient(
            "ftp.example.com",
            21,
            "user",
            "pass",
            FtpEncryptionMode.None,
            "/remote/path");

        // Assert
        Assert.NotNull(client);
    }

    [Fact]
    public void IsEncryptionModeSupported_WithNone_ReturnsTrue() {
        // Act
        var isSupported = FtpClientFactory.IsEncryptionModeSupported(FtpEncryptionMode.None);

        // Assert
        Assert.True(isSupported);
    }

    [Fact]
    public void IsEncryptionModeSupported_WithExplicit_ReturnsTrue() {
        // Act
        var isSupported = FtpClientFactory.IsEncryptionModeSupported(FtpEncryptionMode.Explicit);

        // Assert
        Assert.True(isSupported);
    }

    [Fact]
    public void IsEncryptionModeSupported_WithImplicit_ReturnsTrue() {
        // Act
        var isSupported = FtpClientFactory.IsEncryptionModeSupported(FtpEncryptionMode.Implicit);

        // Assert
        Assert.True(isSupported);
    }

    [Fact]
    public void GetSupportedEncryptionModes_ReturnsArray() {
        // Act
        var modes = FtpClientFactory.GetSupportedEncryptionModes();

        // Assert
        Assert.NotNull(modes);
        Assert.Equal(3, modes.Length);
        Assert.Contains("None (FTP)", modes);
        Assert.Contains("Explicit (FTPS)", modes);
        Assert.Contains("Implicit (FTPS)", modes);
    }
}

#endregion
