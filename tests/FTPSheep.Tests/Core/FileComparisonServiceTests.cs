using FTPSheep.BuildTools.Models;
using FTPSheep.Core.Services;
using Xunit;

namespace FTPSheep.Tests.Core;

public class FileComparisonServiceTests {
    #region Constructor Tests

    [Fact]
    public void Constructor_Default_InitializesCorrectly() {
        // Act
        var service = new FileComparisonService();

        // Assert
        Assert.NotNull(service);
    }

    [Fact]
    public void Constructor_WithExclusionMatcher_InitializesCorrectly() {
        // Arrange
        var exclusionMatcher = new ExclusionPatternMatcher(new[] { "*.log" });

        // Act
        var service = new FileComparisonService(exclusionMatcher);

        // Assert
        Assert.NotNull(service);
    }

    #endregion

    #region CompareFiles Tests - Basic Scenarios

    [Fact]
    public void CompareFiles_WithIdenticalFiles_ReturnsNoObsoleteFiles() {
        // Arrange
        var service = new FileComparisonService();
        var localFiles = new List<FileMetadata> {
            CreateFileMetadata("file1.txt"),
            CreateFileMetadata("file2.txt"),
            CreateFileMetadata("file3.txt")
        };
        var remoteFiles = new List<string> {
            "file1.txt",
            "file2.txt",
            "file3.txt"
        };

        // Act
        var result = service.CompareFiles(localFiles, remoteFiles);

        // Assert
        Assert.Equal(0, result.ObsoleteFileCount);
        Assert.Empty(result.ObsoleteFiles);
        Assert.False(result.HasObsoleteFiles);
    }

    [Fact]
    public void CompareFiles_WithObsoleteFiles_IdentifiesThemCorrectly() {
        // Arrange
        var service = new FileComparisonService();
        var localFiles = new List<FileMetadata> {
            CreateFileMetadata("file1.txt"),
            CreateFileMetadata("file2.txt")
        };
        var remoteFiles = new List<string> {
            "file1.txt",
            "file2.txt",
            "obsolete1.txt",
            "obsolete2.txt"
        };

        // Act
        var result = service.CompareFiles(localFiles, remoteFiles);

        // Assert
        Assert.Equal(2, result.ObsoleteFileCount);
        Assert.Contains("obsolete1.txt", result.ObsoleteFiles);
        Assert.Contains("obsolete2.txt", result.ObsoleteFiles);
        Assert.True(result.HasObsoleteFiles);
    }

    [Fact]
    public void CompareFiles_WithNewLocalFiles_DoesNotMarkAsObsolete() {
        // Arrange
        var service = new FileComparisonService();
        var localFiles = new List<FileMetadata> {
            CreateFileMetadata("file1.txt"),
            CreateFileMetadata("file2.txt"),
            CreateFileMetadata("newfile.txt")
        };
        var remoteFiles = new List<string> {
            "file1.txt",
            "file2.txt"
        };

        // Act
        var result = service.CompareFiles(localFiles, remoteFiles);

        // Assert
        Assert.Equal(0, result.ObsoleteFileCount);
        Assert.Empty(result.ObsoleteFiles);
    }

    #endregion

    #region CompareFiles Tests - With Exclusion Patterns

    [Fact]
    public void CompareFiles_WithExcludedFiles_ExcludesFromObsoleteList() {
        // Arrange
        var exclusionMatcher = new ExclusionPatternMatcher(new[] { "App_Data/**", "*.log" });
        var service = new FileComparisonService(exclusionMatcher);
        var localFiles = new List<FileMetadata> {
            CreateFileMetadata("file1.txt")
        };
        var remoteFiles = new List<string> {
            "file1.txt",
            "App_Data/users.db",
            "error.log",
            "obsolete.txt"
        };

        // Act
        var result = service.CompareFiles(localFiles, remoteFiles);

        // Assert
        Assert.Equal(1, result.ObsoleteFileCount);
        Assert.Contains("obsolete.txt", result.ObsoleteFiles);
        Assert.DoesNotContain("App_Data/users.db", result.ObsoleteFiles);
        Assert.DoesNotContain("error.log", result.ObsoleteFiles);
        Assert.Equal(2, result.ExcludedFileCount);
        Assert.Contains("App_Data/users.db", result.ExcludedFiles);
        Assert.Contains("error.log", result.ExcludedFiles);
    }

    [Fact]
    public void CompareFiles_WithExcludedFilesOnly_ReturnsNoObsoleteFiles() {
        // Arrange
        var exclusionMatcher = new ExclusionPatternMatcher(new[] { "App_Data/**" });
        var service = new FileComparisonService(exclusionMatcher);
        var localFiles = new List<FileMetadata> {
            CreateFileMetadata("file1.txt")
        };
        var remoteFiles = new List<string> {
            "file1.txt",
            "App_Data/users.db",
            "App_Data/logs/error.log"
        };

        // Act
        var result = service.CompareFiles(localFiles, remoteFiles);

        // Assert
        Assert.Equal(0, result.ObsoleteFileCount);
        Assert.False(result.HasObsoleteFiles);
        Assert.Equal(2, result.ExcludedFileCount);
        Assert.True(result.HasExcludedFiles);
    }

    #endregion

    #region CompareFiles Tests - Case Sensitivity

    [Fact]
    public void CompareFiles_CaseInsensitive_MatchesCorrectly() {
        // Arrange
        var service = new FileComparisonService();
        var localFiles = new List<FileMetadata> {
            CreateFileMetadata("File1.TXT"),
            CreateFileMetadata("File2.txt")
        };
        var remoteFiles = new List<string> {
            "file1.txt",
            "FILE2.TXT"
        };

        // Act
        var result = service.CompareFiles(localFiles, remoteFiles);

        // Assert
        Assert.Equal(0, result.ObsoleteFileCount);
        Assert.Empty(result.ObsoleteFiles);
    }

    #endregion

    #region CompareFiles Tests - Path Normalization

    [Fact]
    public void CompareFiles_WithBackslashSeparators_NormalizesCorrectly() {
        // Arrange
        var service = new FileComparisonService();
        var localFiles = new List<FileMetadata> {
            CreateFileMetadata("folder/file1.txt"),
            CreateFileMetadata("folder/sub/file2.txt")
        };
        var remoteFiles = new List<string> {
            "folder\\file1.txt",
            "folder\\sub\\file2.txt"
        };

        // Act
        var result = service.CompareFiles(localFiles, remoteFiles);

        // Assert
        Assert.Equal(0, result.ObsoleteFileCount);
        Assert.Empty(result.ObsoleteFiles);
    }

    #endregion

    #region CompareFiles Tests - Statistics

    [Fact]
    public void CompareFiles_ReturnsCorrectStatistics() {
        // Arrange
        var exclusionMatcher = new ExclusionPatternMatcher(new[] { "*.log" });
        var service = new FileComparisonService(exclusionMatcher);
        var localFiles = new List<FileMetadata> {
            CreateFileMetadata("file1.txt"),
            CreateFileMetadata("file2.txt")
        };
        var remoteFiles = new List<string> {
            "file1.txt",
            "file2.txt",
            "obsolete1.txt",
            "obsolete2.txt",
            "error.log"
        };

        // Act
        var result = service.CompareFiles(localFiles, remoteFiles);

        // Assert
        Assert.Equal(2, result.TotalLocalFiles);
        Assert.Equal(5, result.TotalRemoteFiles);
        Assert.Equal(2, result.ObsoleteFileCount);
        Assert.Equal(1, result.ExcludedFileCount);
    }

    #endregion

    #region CompareFiles Tests - Null Arguments

    [Fact]
    public void CompareFiles_WithNullLocalFiles_ThrowsArgumentNullException() {
        // Arrange
        var service = new FileComparisonService();
        var remoteFiles = new List<string> { "file1.txt" };

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            service.CompareFiles(null!, remoteFiles));
    }

    [Fact]
    public void CompareFiles_WithNullRemoteFiles_ThrowsArgumentNullException() {
        // Arrange
        var service = new FileComparisonService();
        var localFiles = new List<FileMetadata> { CreateFileMetadata("file1.txt") };

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            service.CompareFiles(localFiles, null!));
    }

    #endregion

    #region CompareFiles Tests - Empty Collections

    [Fact]
    public void CompareFiles_WithEmptyLocalFiles_MarksAllRemoteAsObsolete() {
        // Arrange
        var service = new FileComparisonService();
        var localFiles = new List<FileMetadata>();
        var remoteFiles = new List<string> {
            "file1.txt",
            "file2.txt",
            "file3.txt"
        };

        // Act
        var result = service.CompareFiles(localFiles, remoteFiles);

        // Assert
        Assert.Equal(3, result.ObsoleteFileCount);
        Assert.Equal(3, result.ObsoleteFiles.Count);
    }

    [Fact]
    public void CompareFiles_WithEmptyRemoteFiles_ReturnsNoObsoleteFiles() {
        // Arrange
        var service = new FileComparisonService();
        var localFiles = new List<FileMetadata> {
            CreateFileMetadata("file1.txt"),
            CreateFileMetadata("file2.txt")
        };
        var remoteFiles = new List<string>();

        // Act
        var result = service.CompareFiles(localFiles, remoteFiles);

        // Assert
        Assert.Equal(0, result.ObsoleteFileCount);
        Assert.Empty(result.ObsoleteFiles);
    }

    #endregion

    #region IdentifyEmptyDirectories Tests

    [Fact]
    public void IdentifyEmptyDirectories_WithEmptyDirectory_IdentifiesCorrectly() {
        // Arrange
        var service = new FileComparisonService();
        var obsoleteFiles = new List<string> {
            "temp/file1.txt",
            "temp/file2.txt"
        };
        var allRemoteFiles = new List<string> {
            "temp/file1.txt",
            "temp/file2.txt",
            "other/file3.txt"
        };

        // Act
        var result = service.IdentifyEmptyDirectories(obsoleteFiles, allRemoteFiles);

        // Assert
        Assert.Single(result);
        Assert.Contains("temp", result);
    }

    [Fact]
    public void IdentifyEmptyDirectories_WithPartiallyEmptyDirectory_DoesNotIdentify() {
        // Arrange
        var service = new FileComparisonService();
        var obsoleteFiles = new List<string> {
            "temp/file1.txt"
        };
        var allRemoteFiles = new List<string> {
            "temp/file1.txt",
            "temp/file2.txt"  // This file is not obsolete
        };

        // Act
        var result = service.IdentifyEmptyDirectories(obsoleteFiles, allRemoteFiles);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void IdentifyEmptyDirectories_WithNestedDirectories_IdentifiesAll() {
        // Arrange
        var service = new FileComparisonService();
        var obsoleteFiles = new List<string> {
            "temp/sub1/file1.txt",
            "temp/sub2/file2.txt"
        };
        var allRemoteFiles = new List<string> {
            "temp/sub1/file1.txt",
            "temp/sub2/file2.txt",
            "other/file3.txt"
        };

        // Act
        var result = service.IdentifyEmptyDirectories(obsoleteFiles, allRemoteFiles);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains("temp/sub1", result);
        Assert.Contains("temp/sub2", result);
    }

    [Fact]
    public void IdentifyEmptyDirectories_WithNullObsoleteFiles_ThrowsArgumentNullException() {
        // Arrange
        var service = new FileComparisonService();
        var allRemoteFiles = new List<string> { "file1.txt" };

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            service.IdentifyEmptyDirectories(null!, allRemoteFiles));
    }

    [Fact]
    public void IdentifyEmptyDirectories_WithNullAllRemoteFiles_ThrowsArgumentNullException() {
        // Arrange
        var service = new FileComparisonService();
        var obsoleteFiles = new List<string> { "file1.txt" };

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            service.IdentifyEmptyDirectories(obsoleteFiles, null!));
    }

    #endregion

    #region Helper Methods

    private static FileMetadata CreateFileMetadata(string relativePath) {
        return new FileMetadata {
            RelativePath = relativePath,
            AbsolutePath = Path.Combine("C:\\temp", relativePath),
            FileName = Path.GetFileName(relativePath),
            Size = 1024,
            LastModified = DateTime.UtcNow
        };
    }

    #endregion
}
