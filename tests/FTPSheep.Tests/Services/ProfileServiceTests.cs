using FTPSheep.Core.Exceptions;
using FTPSheep.Core.Interfaces;
using FTPSheep.Core.Models;
using FTPSheep.Core.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace FTPSheep.Tests.Services;

public class ProfileServiceTests {
    private readonly Mock<IProfileRepository> repositoryMock;
    private readonly Mock<ICredentialStore> credentialStoreMock;
    private readonly Mock<ILogger<ProfileService>> loggerMock;
    private readonly ProfileService profileService;

    public ProfileServiceTests() {
        repositoryMock = new Mock<IProfileRepository>();
        credentialStoreMock = new Mock<ICredentialStore>();
        loggerMock = new Mock<ILogger<ProfileService>>();

        profileService = new ProfileService(credentialStoreMock.Object, repositoryMock.Object, loggerMock.Object);
    }

    [Fact]
    public async Task CreateProfileAsync_ValidProfile_Succeeds() {
        // Arrange
        var profile = CreateValidProfile("test-profile");
        var profileSavePath = Path.GetTempFileName();

        credentialStoreMock
            .Setup(x => x.SaveCredentialsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await profileService.CreateProfileAsync(profileSavePath, profile);

        // Assert
        Assert.True(File.Exists(profileSavePath));
        credentialStoreMock.Verify(x => x.SaveCredentialsAsync(profileSavePath, "testuser", "testpass", It.IsAny<CancellationToken>()), Times.Once);

        // Cleanup
        File.Delete(profileSavePath);
    }

    [Fact]
    public async Task CreateProfileAsync_InvalidProfile_ThrowsValidationException() {
        // Arrange
        var profile = new DeploymentProfile {
            Name = "", // Invalid: empty name
            Connection = new ServerConnection("ftp.example.com"),
            RemotePath = "/www"
        };
        var profileSavePath = Path.GetTempFileName();

        // Act & Assert
        await Assert.ThrowsAsync<ProfileValidationException>(() => profileService.CreateProfileAsync(profileSavePath, profile));

        // Cleanup
        if(File.Exists(profileSavePath)) {
            File.Delete(profileSavePath);
        }
    }

    [Fact]
    public async Task LoadProfileAsync_ByPath_LoadsFromFile() {
        // Arrange
        var profile = CreateValidProfile("path-test");
        var profilePath = Path.GetTempFileName();

        // Create actual profile file
        await profileService.CreateProfileAsync(profilePath, profile);

        credentialStoreMock
            .Setup(x => x.LoadCredentialsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Credentials?)null);

        repositoryMock
            .Setup(x => x.LoadFromPathAsync(profilePath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);

        // Act
        var loadedProfile = await profileService.LoadProfileAsync(profilePath);

        // Assert
        Assert.NotNull(loadedProfile);
        Assert.Equal("path-test", loadedProfile.Name);

        repositoryMock.Verify(x => x.LoadFromPathAsync(profilePath, It.IsAny<CancellationToken>()), Times.Once);

        // Cleanup
        File.Delete(profilePath);
    }

    [Fact]
    public async Task LoadProfileAsync_NonExistent_ThrowsProfileNotFoundException() {
        // Arrange
        var nonExistentPath = @"C:\non-existent-path\profile.ftpsheep";

        repositoryMock
            .Setup(x => x.LoadFromPathAsync(nonExistentPath, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new FileNotFoundException());

        // Act & Assert
        await Assert.ThrowsAsync<FileNotFoundException>(() => profileService.LoadProfileAsync(nonExistentPath));
    }

    [Fact]
    public async Task UpdateProfileAsync_ValidProfile_Succeeds() {
        // Arrange
        var profile = CreateValidProfile("update-test");
        var profilePath = Path.GetTempFileName();

        repositoryMock
            .Setup(x => x.ExistsAsync(profilePath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        repositoryMock
            .Setup(x => x.SaveAsync(profilePath, It.IsAny<DeploymentProfile>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        credentialStoreMock
            .Setup(x => x.SaveCredentialsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await profileService.UpdateProfileAsync(profile);

        // Assert
        repositoryMock.Verify(x => x.SaveAsync(It.IsAny<string>(), profile, It.IsAny<CancellationToken>()), Times.Once);

        // Cleanup
        if(File.Exists(profilePath)) {
            File.Delete(profilePath);
        }
    }

    [Fact]
    public async Task UpdateProfileAsync_NonExistent_ThrowsProfileNotFoundException() {
        // Arrange
        var profile = CreateValidProfile("non-existent");
        var profilePath = @"C:\non-existent\profile.ftpsheep";

        repositoryMock
            .Setup(x => x.ExistsAsync(profilePath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act & Assert
        await Assert.ThrowsAsync<ProfileNotFoundException>(() => profileService.UpdateProfileAsync(profile));

        repositoryMock.Verify(x => x.SaveAsync(It.IsAny<string>(), It.IsAny<DeploymentProfile>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteProfileAsync_ExistingProfile_ReturnsTrue() {
        // Arrange
        var profilePath = Path.GetTempFileName();

        repositoryMock
            .Setup(x => x.DeleteAsync(profilePath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        credentialStoreMock
            .Setup(x => x.DeleteCredentialsAsync(profilePath, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await profileService.DeleteProfileAsync(profilePath);

        // Assert
        Assert.True(result);
        repositoryMock.Verify(x => x.DeleteAsync(profilePath, It.IsAny<CancellationToken>()), Times.Once);
        credentialStoreMock.Verify(x => x.DeleteCredentialsAsync(profilePath, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteProfileAsync_NonExistent_ReturnsFalse() {
        // Arrange
        var profilePath = @"C:\non-existent\profile.ftpsheep";

        repositoryMock
            .Setup(x => x.DeleteAsync(profilePath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await profileService.DeleteProfileAsync(profilePath);

        // Assert
        Assert.False(result);
        credentialStoreMock.Verify(x => x.DeleteCredentialsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ListProfilesAsync_ReturnsProfileSummaries() {
        // Arrange
        var profilePath1 = @"C:\profiles\profile1.ftpsheep";
        var profilePath2 = @"C:\profiles\profile2.ftpsheep";
        var profilePaths = new List<string> { profilePath1, profilePath2 };

        repositoryMock
            .Setup(x => x.ListProfileNamesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(profilePaths);

        repositoryMock
            .Setup(x => x.LoadFromPathAsync(profilePath1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateValidProfile("profile1"));

        repositoryMock
            .Setup(x => x.LoadFromPathAsync(profilePath2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateValidProfile("profile2"));

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
