using FTPSheep.Core.Models;

namespace FTPSheep.Tests.Services;

public class PublishProfileTests {
    #region ParsePublishUrl Tests

    [Fact]
    public void ParsePublishUrl_WithFullUrl_ParsesCorrectly() {
        // Arrange
        var profile = new PublishProfile {
            PublishUrl = "ftp://ftp.example.com/site/wwwroot"
        };

        // Act
        var (host, port, remotePath) = profile.ParsePublishUrl();

        // Assert
        Assert.Equal("ftp.example.com", host);
        Assert.Equal(21, port);
        Assert.Equal("/site/wwwroot", remotePath);
    }

    [Fact]
    public void ParsePublishUrl_WithFtpsUrl_ParsesCorrectly() {
        // Arrange
        var profile = new PublishProfile {
            PublishUrl = "ftps://ftp.example.com:990/path"
        };

        // Act
        var (host, port, remotePath) = profile.ParsePublishUrl();

        // Assert
        Assert.Equal("ftp.example.com", host);
        Assert.Equal(990, port);
        Assert.Equal("/path", remotePath);
    }

    [Fact]
    public void ParsePublishUrl_WithoutProtocol_AddsDefaultProtocol() {
        // Arrange
        var profile = new PublishProfile {
            PublishUrl = "ftp.example.com/site"
        };

        // Act
        var (host, port, remotePath) = profile.ParsePublishUrl();

        // Assert
        Assert.Equal("ftp.example.com", host);
        Assert.Equal(21, port);
        Assert.Equal("/site", remotePath);
    }

    [Fact]
    public void ParsePublishUrl_WithCustomPort_ParsesPort() {
        // Arrange
        var profile = new PublishProfile {
            PublishUrl = "ftp://ftp.example.com:2121/path"
        };

        // Act
        var (host, port, remotePath) = profile.ParsePublishUrl();

        // Assert
        Assert.Equal("ftp.example.com", host);
        Assert.Equal(2121, port);
        Assert.Equal("/path", remotePath);
    }

    [Fact]
    public void ParsePublishUrl_WithRootPath_ReturnsEmptyPath() {
        // Arrange
        var profile = new PublishProfile {
            PublishUrl = "ftp://ftp.example.com/"
        };

        // Act
        var (host, port, remotePath) = profile.ParsePublishUrl();

        // Assert
        Assert.Equal("ftp.example.com", host);
        Assert.Equal(21, port);
        Assert.Equal(string.Empty, remotePath);
    }

    [Fact]
    public void ParsePublishUrl_WithEmptyUrl_ReturnsDefaults() {
        // Arrange
        var profile = new PublishProfile {
            PublishUrl = string.Empty
        };

        // Act
        var (host, port, remotePath) = profile.ParsePublishUrl();

        // Assert
        Assert.Equal(string.Empty, host);
        Assert.Equal(21, port);
        Assert.Equal(string.Empty, remotePath);
    }

    [Fact]
    public void ParsePublishUrl_WithNullUrl_ReturnsDefaults() {
        // Arrange
        var profile = new PublishProfile {
            PublishUrl = null!
        };

        // Act
        var (host, port, remotePath) = profile.ParsePublishUrl();

        // Assert
        Assert.Equal(string.Empty, host);
        Assert.Equal(21, port);
        Assert.Equal(string.Empty, remotePath);
    }

    [Fact]
    public void ParsePublishUrl_WithHostAndPort_ParsesCorrectly() {
        // Arrange
        var profile = new PublishProfile {
            PublishUrl = "ftp.example.com:2121/site"
        };

        // Act
        var (host, port, remotePath) = profile.ParsePublishUrl();

        // Assert
        Assert.Equal("ftp.example.com", host);
        Assert.Equal(2121, port);
        Assert.Equal("/site", remotePath);
    }

    #endregion

    #region Property Tests

    [Fact]
    public void IsFtpProfile_WithFtpPublishMethod_ReturnsTrue() {
        // Arrange
        var profile = new PublishProfile {
            PublishMethod = "FTP"
        };

        // Act & Assert
        Assert.True(profile.IsFtpProfile);
    }

    [Fact]
    public void IsFtpProfile_WithFtpLowerCase_ReturnsTrue() {
        // Arrange
        var profile = new PublishProfile {
            PublishMethod = "ftp"
        };

        // Act & Assert
        Assert.True(profile.IsFtpProfile);
    }

    [Fact]
    public void IsFtpProfile_WithMsDeployMethod_ReturnsFalse() {
        // Arrange
        var profile = new PublishProfile {
            PublishMethod = "MSDeploy"
        };

        // Act & Assert
        Assert.False(profile.IsFtpProfile);
    }

    [Fact]
    public void IsFtpProfile_WithEmptyMethod_ReturnsFalse() {
        // Arrange
        var profile = new PublishProfile {
            PublishMethod = string.Empty
        };

        // Act & Assert
        Assert.False(profile.IsFtpProfile);
    }

    [Fact]
    public void PublishProfile_DefaultValues_AreSetCorrectly() {
        // Arrange & Act
        var profile = new PublishProfile();

        // Assert
        Assert.Equal(string.Empty, profile.PublishMethod);
        Assert.Equal(string.Empty, profile.PublishUrl);
        Assert.Null(profile.UserName);
        Assert.False(profile.SavePwd);
        Assert.False(profile.DeleteExistingFiles);
        Assert.Null(profile.TargetFramework);
        Assert.Null(profile.SelfContained);
        Assert.Null(profile.RuntimeIdentifier);
        Assert.Null(profile.PublishProtocol);
        Assert.False(profile.ExcludeAppData);
        Assert.NotNull(profile.AdditionalProperties);
        Assert.Empty(profile.AdditionalProperties);
        Assert.Null(profile.SourceFilePath);
    }

    #endregion
}
