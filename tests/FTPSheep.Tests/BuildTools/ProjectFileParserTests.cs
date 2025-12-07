using FTPSheep.BuildTools.Models;
using FTPSheep.BuildTools.Services;
using FTPSheep.BuildTools.Exceptions;

namespace FTPSheep.Tests.BuildTools;

public class ProjectFileParserTests : IDisposable
{
    private readonly string _tempDir;
    private readonly ProjectFileParser _parser;

    public ProjectFileParserTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "FTPSheepTests_" + Guid.NewGuid());
        Directory.CreateDirectory(_tempDir);
        _parser = new ProjectFileParser();
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, true);
        }
    }

    [Fact]
    public void ParseProject_WithNullPath_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _parser.ParseProject(null!));
    }

    [Fact]
    public void ParseProject_WithEmptyPath_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _parser.ParseProject(string.Empty));
    }

    [Fact]
    public void ParseProject_WithNonExistentFile_ShouldThrowFileNotFoundException()
    {
        // Arrange
        var nonExistentPath = Path.Combine(_tempDir, "NonExistent.csproj");

        // Act & Assert
        Assert.Throws<FileNotFoundException>(() => _parser.ParseProject(nonExistentPath));
    }

    [Fact]
    public void ParseProject_WithInvalidXml_ShouldThrowProjectParseException()
    {
        // Arrange
        var projectPath = Path.Combine(_tempDir, "Invalid.csproj");
        File.WriteAllText(projectPath, "This is not valid XML");

        // Act & Assert
        Assert.Throws<ProjectParseException>(() => _parser.ParseProject(projectPath));
    }

    [Fact]
    public void ParseProject_WithSdkStyleWebProject_ShouldParseCorrectly()
    {
        // Arrange
        var projectPath = Path.Combine(_tempDir, "WebApp.csproj");
        File.WriteAllText(projectPath, @"
<Project Sdk=""Microsoft.NET.Sdk.Web"">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <OutputType>Exe</OutputType>
  </PropertyGroup>
</Project>");

        // Act
        var result = _parser.ParseProject(projectPath);

        // Assert
        Assert.Equal("Microsoft.NET.Sdk.Web", result.Sdk);
        Assert.True(result.IsSdkStyle);
        Assert.Equal(ProjectFormat.SdkStyle, result.Format);
        Assert.Single(result.TargetFrameworks);
        Assert.Equal("net8.0", result.PrimaryTargetFramework);
        Assert.Equal("Exe", result.OutputType);
        Assert.Equal(ProjectType.AspNetCore, result.ProjectType);
        Assert.Equal(".csproj", result.FileExtension);
    }

    [Fact]
    public void ParseProject_WithMultiTargeting_ShouldParseAllFrameworks()
    {
        // Arrange
        var projectPath = Path.Combine(_tempDir, "Library.csproj");
        File.WriteAllText(projectPath, @"
<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFrameworks>net6.0;net7.0;net8.0</TargetFrameworks>
    <OutputType>Library</OutputType>
  </PropertyGroup>
</Project>");

        // Act
        var result = _parser.ParseProject(projectPath);

        // Assert
        Assert.Equal(3, result.TargetFrameworks.Count);
        Assert.Contains("net6.0", result.TargetFrameworks);
        Assert.Contains("net7.0", result.TargetFrameworks);
        Assert.Contains("net8.0", result.TargetFrameworks);
        Assert.Equal("net6.0", result.PrimaryTargetFramework);
        Assert.True(result.IsMultiTargeting);
    }

    [Fact]
    public void ParseProject_WithLegacyFrameworkProject_ShouldParseLegacyFormat()
    {
        // Arrange
        var projectPath = Path.Combine(_tempDir, "Legacy.csproj");
        File.WriteAllText(projectPath, @"
<Project ToolsVersion=""15.0"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
  <PropertyGroup>
    <TargetFrameworkVersion>v4.7.2</TargetFrameworkVersion>
    <OutputType>Library</OutputType>
  </PropertyGroup>
</Project>");

        // Act
        var result = _parser.ParseProject(projectPath);

        // Assert
        Assert.Null(result.Sdk);
        Assert.False(result.IsSdkStyle);
        Assert.Equal(ProjectFormat.LegacyFramework, result.Format);
        Assert.Single(result.TargetFrameworks);
        Assert.Equal("net472", result.PrimaryTargetFramework);
        Assert.Equal("Library", result.OutputType);
    }

    [Fact]
    public void ParseProject_WithBlazorWebAssembly_ShouldDetectBlazor()
    {
        // Arrange
        var projectPath = Path.Combine(_tempDir, "BlazorApp.csproj");
        File.WriteAllText(projectPath, @"
<Project Sdk=""Microsoft.NET.Sdk.Web"">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include=""Microsoft.AspNetCore.Components.WebAssembly"" Version=""8.0.0"" />
  </ItemGroup>
</Project>");

        // Act
        var result = _parser.ParseProject(projectPath);

        // Assert
        Assert.Equal(ProjectType.Blazor, result.ProjectType);
    }

    [Fact]
    public void ParseProject_WithWorkerService_ShouldDetectWorkerService()
    {
        // Arrange
        var projectPath = Path.Combine(_tempDir, "Worker.csproj");
        File.WriteAllText(projectPath, @"
<Project Sdk=""Microsoft.NET.Sdk.Worker"">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
  </PropertyGroup>
</Project>");

        // Act
        var result = _parser.ParseProject(projectPath);

        // Assert
        Assert.Equal(ProjectType.WorkerService, result.ProjectType);
    }

    [Fact]
    public void ParseProject_WithConsoleApp_ShouldDetectConsole()
    {
        // Arrange
        var projectPath = Path.Combine(_tempDir, "Console.csproj");
        File.WriteAllText(projectPath, @"
<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <OutputType>Exe</OutputType>
  </PropertyGroup>
</Project>");

        // Act
        var result = _parser.ParseProject(projectPath);

        // Assert
        Assert.Equal(ProjectType.Console, result.ProjectType);
    }

    [Fact]
    public void ParseProject_WithClassLibrary_ShouldDetectLibrary()
    {
        // Arrange
        var projectPath = Path.Combine(_tempDir, "Library.csproj");
        File.WriteAllText(projectPath, @"
<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <OutputType>Library</OutputType>
  </PropertyGroup>
</Project>");

        // Act
        var result = _parser.ParseProject(projectPath);

        // Assert
        Assert.Equal(ProjectType.Library, result.ProjectType);
    }

    [Fact]
    public void ParseProject_WithWindowsApp_ShouldDetectWindowsApp()
    {
        // Arrange
        var projectPath = Path.Combine(_tempDir, "WinApp.csproj");
        File.WriteAllText(projectPath, @"
<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net8.0-windows</TargetFramework>
    <OutputType>WinExe</OutputType>
  </PropertyGroup>
</Project>");

        // Act
        var result = _parser.ParseProject(projectPath);

        // Assert
        Assert.Equal(ProjectType.WindowsApp, result.ProjectType);
    }

    [Fact]
    public void ParseProject_WithLegacyAspNetMvc_ShouldDetectMvc()
    {
        // Arrange
        var projectPath = Path.Combine(_tempDir, "MvcApp.csproj");
        File.WriteAllText(projectPath, @"
<Project ToolsVersion=""15.0"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
  <PropertyGroup>
    <TargetFrameworkVersion>v4.7.2</TargetFrameworkVersion>
    <ProjectTypeGuids>{E3E379DF-F4C6-4180-9B81-6769533ABE47};{349C5851-65DF-11DA-9384-00065B846F21};{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}</ProjectTypeGuids>
  </PropertyGroup>
</Project>");

        // Act
        var result = _parser.ParseProject(projectPath);

        // Assert
        Assert.Equal(ProjectType.AspNetMvc, result.ProjectType);
    }

    [Fact]
    public async Task ParseProjectAsync_WithValidProject_ShouldParseCorrectly()
    {
        // Arrange
        var projectPath = Path.Combine(_tempDir, "AsyncTest.csproj");
        File.WriteAllText(projectPath, @"
<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
  </PropertyGroup>
</Project>");

        // Act
        var result = await _parser.ParseProjectAsync(projectPath);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("net8.0", result.PrimaryTargetFramework);
    }

    [Fact]
    public void ParseProject_WithVbProj_ShouldRecognizeExtension()
    {
        // Arrange
        var projectPath = Path.Combine(_tempDir, "VbApp.vbproj");
        File.WriteAllText(projectPath, @"
<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
  </PropertyGroup>
</Project>");

        // Act
        var result = _parser.ParseProject(projectPath);

        // Assert
        Assert.Equal(".vbproj", result.FileExtension);
    }

    [Fact]
    public void ParseProject_WithFsProj_ShouldRecognizeExtension()
    {
        // Arrange
        var projectPath = Path.Combine(_tempDir, "FsApp.fsproj");
        File.WriteAllText(projectPath, @"
<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
  </PropertyGroup>
</Project>");

        // Act
        var result = _parser.ParseProject(projectPath);

        // Assert
        Assert.Equal(".fsproj", result.FileExtension);
    }
}
