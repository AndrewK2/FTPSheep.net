using FTPSheep.Core.Utils;

namespace FTPSheep.Tests.Utils;

public class PathResolverTests
{
    [Fact]
    public void GetApplicationDataPath_ReturnsCorrectPath()
    {
        // Act
        var path = PathResolver.GetApplicationDataPath();

        // Assert
        Assert.NotNull(path);
        Assert.EndsWith(".ftpsheep", path);
        Assert.Contains("AppData", path);
    }

    [Fact]
    public void GetProfilesDirectoryPath_ReturnsCorrectPath()
    {
        // Act
        var path = PathResolver.GetProfilesDirectoryPath();

        // Assert
        Assert.NotNull(path);
        Assert.EndsWith("profiles", path);
        Assert.Contains(".ftpsheep", path);
    }

    [Fact]
    public void GetGlobalConfigPath_ReturnsCorrectPath()
    {
        // Act
        var path = PathResolver.GetGlobalConfigPath();

        // Assert
        Assert.NotNull(path);
        Assert.EndsWith("config.json", path);
        Assert.Contains(".ftpsheep", path);
    }

    [Fact]
    public void GetProfileFilePath_ReturnsCorrectPath()
    {
        // Act
        var path = PathResolver.GetProfileFilePath("test-profile");

        // Assert
        Assert.NotNull(path);
        Assert.EndsWith("test-profile.json", path);
        Assert.Contains(".ftpsheep", path);
        Assert.Contains("profiles", path);
    }

    [Fact]
    public void ValidateProfileName_ValidName_ReturnsTrue()
    {
        // Arrange
        var validNames = new[] { "production", "test-server", "dev_local", "staging123", "MyProfile" };

        foreach (var name in validNames)
        {
            // Act
            var result = PathResolver.ValidateProfileName(name, out var errors);

            // Assert
            Assert.True(result, $"Name '{name}' should be valid. Errors: {string.Join(", ", errors)}");
            Assert.Empty(errors);
        }
    }

    [Fact]
    public void ValidateProfileName_InvalidChars_ReturnsFalse()
    {
        // Arrange
        var invalidNames = new[] { "test profile", "test@profile", "test/profile", "test\\profile", "test.profile" };

        foreach (var name in invalidNames)
        {
            // Act
            var result = PathResolver.ValidateProfileName(name, out var errors);

            // Assert
            Assert.False(result, $"Name '{name}' should be invalid");
            Assert.NotEmpty(errors);
        }
    }

    [Fact]
    public void ValidateProfileName_StartsWithInvalidChar_ReturnsFalse()
    {
        // Arrange
        var name = "-invalid";

        // Act
        var result = PathResolver.ValidateProfileName(name, out var errors);

        // Assert
        Assert.False(result);
        Assert.Contains(errors, e => e.Contains("must start with a letter or number"));
    }

    [Fact]
    public void ValidateProfileName_ReservedName_ReturnsFalse()
    {
        // Arrange
        var reservedNames = new[] { "CON", "PRN", "AUX", "NUL", "COM1", "LPT1" };

        foreach (var name in reservedNames)
        {
            // Act
            var result = PathResolver.ValidateProfileName(name, out var errors);

            // Assert
            Assert.False(result, $"Name '{name}' should be invalid (reserved)");
            Assert.Contains(errors, e => e.Contains("reserved"));
        }
    }

    [Fact]
    public void ValidateProfileName_TooLong_ReturnsFalse()
    {
        // Arrange
        var name = new string('a', 101); // 101 characters

        // Act
        var result = PathResolver.ValidateProfileName(name, out var errors);

        // Assert
        Assert.False(result);
        Assert.Contains(errors, e => e.Contains("cannot exceed"));
    }

    [Fact]
    public void ValidateProfileName_EmptyName_ReturnsFalse()
    {
        // Act
        var result = PathResolver.ValidateProfileName("", out var errors);

        // Assert
        Assert.False(result);
        Assert.Contains(errors, e => e.Contains("cannot be empty"));
    }

    [Fact]
    public void IsAbsolutePath_AbsolutePath_ReturnsTrue()
    {
        // Arrange
        var absolutePaths = new[]
        {
            @"C:\Users\test\profile.json",
            @"D:\Projects\FTPSheep\profile.json",
            @"\\server\share\profile.json",
            "/home/user/profile.json",
            "/var/config/profile.json"
        };

        foreach (var path in absolutePaths)
        {
            // Act
            var result = PathResolver.IsAbsolutePath(path);

            // Assert
            Assert.True(result, $"Path '{path}' should be absolute");
        }
    }

    [Fact]
    public void IsAbsolutePath_ProfileName_ReturnsFalse()
    {
        // Arrange
        var profileNames = new[] { "production", "test-server", "dev_local", "staging" };

        foreach (var name in profileNames)
        {
            // Act
            var result = PathResolver.IsAbsolutePath(name);

            // Assert
            Assert.False(result, $"Name '{name}' should not be treated as absolute path");
        }
    }

    [Fact]
    public void IsAbsolutePath_RelativePath_ReturnsFalse()
    {
        // Arrange
        var relativePaths = new[] { "profiles/test.json", @"config\profile.json", "./test.json" };

        foreach (var path in relativePaths)
        {
            // Act
            var result = PathResolver.IsAbsolutePath(path);

            // Assert
            Assert.False(result, $"Path '{path}' should be relative");
        }
    }

    [Fact]
    public void EnsureDirectoriesExist_CreatesDirectories()
    {
        // This test verifies the method runs without error
        // Actual directory creation is tested implicitly by other integration tests

        // Act & Assert
        var exception = Record.Exception(() => PathResolver.EnsureDirectoriesExist());
        Assert.Null(exception);
    }
}
