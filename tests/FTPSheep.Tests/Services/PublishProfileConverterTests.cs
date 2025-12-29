using FTPSheep.Core.Exceptions;
using FTPSheep.Core.Models;
using FTPSheep.Core.Services;

namespace FTPSheep.Tests.Services;

public class PublishProfileConverterTests {
    private readonly PublishProfileConverter converter = new();

    #region Convert Tests

    [Fact]
    public void Convert_WithNullProfile_ThrowsArgumentNullException() {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => converter.Convert(null!));
    }

    [Fact]
    public void Convert_WithNonFtpProfile_ThrowsProfileException() {
        // Arrange
        var publishProfile = new PublishProfile {
            PublishMethod = "MSDeploy",
            PublishUrl = "http://example.com",
            SourceFilePath = "test.pubxml"
        };

        // Act & Assert
        var ex = Assert.Throws<ProfileException>(() => converter.Convert(publishProfile));
        Assert.Contains("test.pubxml", ex.Message);
        Assert.Contains("MSDeploy", ex.Message);
    }

    [Fact]
    public void Convert_WithMissingPublishUrl_ThrowsProfileException() {
        // Arrange
        var publishProfile = new PublishProfile {
            PublishMethod = "FTP",
            PublishUrl = string.Empty,
            SourceFilePath = "test.pubxml"
        };

        // Act & Assert
        var ex = Assert.Throws<ProfileException>(() => converter.Convert(publishProfile));
        Assert.Contains("PublishUrl is missing", ex.Message);
    }

    [Fact]
    public void Convert_WithValidFtpProfile_CreatesDeploymentProfile() {
        // Arrange
        var publishProfile = new PublishProfile {
            PublishMethod = "FTP",
            PublishUrl = "ftp://ftp.example.com:21/site/wwwroot",
            UserName = "testuser",
            DeleteExistingFiles = true,
            TargetFramework = "net8.0",
            RuntimeIdentifier = "win-x64",
            SelfContained = true,
            SourceFilePath = "FTPProfile.pubxml"
        };

        // Act
        var deploymentProfile = converter.Convert(publishProfile);

        // Assert
        Assert.Equal("FTPProfile", deploymentProfile.Name);
        Assert.Equal("ftp.example.com", deploymentProfile.Connection.Host);
        Assert.Equal(21, deploymentProfile.Connection.Port);
        Assert.Equal(ProtocolType.Ftp, deploymentProfile.Connection.Protocol);
        Assert.Equal("testuser", deploymentProfile.Username);
        Assert.Equal("/site/wwwroot", deploymentProfile.RemotePath);
        Assert.Equal("Release", deploymentProfile.Build.Configuration);
        Assert.Equal("net8.0", deploymentProfile.Build.TargetFramework);
        Assert.Equal("win-x64", deploymentProfile.Build.RuntimeIdentifier);
        Assert.True(deploymentProfile.Build.SelfContained);
        Assert.Equal(CleanupMode.DeleteObsolete, deploymentProfile.CleanupMode);
        Assert.Equal(4, deploymentProfile.Concurrency);
        Assert.Equal(3, deploymentProfile.RetryCount);
    }

    [Fact]
    public void Convert_WithFtpsProtocol_SetsUseSslTrue() {
        // Arrange
        var publishProfile = new PublishProfile {
            PublishMethod = "FTP",
            PublishUrl = "ftps://ftp.example.com/site",
            PublishProtocol = "ftps",
            UserName = "testuser"
        };

        // Act
        var deploymentProfile = converter.Convert(publishProfile);

        // Assert
        Assert.True(deploymentProfile.Connection.UseSsl);
    }

    [Fact]
    public void Convert_WithFtpProtocol_SetsUseSslFalse() {
        // Arrange
        var publishProfile = new PublishProfile {
            PublishMethod = "FTP",
            PublishUrl = "ftp://ftp.example.com/site",
            PublishProtocol = "ftp",
            UserName = "testuser"
        };

        // Act
        var deploymentProfile = converter.Convert(publishProfile);

        // Assert
        Assert.False(deploymentProfile.Connection.UseSsl);
    }

    [Fact]
    public void Convert_WithFtpsUrlScheme_SetsUseSslTrue() {
        // Arrange
        var publishProfile = new PublishProfile {
            PublishMethod = "FTP",
            PublishUrl = "ftps://ftp.example.com/site",
            UserName = "testuser"
        };

        // Act
        var deploymentProfile = converter.Convert(publishProfile);

        // Assert
        Assert.True(deploymentProfile.Connection.UseSsl);
    }

    [Fact]
    public void Convert_WithCustomProfileName_UsesProvidedName() {
        // Arrange
        var publishProfile = new PublishProfile {
            PublishMethod = "FTP",
            PublishUrl = "ftp://ftp.example.com/site",
            UserName = "testuser",
            SourceFilePath = "Original.pubxml"
        };

        // Act
        var deploymentProfile = converter.Convert(publishProfile, "CustomName");

        // Assert
        Assert.Equal("CustomName", deploymentProfile.Name);
    }

    [Fact]
    public void Convert_WithNoSourceFilePath_GeneratesNameFromHost() {
        // Arrange
        var publishProfile = new PublishProfile {
            PublishMethod = "FTP",
            PublishUrl = "ftp://ftp.example.com/site",
            UserName = "testuser",
            SourceFilePath = null
        };

        // Act
        var deploymentProfile = converter.Convert(publishProfile);

        // Assert
        Assert.Equal("ftp.example.com-import", deploymentProfile.Name);
    }

    [Fact]
    public void Convert_WithDeleteExistingFilesFalse_SetsCleanupModeNone() {
        // Arrange
        var publishProfile = new PublishProfile {
            PublishMethod = "FTP",
            PublishUrl = "ftp://ftp.example.com/site",
            UserName = "testuser",
            DeleteExistingFiles = false
        };

        // Act
        var deploymentProfile = converter.Convert(publishProfile);

        // Assert
        Assert.Equal(CleanupMode.None, deploymentProfile.CleanupMode);
    }

    [Fact]
    public void Convert_WithExcludeAppData_SetsAppOfflineEnabledTrue() {
        // Arrange
        var publishProfile = new PublishProfile {
            PublishMethod = "FTP",
            PublishUrl = "ftp://ftp.example.com/site",
            UserName = "testuser",
            ExcludeAppData = true
        };

        // Act
        var deploymentProfile = converter.Convert(publishProfile);

        // Assert
        Assert.False(deploymentProfile.AppOfflineEnabled);
    }

    [Fact]
    public void Convert_WithoutExcludeAppData_SetsAppOfflineEnabledFalse() {
        // Arrange
        var publishProfile = new PublishProfile {
            PublishMethod = "FTP",
            PublishUrl = "ftp://ftp.example.com/site",
            UserName = "testuser",
            ExcludeAppData = false
        };

        // Act
        var deploymentProfile = converter.Convert(publishProfile);

        // Assert
        Assert.True(deploymentProfile.AppOfflineEnabled);
    }

    [Fact]
    public void Convert_WithUrlWithoutPath_SetsEmptyRemotePath() {
        // Arrange
        var publishProfile = new PublishProfile {
            PublishMethod = "FTP",
            PublishUrl = "ftp://ftp.example.com",
            UserName = "testuser"
        };

        // Act
        var deploymentProfile = converter.Convert(publishProfile);

        // Assert
        Assert.Equal(string.Empty, deploymentProfile.RemotePath);
    }

    [Fact]
    public void Convert_WithCustomPort_UsesCustomPort() {
        // Arrange
        var publishProfile = new PublishProfile {
            PublishMethod = "FTP",
            PublishUrl = "ftp://ftp.example.com:2121/site",
            UserName = "testuser"
        };

        // Act
        var deploymentProfile = converter.Convert(publishProfile);

        // Assert
        Assert.Equal(2121, deploymentProfile.Connection.Port);
    }

    [Fact]
    public void Convert_WithFtpSitePath_UsesFtpSitePathAsRemotePath() {
        // Arrange
        var publishProfile = new PublishProfile {
            PublishMethod = "FTP",
            PublishUrl = "ftp://ftp.example.com/www",
            UserName = "testuser"
        };
        publishProfile.AdditionalProperties["FtpSitePath"] = "/site/wwwroot";

        // Act
        var deploymentProfile = converter.Convert(publishProfile);

        // Assert
        Assert.Equal("/site/wwwroot", deploymentProfile.RemotePath);
    }

    [Fact]
    public void Convert_WithFtpSitePathAndDifferentPublishUrlPath_PrioritizesFtpSitePath() {
        // Arrange - PublishUrl has /www but FtpSitePath has /site/wwwroot
        var publishProfile = new PublishProfile {
            PublishMethod = "FTP",
            PublishUrl = "ftp://ftp.example.com/www",
            UserName = "testuser"
        };
        publishProfile.AdditionalProperties["FtpSitePath"] = "/site/wwwroot";

        // Act
        var deploymentProfile = converter.Convert(publishProfile);

        // Assert - Should use FtpSitePath, not the path from PublishUrl
        Assert.Equal("/site/wwwroot", deploymentProfile.RemotePath);
    }

    [Fact]
    public void Convert_WithoutFtpSitePath_UsesPublishUrlPath() {
        // Arrange
        var publishProfile = new PublishProfile {
            PublishMethod = "FTP",
            PublishUrl = "ftp://ftp.example.com/www",
            UserName = "testuser"
        };
        // No FtpSitePath in AdditionalProperties

        // Act
        var deploymentProfile = converter.Convert(publishProfile);

        // Assert - Should use path from PublishUrl
        Assert.Equal("/www", deploymentProfile.RemotePath);
    }

    #endregion

    #region ValidateImportedProfile Tests

    [Fact]
    public void ValidateImportedProfile_WithValidProfile_ReturnsEmptyErrors() {
        // Arrange
        var profile = new DeploymentProfile {
            Name = "test-profile",
            Connection = new ServerConnection {
                Host = "ftp.example.com",
                Port = 21
            },
            Username = "testuser"
        };

        // Act
        var errors = converter.ValidateImportedProfile(profile);

        // Assert
        Assert.Empty(errors);
    }

    [Fact]
    public void ValidateImportedProfile_WithMissingName_ReturnsError() {
        // Arrange
        var profile = new DeploymentProfile {
            Name = string.Empty,
            Connection = new ServerConnection {
                Host = "ftp.example.com",
                Port = 21
            },
            Username = "testuser"
        };

        // Act
        var errors = converter.ValidateImportedProfile(profile);

        // Assert
        Assert.Single(errors);
        Assert.Contains("name is required", errors[0], StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ValidateImportedProfile_WithMissingHost_ReturnsError() {
        // Arrange
        var profile = new DeploymentProfile {
            Name = "test-profile",
            Connection = new ServerConnection {
                Host = string.Empty,
                Port = 21
            },
            Username = "testuser"
        };

        // Act
        var errors = converter.ValidateImportedProfile(profile);

        // Assert
        Assert.Single(errors);
        Assert.Contains("host is required", errors[0], StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ValidateImportedProfile_WithInvalidPort_ReturnsError() {
        // Arrange
        var profile = new DeploymentProfile {
            Name = "test-profile",
            Connection = new ServerConnection {
                Host = "ftp.example.com",
                Port = 70000
            },
            Username = "testuser"
        };

        // Act
        var errors = converter.ValidateImportedProfile(profile);

        // Assert
        Assert.Single(errors);
        Assert.Contains("Invalid port", errors[0]);
    }

    [Fact]
    public void ValidateImportedProfile_WithMissingUsername_ReturnsError() {
        // Arrange
        var profile = new DeploymentProfile {
            Name = "test-profile",
            Connection = new ServerConnection {
                Host = "ftp.example.com",
                Port = 21
            },
            Username = string.Empty
        };

        // Act
        var errors = converter.ValidateImportedProfile(profile);

        // Assert
        Assert.Single(errors);
        Assert.Contains("Username is required", errors[0]);
    }

    [Fact]
    public void ValidateImportedProfile_WithMultipleErrors_ReturnsAllErrors() {
        // Arrange
        var profile = new DeploymentProfile {
            Name = string.Empty,
            Connection = new ServerConnection {
                Host = string.Empty,
                Port = -1
            },
            Username = null
        };

        // Act
        var errors = converter.ValidateImportedProfile(profile);

        // Assert
        Assert.Equal(4, errors.Count);
    }

    #endregion
}
