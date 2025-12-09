using FTPSheep.Core.Exceptions;
using FTPSheep.Core.Interfaces;
using FTPSheep.Core.Models;
using FTPSheep.Core.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace FTPSheep.Tests.Services;

public class ProfileServiceTests {
    private readonly Mock<IProfileRepository> repositoryMock;
    private readonly Mock<IConfigurationService> configServiceMock;
    private readonly Mock<ICredentialStore> credentialStoreMock;
    private readonly Mock<ILogger<ProfileService>> loggerMock;
    private readonly ProfileService profileService;

    public ProfileServiceTests() {
        repositoryMock = new Mock<IProfileRepository>();
        configServiceMock = new Mock<IConfigurationService>();
        credentialStoreMock = new Mock<ICredentialStore>();
        loggerMock = new Mock<ILogger<ProfileService>>();

        profileService = new ProfileService(
            repositoryMock.Object,
            configServiceMock.Object,
            credentialStoreMock.Object,
            loggerMock.Object);

        // Setup default global configuration
        configServiceMock
            .Setup(x => x.LoadConfigurationAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(GlobalConfiguration.CreateDefault());
    }

    [Fact]
    public async Task CreateProfileAsync_ValidProfile_Succeeds() {
        // Arrange
        var profile = CreateValidProfile("test-profile");

        repositoryMock
            .Setup(x => x.ExistsAsync("test-profile", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        repositoryMock
            .Setup(x => x.SaveAsync(It.IsAny<DeploymentProfile>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        credentialStoreMock
            .Setup(x => x.SaveCredentialsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await profileService.CreateProfileAsync(profile);

        // Assert
        repositoryMock.Verify(x => x.SaveAsync(profile, It.IsAny<CancellationToken>()), Times.Once);
        credentialStoreMock.Verify(x => x.SaveCredentialsAsync("test-profile", "testuser", "testpass", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateProfileAsync_DuplicateName_ThrowsException() {
        // Arrange
        var profile = CreateValidProfile("duplicate");

        repositoryMock
            .Setup(x => x.ExistsAsync("duplicate", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act & Assert
        await Assert.ThrowsAsync<ProfileAlreadyExistsException>(() => profileService.CreateProfileAsync(profile));

        repositoryMock.Verify(x => x.SaveAsync(It.IsAny<DeploymentProfile>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateProfileAsync_InvalidProfile_ThrowsValidationException() {
        // Arrange
        var profile = new DeploymentProfile {
            Name = "", // Invalid: empty name
            Connection = new ServerConnection("ftp.example.com"),
            RemotePath = "/www"
        };

        // Act & Assert
        await Assert.ThrowsAsync<ProfileValidationException>(() => profileService.CreateProfileAsync(profile));

        repositoryMock.Verify(x => x.SaveAsync(It.IsAny<DeploymentProfile>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task LoadProfileAsync_ByName_LoadsWithDefaults() {
        // Arrange
        var profile = CreateValidProfile("load-test");

        repositoryMock
            .Setup(x => x.LoadAsync("load-test", It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);

        credentialStoreMock
            .Setup(x => x.LoadCredentialsAsync("load-test", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Credentials("saveduser", "savedpass"));

        // Act
        var loadedProfile = await profileService.LoadProfileAsync("load-test");

        // Assert
        Assert.NotNull(loadedProfile);
        Assert.Equal("load-test", loadedProfile.Name);
        Assert.Equal("saveduser", loadedProfile.Username);
        Assert.Equal("savedpass", loadedProfile.Password);

        configServiceMock.Verify(x => x.ApplyDefaultsAsync(It.IsAny<DeploymentProfile>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task LoadProfileAsync_ByPath_LoadsFromFile() {
        // Arrange
        var filePath = @"C:\test\profile.json";
        var profile = CreateValidProfile("path-test");

        repositoryMock
            .Setup(x => x.LoadFromPathAsync(filePath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);

        credentialStoreMock
            .Setup(x => x.LoadCredentialsAsync("path-test", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Credentials?)null);

        // Act
        var loadedProfile = await profileService.LoadProfileAsync(filePath);

        // Assert
        Assert.NotNull(loadedProfile);
        Assert.Equal("path-test", loadedProfile.Name);

        repositoryMock.Verify(x => x.LoadFromPathAsync(filePath, It.IsAny<CancellationToken>()), Times.Once);
        repositoryMock.Verify(x => x.LoadAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task LoadProfileAsync_NonExistent_ThrowsProfileNotFoundException() {
        // Arrange
        repositoryMock
            .Setup(x => x.LoadAsync("non-existent", It.IsAny<CancellationToken>()))
            .ReturnsAsync((DeploymentProfile?)null);

        // Act & Assert
        await Assert.ThrowsAsync<ProfileNotFoundException>(() => profileService.LoadProfileAsync("non-existent"));
    }

    [Fact]
    public async Task UpdateProfileAsync_ValidProfile_Succeeds() {
        // Arrange
        var profile = CreateValidProfile("update-test");

        repositoryMock
            .Setup(x => x.ExistsAsync("update-test", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        repositoryMock
            .Setup(x => x.SaveAsync(It.IsAny<DeploymentProfile>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        credentialStoreMock
            .Setup(x => x.SaveCredentialsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await profileService.UpdateProfileAsync(profile);

        // Assert
        repositoryMock.Verify(x => x.SaveAsync(profile, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateProfileAsync_NonExistent_ThrowsProfileNotFoundException() {
        // Arrange
        var profile = CreateValidProfile("non-existent");

        repositoryMock
            .Setup(x => x.ExistsAsync("non-existent", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act & Assert
        await Assert.ThrowsAsync<ProfileNotFoundException>(() => profileService.UpdateProfileAsync(profile));

        repositoryMock.Verify(x => x.SaveAsync(It.IsAny<DeploymentProfile>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteProfileAsync_ExistingProfile_ReturnsTrue() {
        // Arrange
        repositoryMock
            .Setup(x => x.DeleteAsync("delete-test", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        credentialStoreMock
            .Setup(x => x.DeleteCredentialsAsync("delete-test", It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await profileService.DeleteProfileAsync("delete-test");

        // Assert
        Assert.True(result);
        repositoryMock.Verify(x => x.DeleteAsync("delete-test", It.IsAny<CancellationToken>()), Times.Once);
        credentialStoreMock.Verify(x => x.DeleteCredentialsAsync("delete-test", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteProfileAsync_NonExistent_ReturnsFalse() {
        // Arrange
        repositoryMock
            .Setup(x => x.DeleteAsync("non-existent", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await profileService.DeleteProfileAsync("non-existent");

        // Assert
        Assert.False(result);
        credentialStoreMock.Verify(x => x.DeleteCredentialsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ListProfilesAsync_ReturnsProfileSummaries() {
        // Arrange
        var profileNames = new List<string> { "profile1", "profile2" };

        repositoryMock
            .Setup(x => x.ListProfileNamesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(profileNames);

        repositoryMock
            .Setup(x => x.LoadAsync("profile1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateValidProfile("profile1"));

        repositoryMock
            .Setup(x => x.LoadAsync("profile2", It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateValidProfile("profile2"));

        repositoryMock
            .Setup(x => x.GetProfilePath(It.IsAny<string>()))
            .Returns<string>(name => $@"C:\test\{name}.json");

        credentialStoreMock
            .Setup(x => x.HasCredentialsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var summaries = await profileService.ListProfilesAsync();

        // Assert
        Assert.Equal(2, summaries.Count);
        Assert.Contains(summaries, s => s.Name == "profile1");
        Assert.Contains(summaries, s => s.Name == "profile2");
        Assert.All(summaries, s => Assert.True(s.HasCredentials));
    }

    [Fact]
    public void ValidateProfile_ValidProfile_ReturnsSuccess() {
        // Arrange
        var profile = CreateValidProfile("valid-test");

        // Act
        var result = profileService.ValidateProfile(profile);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void ValidateProfile_InvalidConnection_ReturnsErrors() {
        // Arrange
        var profile = new DeploymentProfile {
            Name = "test",
            Connection = new ServerConnection {
                Host = "", // Invalid: empty host
                Port = 21
            },
            RemotePath = "/www"
        };

        // Act
        var result = profileService.ValidateProfile(profile);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("Connection"));
    }

    [Fact]
    public void ValidateProfile_InvalidConcurrency_ReturnsErrors() {
        // Arrange
        var profile = CreateValidProfile("test");
        profile.Concurrency = 100; // Invalid: too high

        // Act
        var result = profileService.ValidateProfile(profile);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("Concurrency"));
    }

    [Fact]
    public void ValidateProfile_InvalidRetryCount_ReturnsErrors() {
        // Arrange
        var profile = CreateValidProfile("test");
        profile.RetryCount = -1; // Invalid: negative

        // Act
        var result = profileService.ValidateProfile(profile);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("Retry count"));
    }

    [Fact]
    public void ValidateProfile_EmptyRemotePath_ReturnsErrors() {
        // Arrange
        var profile = CreateValidProfile("test");
        profile.RemotePath = ""; // Invalid: empty

        // Act
        var result = profileService.ValidateProfile(profile);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("Remote path"));
    }

    [Fact]
    public void ValidateProfile_InvalidProfileName_ReturnsErrors() {
        // Arrange
        var profile = CreateValidProfile("invalid name with spaces"); // Invalid name

        // Act
        var result = profileService.ValidateProfile(profile);

        // Assert
        Assert.False(result.IsValid);
        Assert.NotEmpty(result.Errors);
    }

    private static DeploymentProfile CreateValidProfile(string name) {
        return new DeploymentProfile {
            Name = name,
            Connection = new ServerConnection("ftp.example.com", 21, ProtocolType.Ftp),
            Username = "testuser",
            Password = "testpass",
            RemotePath = "/www",
            ProjectPath = @"C:\Projects\Test\Test.csproj",
            Concurrency = 4,
            RetryCount = 3,
            Build = new BuildConfiguration("Release")
        };
    }
}
