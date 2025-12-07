using FTPSheep.BuildTools.Services;
using FTPSheep.BuildTools.Exceptions;

namespace FTPSheep.Tests.BuildTools;

public class BuildToolLocatorTests
{
    private readonly BuildToolLocator _locator;

    public BuildToolLocatorTests()
    {
        _locator = new BuildToolLocator();
    }

    [Fact]
    public void IsDotnetCliAvailable_ShouldReturnBoolean()
    {
        // Act
        var result = _locator.IsDotnetCliAvailable();

        // Assert - Just verify it returns a boolean without throwing
        Assert.True(result is true or false);
    }

    [Fact]
    public void IsMSBuildAvailable_ShouldReturnBoolean()
    {
        // Act
        var result = _locator.IsMSBuildAvailable();

        // Assert - Just verify it returns a boolean without throwing
        Assert.True(result is true or false);
    }

    [Fact]
    public void LocateDotnetCli_WhenAvailable_ShouldReturnValidPath()
    {
        // Arrange & Act
        if (!_locator.IsDotnetCliAvailable())
        {
            // Skip test if dotnet CLI is not available
            return;
        }

        var dotnetPath = _locator.LocateDotnetCli();

        // Assert
        Assert.NotNull(dotnetPath);
        Assert.NotEmpty(dotnetPath);
        Assert.True(File.Exists(dotnetPath), $"Dotnet CLI path does not exist: {dotnetPath}");
        Assert.EndsWith("dotnet.exe", dotnetPath, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void LocateDotnetCli_WhenNotAvailable_ShouldThrowToolNotFoundException()
    {
        // Arrange & Act
        if (_locator.IsDotnetCliAvailable())
        {
            // Skip test if dotnet CLI is available (can't test the negative case)
            return;
        }

        // Act & Assert
        var exception = Assert.Throws<ToolNotFoundException>(() => _locator.LocateDotnetCli());
        Assert.Contains("dotnet", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void LocateMSBuild_WhenAvailable_ShouldReturnValidPath()
    {
        // Arrange & Act
        if (!_locator.IsMSBuildAvailable())
        {
            // Skip test if MSBuild is not available
            return;
        }

        var msbuildPath = _locator.LocateMSBuild();

        // Assert
        Assert.NotNull(msbuildPath);
        Assert.NotEmpty(msbuildPath);
        Assert.True(File.Exists(msbuildPath), $"MSBuild path does not exist: {msbuildPath}");
        Assert.EndsWith("MSBuild.exe", msbuildPath, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void LocateMSBuild_WhenNotAvailable_ShouldThrowToolNotFoundException()
    {
        // Arrange & Act
        if (_locator.IsMSBuildAvailable())
        {
            // Skip test if MSBuild is available (can't test the negative case)
            return;
        }

        // Act & Assert
        var exception = Assert.Throws<ToolNotFoundException>(() => _locator.LocateMSBuild());
        Assert.Contains("MSBuild", exception.Message);
    }

    [Fact]
    public void GetDotnetCliVersion_WhenAvailable_ShouldReturnVersion()
    {
        // Arrange & Act
        if (!_locator.IsDotnetCliAvailable())
        {
            // Skip test if dotnet CLI is not available
            return;
        }

        var version = _locator.GetDotnetCliVersion();

        // Assert
        Assert.NotNull(version);
        Assert.NotEmpty(version);
        // Version should contain numbers (like "8.0.100" or "7.0.405")
        Assert.Matches(@"\d+\.\d+", version);
    }

    [Fact]
    public void GetDotnetCliVersion_WhenNotAvailable_ShouldReturnNull()
    {
        // Arrange & Act
        if (_locator.IsDotnetCliAvailable())
        {
            // Skip test if dotnet CLI is available (can't test the negative case)
            return;
        }

        var version = _locator.GetDotnetCliVersion();

        // Assert
        Assert.Null(version);
    }

    [Fact]
    public void IsDotnetCliAvailable_WhenDotnetExists_ShouldMatchLocateDotnetCli()
    {
        // Act
        var isAvailable = _locator.IsDotnetCliAvailable();

        // Assert
        if (isAvailable)
        {
            // Should not throw
            var path = _locator.LocateDotnetCli();
            Assert.NotNull(path);
        }
        else
        {
            // Should throw
            Assert.Throws<ToolNotFoundException>(() => _locator.LocateDotnetCli());
        }
    }

    [Fact]
    public void IsMSBuildAvailable_WhenMSBuildExists_ShouldMatchLocateMSBuild()
    {
        // Act
        var isAvailable = _locator.IsMSBuildAvailable();

        // Assert
        if (isAvailable)
        {
            // Should not throw
            var path = _locator.LocateMSBuild();
            Assert.NotNull(path);
        }
        else
        {
            // Should throw
            Assert.Throws<ToolNotFoundException>(() => _locator.LocateMSBuild());
        }
    }
}
