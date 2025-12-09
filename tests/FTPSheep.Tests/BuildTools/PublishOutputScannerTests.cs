using FTPSheep.BuildTools.Services;

namespace FTPSheep.Tests.BuildTools;

/// <summary>
/// Tests for the PublishOutputScanner class.
/// </summary>
public class PublishOutputScannerTests : IDisposable {
    private readonly PublishOutputScanner _scanner;
    private readonly string _testDirectory;

    /// <summary>
    /// Initializes a new instance of the <see cref="PublishOutputScannerTests"/> class.
    /// </summary>
    public PublishOutputScannerTests() {
        _scanner = new PublishOutputScanner();
        _testDirectory = Path.Combine(Path.GetTempPath(), $"FTPSheep_Test_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDirectory);
    }

    /// <summary>
    /// Cleans up test files.
    /// </summary>
    public void Dispose() {
        if(Directory.Exists(_testDirectory)) {
            Directory.Delete(_testDirectory, true);
        }
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Tests that scanning a directory with files returns correct file count.
    /// </summary>
    [Fact]
    public void ScanPublishOutput_WithFiles_ReturnsCorrectFileCount() {
        // Arrange
        CreateTestFile("app.dll", 1024);
        CreateTestFile("web.config", 512);
        CreateTestFile("index.html", 256);

        // Act
        var output = _scanner.ScanPublishOutput(_testDirectory, validateOutput: false);

        // Assert
        Assert.Equal(3, output.FileCount);
    }

    /// <summary>
    /// Tests that scanning calculates total size correctly.
    /// </summary>
    [Fact]
    public void ScanPublishOutput_CalculatesTotalSize_Correctly() {
        // Arrange
        CreateTestFile("file1.dll", 1024);
        CreateTestFile("file2.dll", 2048);
        CreateTestFile("file3.dll", 512);

        // Act
        var output = _scanner.ScanPublishOutput(_testDirectory, validateOutput: false);

        // Assert
        Assert.Equal(3584, output.TotalSize);
    }

    /// <summary>
    /// Tests that file metadata includes correct paths.
    /// </summary>
    [Fact]
    public void ScanPublishOutput_SetsFileMetadata_Correctly() {
        // Arrange
        var filePath = CreateTestFile("test.dll", 1024);

        // Act
        var output = _scanner.ScanPublishOutput(_testDirectory, validateOutput: false);

        // Assert
        var file = output.Files.First();
        Assert.Equal("test.dll", file.FileName);
        Assert.Equal(".dll", file.Extension);
        Assert.Equal("test.dll", file.RelativePath);
        Assert.Equal(1024, file.Size);
        Assert.True(file.IsAssembly);
    }

    /// <summary>
    /// Tests that subdirectories are scanned recursively.
    /// </summary>
    [Fact]
    public void ScanPublishOutput_WithSubdirectories_ScansRecursively() {
        // Arrange
        var subDir = Path.Combine(_testDirectory, "bin");
        Directory.CreateDirectory(subDir);
        CreateTestFile("root.dll", 1024);
        CreateTestFile(Path.Combine("bin", "sub.dll"), 512);

        // Act
        var output = _scanner.ScanPublishOutput(_testDirectory, validateOutput: false);

        // Assert
        Assert.Equal(2, output.FileCount);
        Assert.Contains(output.Files, f => f.RelativePath == "root.dll");
        Assert.Contains(output.Files, f => f.RelativePath == Path.Combine("bin", "sub.dll"));
    }

    /// <summary>
    /// Tests that exclusion patterns filter out matching files.
    /// </summary>
    [Fact]
    public void ScanPublishOutput_WithExclusionPatterns_FiltersFiles() {
        // Arrange
        CreateTestFile("app.dll", 1024);
        CreateTestFile("app.pdb", 512);
        CreateTestFile("app.xml", 256);

        var exclusions = new List<string> { "*.pdb", "*.xml" };

        // Act
        var output = _scanner.ScanPublishOutput(_testDirectory, exclusions, validateOutput: false);

        // Assert
        Assert.Single(output.Files);
        Assert.Equal("app.dll", output.Files.First().FileName);
    }

    /// <summary>
    /// Tests that default exclusion patterns exclude debug files.
    /// </summary>
    [Fact]
    public void ScanPublishOutput_WithDefaultExclusions_ExcludesDebugFiles() {
        // Arrange
        CreateTestFile("app.dll", 1024);
        CreateTestFile("app.pdb", 512);

        // Act
        var output = _scanner.ScanPublishOutput(_testDirectory, validateOutput: false);

        // Assert
        Assert.Single(output.Files);
        Assert.Equal("app.dll", output.Files.First().FileName);
    }

    /// <summary>
    /// Tests validation with empty directory.
    /// </summary>
    [Fact]
    public void ScanPublishOutput_WithEmptyDirectory_ReturnsError() {
        // Act
        var output = _scanner.ScanPublishOutput(_testDirectory, validateOutput: true);

        // Assert
        Assert.True(output.HasErrors);
        Assert.Contains("No files found", output.Errors.First());
    }

    /// <summary>
    /// Tests validation detects missing web.config in web apps.
    /// </summary>
    [Fact]
    public void ScanPublishOutput_WebAppWithoutWebConfig_ReturnsWarning() {
        // Arrange
        CreateTestFile("app.dll", 1024);
        CreateTestFile("index.html", 512);

        // Act
        var output = _scanner.ScanPublishOutput(_testDirectory, validateOutput: true);

        // Assert
        Assert.True(output.HasWarnings);
        Assert.Contains(output.Warnings, w => w.Contains("web.config"));
    }

    /// <summary>
    /// Tests validation detects web.config when present.
    /// </summary>
    [Fact]
    public void ScanPublishOutput_WebAppWithWebConfig_NoWarning() {
        // Arrange
        CreateTestFile("app.dll", 1024);
        CreateTestFile("index.html", 512);
        CreateTestFile("web.config", 256);

        // Act
        var output = _scanner.ScanPublishOutput(_testDirectory, validateOutput: true);

        // Assert
        Assert.False(output.Warnings.Any(w => w.Contains("web.config")));
    }

    /// <summary>
    /// Tests validation detects development files.
    /// </summary>
    [Fact]
    public void ScanPublishOutput_WithDevelopmentFiles_ReturnsWarning() {
        // Arrange
        CreateTestFile("app.dll", 1024);
        CreateTestFile("appsettings.Development.json", 256);

        // Act
        var output = _scanner.ScanPublishOutput(_testDirectory, validateOutput: true);

        // Assert
        Assert.True(output.HasWarnings);
        Assert.Contains(output.Warnings, w => w.Contains("Development file"));
    }

    /// <summary>
    /// Tests validation detects large files.
    /// </summary>
    [Fact]
    public void ScanPublishOutput_WithLargeFile_ReturnsWarning() {
        // Arrange - Create a file > 100 MB
        var largeFilePath = Path.Combine(_testDirectory, "large.bin");
        using(var fs = new FileStream(largeFilePath, FileMode.Create)) {
            fs.SetLength(101 * 1024 * 1024); // 101 MB
        }

        // Act
        var output = _scanner.ScanPublishOutput(_testDirectory, validateOutput: true);

        // Assert
        Assert.True(output.HasWarnings);
        Assert.Contains(output.Warnings, w => w.Contains("Large file"));
    }

    /// <summary>
    /// Tests that files are sorted by size correctly.
    /// </summary>
    [Fact]
    public void FilesSortedBySize_ReturnsSortedFiles() {
        // Arrange
        CreateTestFile("large.dll", 3000);
        CreateTestFile("medium.dll", 2000);
        CreateTestFile("small.dll", 1000);

        // Act
        var output = _scanner.ScanPublishOutput(_testDirectory, validateOutput: false);
        var sorted = output.FilesSortedBySize.ToList();

        // Assert
        Assert.Equal("small.dll", sorted[0].FileName);
        Assert.Equal("medium.dll", sorted[1].FileName);
        Assert.Equal("large.dll", sorted[2].FileName);
    }

    /// <summary>
    /// Tests async scanning.
    /// </summary>
    [Fact]
    public async Task ScanPublishOutputAsync_WorksCorrectly() {
        // Arrange
        CreateTestFile("app.dll", 1024);

        // Act
        var output = await _scanner.ScanPublishOutputAsync(_testDirectory, validateOutput: false);

        // Assert
        Assert.Single(output.Files);
    }

    /// <summary>
    /// Tests scanning with null path throws exception.
    /// </summary>
    [Fact]
    public void ScanPublishOutput_WithNullPath_ThrowsException() {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            _scanner.ScanPublishOutput(null!, validateOutput: false));
    }

    /// <summary>
    /// Tests scanning non-existent directory throws exception.
    /// </summary>
    [Fact]
    public void ScanPublishOutput_WithNonExistentDirectory_ThrowsException() {
        // Arrange
        var nonExistentPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        // Act & Assert
        Assert.Throws<DirectoryNotFoundException>(() =>
            _scanner.ScanPublishOutput(nonExistentPath, validateOutput: false));
    }

    /// <summary>
    /// Tests that formatted size is correct.
    /// </summary>
    [Fact]
    public void FormattedTotalSize_FormatsCorrectly() {
        // Arrange
        CreateTestFile("file.dll", 1536); // 1.5 KB

        // Act
        var output = _scanner.ScanPublishOutput(_testDirectory, validateOutput: false);

        // Assert
        Assert.Contains("KB", output.FormattedTotalSize);
    }

    /// <summary>
    /// Helper method to create a test file with specified size.
    /// </summary>
    private string CreateTestFile(string relativePath, int sizeInBytes) {
        var fullPath = Path.Combine(_testDirectory, relativePath);
        var directory = Path.GetDirectoryName(fullPath);
        if(!string.IsNullOrEmpty(directory)) {
            Directory.CreateDirectory(directory);
        }

        var content = new byte[sizeInBytes];
        File.WriteAllBytes(fullPath, content);
        return fullPath;
    }
}
