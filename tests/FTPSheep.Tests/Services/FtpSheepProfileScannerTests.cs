using FTPSheep.Core.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace FTPSheep.Tests.Services;

public class FtpSheepProfileScannerTests : IDisposable {
    private readonly string _testRoot;
    private readonly FtpSheepProfileScanner _scanner;

    public FtpSheepProfileScannerTests() {
        _testRoot = Path.Combine(Path.GetTempPath(), "FtpSheepTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testRoot);
        _scanner = new FtpSheepProfileScanner(NullLogger<FtpSheepProfileScanner>.Instance);
    }

    public void Dispose() {
        if(Directory.Exists(_testRoot)) {
            Directory.Delete(_testRoot, true);
        }
    }

    #region System Directory Detection Tests

    [Fact]
    public void IsSystemDirectory_RootDrive_ReturnsTrue() {
        // Arrange
        var driveLetter = Path.GetPathRoot(Environment.SystemDirectory);

        // Act
        var result = _scanner.IsSystemDirectory(driveLetter!, out var message);

        // Assert
        Assert.True(result);
        Assert.NotNull(message);
        Assert.Contains("root of a drive", message);
    }

    [Fact]
    public void IsSystemDirectory_WindowsFolder_ReturnsTrue() {
        // Arrange
        var windowsPath = Environment.GetFolderPath(Environment.SpecialFolder.Windows);

        // Act
        var result = _scanner.IsSystemDirectory(windowsPath, out var message);

        // Assert
        Assert.True(result);
        Assert.NotNull(message);
        Assert.Contains("Windows", message);
    }

    [Fact]
    public void IsSystemDirectory_ProgramFiles_ReturnsTrue() {
        // Arrange
        var programFilesPath = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);

        // Skip test if ProgramFiles path is empty (shouldn't happen on Windows)
        if(string.IsNullOrEmpty(programFilesPath)) {
            return;
        }

        // Act
        var result = _scanner.IsSystemDirectory(programFilesPath, out var message);

        // Assert
        Assert.True(result);
        Assert.NotNull(message);
        Assert.Contains("Program Files", message);
    }

    [Fact]
    public void IsSystemDirectory_ProgramFilesX86_ReturnsTrue() {
        // Arrange
        var programFilesX86Path = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);

        // Skip test if ProgramFilesX86 path is empty (32-bit systems don't have this)
        if(string.IsNullOrEmpty(programFilesX86Path)) {
            return;
        }

        // Act
        var result = _scanner.IsSystemDirectory(programFilesX86Path, out var message);

        // Assert
        Assert.True(result);
        Assert.NotNull(message);
        Assert.Contains("Program Files", message);
    }

    [Fact]
    public void IsSystemDirectory_ProgramData_ReturnsTrue() {
        // Arrange
        var programDataPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);

        // Act
        var result = _scanner.IsSystemDirectory(programDataPath, out var message);

        // Assert
        Assert.True(result);
        Assert.NotNull(message);
        Assert.Contains("ProgramData", message);
    }

    [Fact]
    public void IsSystemDirectory_UserProfileRoot_ReturnsTrue() {
        // Arrange
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var usersRoot = Path.GetDirectoryName(userProfile);

        // Skip if we can't determine users root
        if(string.IsNullOrEmpty(usersRoot)) {
            return;
        }

        // Act
        var result = _scanner.IsSystemDirectory(usersRoot, out var message);

        // Assert
        Assert.True(result);
        Assert.NotNull(message);
        Assert.Contains("Users", message);
    }

    [Fact]
    public void IsSystemDirectory_UserProfileSubdirectory_ReturnsFalse() {
        // Arrange
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var projectPath = Path.Combine(userProfile, "Projects");

        // Act
        var result = _scanner.IsSystemDirectory(projectPath, out var message);

        // Assert
        Assert.False(result);
        Assert.Null(message);
    }

    [Fact]
    public void IsSystemDirectory_ProjectDirectory_ReturnsFalse() {
        // Arrange
        var projectPath = Path.Combine(_testRoot, "MyProject");
        Directory.CreateDirectory(projectPath);

        // Act
        var result = _scanner.IsSystemDirectory(projectPath, out var message);

        // Assert
        Assert.False(result);
        Assert.Null(message);
    }

    [Fact]
    public void IsFileSystemRoot_VariousDrives_DetectsCorrectly() {
        // Arrange
        var currentDrive = Path.GetPathRoot(Environment.SystemDirectory);

        // Act
        var result = _scanner.IsFileSystemRoot(currentDrive!);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsFileSystemRoot_SubDirectory_ReturnsFalse() {
        // Arrange
        var subDir = Path.Combine(_testRoot, "subfolder");

        // Act
        var result = _scanner.IsFileSystemRoot(subDir);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsFileSystemRoot_NullOrEmpty_ReturnsFalse() {
        // Act & Assert
        Assert.False(_scanner.IsFileSystemRoot(null!));
        Assert.False(_scanner.IsFileSystemRoot(""));
        Assert.False(_scanner.IsFileSystemRoot("   "));
    }

    #endregion

    #region Profile Discovery Tests

    [Fact]
    public void DiscoverProfiles_SingleFile_ReturnsFile() {
        // Arrange
        var profilePath = Path.Combine(_testRoot, "profile1.ftpsheep");
        File.WriteAllText(profilePath, "{}");

        // Act
        var results = _scanner.DiscoverProfiles(_testRoot);

        // Assert
        Assert.Single(results);
        Assert.Equal(profilePath, results[0]);
    }

    [Fact]
    public void DiscoverProfiles_MultipleFiles_ReturnsAll() {
        // Arrange
        var profile1 = Path.Combine(_testRoot, "profile1.ftpsheep");
        var profile2 = Path.Combine(_testRoot, "profile2.ftpsheep");
        var profile3 = Path.Combine(_testRoot, "profile3.ftpsheep");
        File.WriteAllText(profile1, "{}");
        File.WriteAllText(profile2, "{}");
        File.WriteAllText(profile3, "{}");

        // Act
        var results = _scanner.DiscoverProfiles(_testRoot);

        // Assert
        Assert.Equal(3, results.Count);
        Assert.Contains(profile1, results);
        Assert.Contains(profile2, results);
        Assert.Contains(profile3, results);
    }

    [Fact]
    public void DiscoverProfiles_NoFiles_ReturnsEmpty() {
        // Arrange
        // No .ftpsheep files created

        // Act
        var results = _scanner.DiscoverProfiles(_testRoot);

        // Assert
        Assert.Empty(results);
    }

    [Fact]
    public void DiscoverProfiles_NestedDirectories_FindsAll() {
        // Arrange
        var level1 = Path.Combine(_testRoot, "level1");
        var level2 = Path.Combine(level1, "level2");
        Directory.CreateDirectory(level2);

        var profile1 = Path.Combine(_testRoot, "root.ftpsheep");
        var profile2 = Path.Combine(level1, "level1.ftpsheep");
        var profile3 = Path.Combine(level2, "level2.ftpsheep");

        File.WriteAllText(profile1, "{}");
        File.WriteAllText(profile2, "{}");
        File.WriteAllText(profile3, "{}");

        // Act
        var results = _scanner.DiscoverProfiles(_testRoot);

        // Assert
        Assert.Equal(3, results.Count);
        Assert.Contains(profile1, results);
        Assert.Contains(profile2, results);
        Assert.Contains(profile3, results);
    }

    [Fact]
    public void DiscoverProfiles_OnlyFtpSheepExtension_IgnoresOthers() {
        // Arrange
        var ftpsheepFile = Path.Combine(_testRoot, "profile.ftpsheep");
        var jsonFile = Path.Combine(_testRoot, "config.json");
        var txtFile = Path.Combine(_testRoot, "readme.txt");

        File.WriteAllText(ftpsheepFile, "{}");
        File.WriteAllText(jsonFile, "{}");
        File.WriteAllText(txtFile, "text");

        // Act
        var results = _scanner.DiscoverProfiles(_testRoot);

        // Assert
        Assert.Single(results);
        Assert.Equal(ftpsheepFile, results[0]);
    }

    [Fact]
    public void DiscoverProfiles_EmptyDirectory_ReturnsEmpty() {
        // Arrange
        var emptyDir = Path.Combine(_testRoot, "empty");
        Directory.CreateDirectory(emptyDir);

        // Act
        var results = _scanner.DiscoverProfiles(emptyDir);

        // Assert
        Assert.Empty(results);
    }

    [Fact]
    public void DiscoverProfiles_NonExistentDirectory_ReturnsEmpty() {
        // Arrange
        var nonExistent = Path.Combine(_testRoot, "does_not_exist");

        // Act
        var results = _scanner.DiscoverProfiles(nonExistent);

        // Assert
        Assert.Empty(results);
    }

    [Fact]
    public void DiscoverProfiles_SkipsHiddenDirectories() {
        // Arrange
        var hiddenDir = Path.Combine(_testRoot, ".hidden");
        Directory.CreateDirectory(hiddenDir);

        var hiddenDirInfo = new DirectoryInfo(hiddenDir);
        hiddenDirInfo.Attributes |= FileAttributes.Hidden;

        var profileInHidden = Path.Combine(hiddenDir, "hidden.ftpsheep");
        File.WriteAllText(profileInHidden, "{}");

        var profileInRoot = Path.Combine(_testRoot, "root.ftpsheep");
        File.WriteAllText(profileInRoot, "{}");

        // Act
        var results = _scanner.DiscoverProfiles(_testRoot);

        // Assert
        Assert.Single(results);
        Assert.Equal(profileInRoot, results[0]);
        Assert.DoesNotContain(profileInHidden, results);
    }

    #endregion

    #region Limit Enforcement Tests

    [Fact]
    public void DiscoverProfiles_MaxDepthLimit_StopsAtLimit() {
        // Arrange
        var level1 = Path.Combine(_testRoot, "l1");
        var level2 = Path.Combine(level1, "l2");
        var level3 = Path.Combine(level2, "l3");
        var level4 = Path.Combine(level3, "l4");

        Directory.CreateDirectory(level4);

        var profile1 = Path.Combine(level1, "p1.ftpsheep");
        var profile2 = Path.Combine(level2, "p2.ftpsheep");
        var profile3 = Path.Combine(level3, "p3.ftpsheep");
        var profile4 = Path.Combine(level4, "p4.ftpsheep");

        File.WriteAllText(profile1, "{}");
        File.WriteAllText(profile2, "{}");
        File.WriteAllText(profile3, "{}");
        File.WriteAllText(profile4, "{}");

        // Act - max depth of 2 should find p1 and p2, but not p3 or p4
        var results = _scanner.DiscoverProfiles(_testRoot, maxDepth: 2);

        // Assert
        Assert.Equal(2, results.Count);
        Assert.Contains(profile1, results);
        Assert.Contains(profile2, results);
        Assert.DoesNotContain(profile3, results);
        Assert.DoesNotContain(profile4, results);
    }

    [Fact]
    public void DiscoverProfiles_MaxFilesLimit_StopsAtLimit() {
        // Arrange
        for(var i = 1; i <= 10; i++) {
            var profilePath = Path.Combine(_testRoot, $"profile{i}.ftpsheep");
            File.WriteAllText(profilePath, "{}");
        }

        // Act - limit to 5 files
        var results = _scanner.DiscoverProfiles(_testRoot, maxFiles: 5);

        // Assert
        Assert.Equal(5, results.Count);
    }

    [Fact]
    public void DiscoverProfiles_DefaultLimits_Applied() {
        // Arrange
        var profile = Path.Combine(_testRoot, "test.ftpsheep");
        File.WriteAllText(profile, "{}");

        // Act - use default limits
        var results = _scanner.DiscoverProfiles(_testRoot);

        // Assert
        Assert.Single(results);
    }

    [Fact]
    public void DiscoverProfiles_CustomLimits_Applied() {
        // Arrange
        var level1 = Path.Combine(_testRoot, "l1");
        Directory.CreateDirectory(level1);

        for(var i = 1; i <= 20; i++) {
            var profilePath = Path.Combine(_testRoot, $"profile{i}.ftpsheep");
            File.WriteAllText(profilePath, "{}");
        }

        // Act - custom limits: depth 5, files 10
        var results = _scanner.DiscoverProfiles(_testRoot, maxDepth: 5, maxFiles: 10);

        // Assert
        Assert.Equal(10, results.Count);
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public void DiscoverProfiles_NullPath_ThrowsException() {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _scanner.DiscoverProfiles(null!));
    }

    [Fact]
    public void DiscoverProfiles_EmptyPath_ThrowsException() {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _scanner.DiscoverProfiles(""));
    }

    [Fact]
    public void DiscoverProfiles_WhitespacePath_ThrowsException() {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _scanner.DiscoverProfiles("   "));
    }

    [Fact]
    public void IsSystemDirectory_NullPath_ReturnsFalse() {
        // Act
        var result = _scanner.IsSystemDirectory(null!, out var message);

        // Assert
        Assert.False(result);
        Assert.Null(message);
    }

    [Fact]
    public void IsSystemDirectory_EmptyPath_ReturnsFalse() {
        // Act
        var result = _scanner.IsSystemDirectory("", out var message);

        // Assert
        Assert.False(result);
        Assert.Null(message);
    }

    #endregion
}
