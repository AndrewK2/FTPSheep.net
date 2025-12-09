using FTPSheep.Core.Exceptions;
using FTPSheep.Core.Models;
using FTPSheep.Core.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace FTPSheep.Tests.Services;

public class JsonProfileRepositoryTests : IDisposable {
    private readonly string testDirectory;
    private readonly JsonProfileRepository repository;
    private readonly Mock<ILogger<JsonProfileRepository>> loggerMock;

    public JsonProfileRepositoryTests() {
        // Create a unique temporary directory for each test
        testDirectory = Path.Combine(Path.GetTempPath(), "FTPSheep.Tests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(testDirectory);

        loggerMock = new Mock<ILogger<JsonProfileRepository>>();
        repository = new JsonProfileRepository(loggerMock.Object);

        // Override the profiles directory for testing by creating it within test directory
        // Note: In real implementation, we'd inject a configuration or use a test-specific path
        Environment.SetEnvironmentVariable("FTPSHEEP_TEST_MODE", "true");
    }

    public void Dispose() {
        // Clean up test directory
        if(Directory.Exists(testDirectory)) {
            Directory.Delete(testDirectory, recursive: true);
        }

        Environment.SetEnvironmentVariable("FTPSHEEP_TEST_MODE", null);
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task SaveAsync_ValidProfile_CreatesFile() {
        // Arrange
        var profile = new DeploymentProfile {
            Name = "test-profile",
            Connection = new ServerConnection("ftp.example.com", 21, ProtocolType.Ftp),
            Username = "testuser",
            RemotePath = "/www"
        };

        // Act
        await repository.SaveAsync(profile);

        // Assert
        var filePath = repository.GetProfilePath("test-profile");
        Assert.True(File.Exists(filePath));

        // Verify JSON content
        var json = await File.ReadAllTextAsync(filePath);
        Assert.Contains("test-profile", json);
        Assert.Contains("ftp.example.com", json);
    }

    [Fact]
    public async Task SaveAsync_InvalidName_ThrowsException() {
        // Arrange
        var profile = new DeploymentProfile {
            Name = "invalid name with spaces",
            Connection = new ServerConnection("ftp.example.com")
        };

        // Act & Assert
        await Assert.ThrowsAsync<ProfileStorageException>(() => repository.SaveAsync(profile));
    }

    [Fact]
    public async Task SaveAsync_EmptyName_ThrowsException() {
        // Arrange
        var profile = new DeploymentProfile {
            Name = "",
            Connection = new ServerConnection("ftp.example.com")
        };

        // Act & Assert
        await Assert.ThrowsAsync<ProfileStorageException>(() => repository.SaveAsync(profile));
    }

    [Fact]
    public async Task LoadAsync_ExistingProfile_ReturnsProfile() {
        // Arrange
        var originalProfile = new DeploymentProfile {
            Name = "test-load",
            Connection = new ServerConnection("ftp.example.com", 21, ProtocolType.Ftp),
            Username = "testuser",
            RemotePath = "/www",
            Build = new BuildConfiguration("Release")
        };

        await repository.SaveAsync(originalProfile);

        // Act
        var loadedProfile = await repository.LoadAsync("test-load");

        // Assert
        Assert.NotNull(loadedProfile);
        Assert.Equal("test-load", loadedProfile.Name);
        Assert.Equal("ftp.example.com", loadedProfile.Connection.Host);
        Assert.Equal(21, loadedProfile.Connection.Port);
        Assert.Equal("testuser", loadedProfile.Username);
        Assert.Equal("/www", loadedProfile.RemotePath);
    }

    [Fact]
    public async Task LoadAsync_NonExistent_ReturnsNull() {
        // Act
        var profile = await repository.LoadAsync("non-existent-profile");

        // Assert
        Assert.Null(profile);
    }

    [Fact]
    public async Task LoadAsync_CorruptedJson_ThrowsProfileStorageException() {
        // Arrange
        var filePath = repository.GetProfilePath("corrupted");
        var directory = Path.GetDirectoryName(filePath)!;
        Directory.CreateDirectory(directory);
        await File.WriteAllTextAsync(filePath, "{ invalid json content !!!!");

        // Act & Assert
        await Assert.ThrowsAsync<ProfileStorageException>(() => repository.LoadAsync("corrupted"));
    }

    [Fact]
    public async Task LoadFromPathAsync_ExistingFile_ReturnsProfile() {
        // Arrange
        var profile = new DeploymentProfile {
            Name = "path-test",
            Connection = new ServerConnection("ftp.example.com")
        };

        await repository.SaveAsync(profile);
        var filePath = repository.GetProfilePath("path-test");

        // Act
        var loadedProfile = await repository.LoadFromPathAsync(filePath);

        // Assert
        Assert.NotNull(loadedProfile);
        Assert.Equal("path-test", loadedProfile.Name);
    }

    [Fact]
    public async Task LoadFromPathAsync_NonExistentFile_ThrowsFileNotFoundException() {
        // Arrange
        var filePath = Path.Combine(testDirectory, "non-existent.json");

        // Act & Assert
        await Assert.ThrowsAsync<FileNotFoundException>(() => repository.LoadFromPathAsync(filePath));
    }

    [Fact]
    public async Task ListProfileNamesAsync_MultipleProfiles_ReturnsSortedList() {
        // Arrange - Clean up all existing profiles first
        var existingProfiles = await repository.ListProfileNamesAsync();
        foreach(var existing in existingProfiles) {
            await repository.DeleteAsync(existing);
        }

        var profiles = new[]
        {
            new DeploymentProfile { Name = "zebra", Connection = new ServerConnection("ftp.example.com") },
            new DeploymentProfile { Name = "alpha", Connection = new ServerConnection("ftp.example.com") },
            new DeploymentProfile { Name = "beta", Connection = new ServerConnection("ftp.example.com") }
        };

        foreach(var profile in profiles) {
            await repository.SaveAsync(profile);
        }

        // Act
        var profileNames = await repository.ListProfileNamesAsync();

        // Assert
        Assert.Equal(3, profileNames.Count);
        Assert.Equal("alpha", profileNames[0]);
        Assert.Equal("beta", profileNames[1]);
        Assert.Equal("zebra", profileNames[2]);
    }

    [Fact]
    public async Task ListProfileNamesAsync_EmptyDirectory_ReturnsEmptyList() {
        // Arrange - Clean up all existing profiles first
        var existingProfiles = await repository.ListProfileNamesAsync();
        foreach(var profile in existingProfiles) {
            await repository.DeleteAsync(profile);
        }

        // Act
        var profileNames = await repository.ListProfileNamesAsync();

        // Assert
        Assert.Empty(profileNames);
    }

    [Fact]
    public async Task DeleteAsync_ExistingProfile_ReturnsTrue() {
        // Arrange
        var profile = new DeploymentProfile {
            Name = "delete-test",
            Connection = new ServerConnection("ftp.example.com")
        };

        await repository.SaveAsync(profile);
        var filePath = repository.GetProfilePath("delete-test");
        Assert.True(File.Exists(filePath));

        // Act
        var result = await repository.DeleteAsync("delete-test");

        // Assert
        Assert.True(result);
        Assert.False(File.Exists(filePath));
    }

    [Fact]
    public async Task DeleteAsync_NonExistentProfile_ReturnsFalse() {
        // Act
        var result = await repository.DeleteAsync("non-existent");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task ExistsAsync_ExistingProfile_ReturnsTrue() {
        // Arrange
        var profile = new DeploymentProfile {
            Name = "exists-test",
            Connection = new ServerConnection("ftp.example.com")
        };

        await repository.SaveAsync(profile);

        // Act
        var exists = await repository.ExistsAsync("exists-test");

        // Assert
        Assert.True(exists);
    }

    [Fact]
    public async Task ExistsAsync_NonExistentProfile_ReturnsFalse() {
        // Act
        var exists = await repository.ExistsAsync("non-existent");

        // Assert
        Assert.False(exists);
    }

    [Fact]
    public void GetProfilePath_ReturnsCorrectPath() {
        // Act
        var path = repository.GetProfilePath("test-profile");

        // Assert
        Assert.NotNull(path);
        Assert.EndsWith("test-profile.json", path);
        Assert.Contains("profiles", path);
    }

    [Fact]
    public async Task SaveAsync_OverwriteExisting_UpdatesFile() {
        // Arrange
        var profile1 = new DeploymentProfile {
            Name = "update-test",
            Connection = new ServerConnection("ftp1.example.com"),
            RemotePath = "/old"
        };

        await repository.SaveAsync(profile1);

        var profile2 = new DeploymentProfile {
            Name = "update-test",
            Connection = new ServerConnection("ftp2.example.com"),
            RemotePath = "/new"
        };

        // Act
        await repository.SaveAsync(profile2);

        // Assert
        var loadedProfile = await repository.LoadAsync("update-test");
        Assert.NotNull(loadedProfile);
        Assert.Equal("ftp2.example.com", loadedProfile.Connection.Host);
        Assert.Equal("/new", loadedProfile.RemotePath);
    }
}
