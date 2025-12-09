using FTPSheep.BuildTools.Services;

namespace FTPSheep.Tests.BuildTools;

/// <summary>
/// Tests for the BuildService class.
/// </summary>
public class BuildServiceTests {
    private readonly BuildService _buildService;

    /// <summary>
    /// Initializes a new instance of the <see cref="BuildServiceTests"/> class.
    /// </summary>
    public BuildServiceTests() {
        _buildService = new BuildService();
    }

    /// <summary>
    /// Tests that GetProjectInfo throws FileNotFoundException for non-existent project.
    /// </summary>
    [Fact]
    public void GetProjectInfo_WithNonExistentProject_ThrowsFileNotFoundException() {
        // Arrange
        var projectPath = Path.Combine(Path.GetTempPath(), "NonExistent.csproj");

        // Act & Assert
        Assert.Throws<FileNotFoundException>(() => _buildService.GetProjectInfo(projectPath));
    }

    /// <summary>
    /// Tests that PublishDotNetCoreAsync throws InvalidOperationException for .NET Framework projects.
    /// </summary>
    [Fact]
    public async Task PublishDotNetCoreAsync_WithFrameworkProject_ThrowsInvalidOperationException() {
        // Arrange - Create a temporary .NET Framework project file
        var tempPath = Path.GetTempFileName();
        File.Delete(tempPath);
        var projectPath = Path.ChangeExtension(tempPath, ".csproj");

        try {
            // Create a legacy .NET Framework project file
            File.WriteAllText(projectPath, @"<?xml version=""1.0"" encoding=""utf-8""?>
<Project ToolsVersion=""15.0"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
  <PropertyGroup>
    <TargetFrameworkVersion>v4.7.2</TargetFrameworkVersion>
    <OutputType>Exe</OutputType>
  </PropertyGroup>
</Project>");

            var outputPath = Path.Combine(Path.GetTempPath(), "TestOutput");

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await _buildService.PublishDotNetCoreAsync(projectPath, outputPath));
        } finally {
            if(File.Exists(projectPath)) {
                File.Delete(projectPath);
            }
        }
    }

    /// <summary>
    /// Tests that GetProjectInfo correctly identifies an SDK-style project.
    /// </summary>
    [Fact]
    public void GetProjectInfo_WithSdkStyleProject_ReturnsCorrectInfo() {
        // Arrange - Create a temporary SDK-style project file
        var tempPath = Path.GetTempFileName();
        File.Delete(tempPath);
        var projectPath = Path.ChangeExtension(tempPath, ".csproj");

        try {
            File.WriteAllText(projectPath, @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <OutputType>Exe</OutputType>
  </PropertyGroup>
</Project>");

            // Act
            var info = _buildService.GetProjectInfo(projectPath);

            // Assert
            Assert.True(info.IsSdkStyle);
            Assert.Equal("Microsoft.NET.Sdk", info.Sdk);
        } finally {
            if(File.Exists(projectPath)) {
                File.Delete(projectPath);
            }
        }
    }

    /// <summary>
    /// Tests that GetProjectDescription returns appropriate description.
    /// </summary>
    [Fact]
    public void GetProjectDescription_WithConsoleProject_ReturnsConsoleDescription() {
        // Arrange - Create a temporary console project file
        var tempPath = Path.GetTempFileName();
        File.Delete(tempPath);
        var projectPath = Path.ChangeExtension(tempPath, ".csproj");

        try {
            File.WriteAllText(projectPath, @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <OutputType>Exe</OutputType>
  </PropertyGroup>
</Project>");

            // Act
            var description = _buildService.GetProjectDescription(projectPath);

            // Assert
            Assert.NotNull(description);
            Assert.NotEmpty(description);
        } finally {
            if(File.Exists(projectPath)) {
                File.Delete(projectPath);
            }
        }
    }
}
