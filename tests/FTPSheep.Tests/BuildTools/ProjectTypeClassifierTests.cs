using FTPSheep.BuildTools.Models;
using FTPSheep.BuildTools.Services;

namespace FTPSheep.Tests.BuildTools;

public class ProjectTypeClassifierTests {
    private readonly ProjectTypeClassifier classifier;

    public ProjectTypeClassifierTests() {
        classifier = new ProjectTypeClassifier();
    }

    [Fact]
    public void IsDotNetFramework_WithFramework472_ShouldReturnTrue() {
        // Arrange
        var projectInfo = new ProjectInfo {
            TargetFrameworks = new List<string> { "net472" }
        };

        // Act
        var result = classifier.IsDotNetFramework(projectInfo);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsDotNetFramework_WithFramework48_ShouldReturnTrue() {
        // Arrange
        var projectInfo = new ProjectInfo {
            TargetFrameworks = new List<string> { "net48" }
        };

        // Act
        var result = classifier.IsDotNetFramework(projectInfo);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsDotNetFramework_WithNet60_ShouldReturnFalse() {
        // Arrange
        var projectInfo = new ProjectInfo {
            TargetFrameworks = new List<string> { "net6.0" }
        };

        // Act
        var result = classifier.IsDotNetFramework(projectInfo);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsDotNetCore_WithNetCoreApp31_ShouldReturnTrue() {
        // Arrange
        var projectInfo = new ProjectInfo {
            TargetFrameworks = new List<string> { "netcoreapp3.1" }
        };

        // Act
        var result = classifier.IsDotNetCore(projectInfo);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsDotNetCore_WithNetCoreApp21_ShouldReturnTrue() {
        // Arrange
        var projectInfo = new ProjectInfo {
            TargetFrameworks = new List<string> { "netcoreapp2.1" }
        };

        // Act
        var result = classifier.IsDotNetCore(projectInfo);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsDotNet5Plus_WithNet50_ShouldReturnTrue() {
        // Arrange
        var projectInfo = new ProjectInfo {
            TargetFrameworks = new List<string> { "net5.0" }
        };

        // Act
        var result = classifier.IsDotNet5Plus(projectInfo);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsDotNet5Plus_WithNet60_ShouldReturnTrue() {
        // Arrange
        var projectInfo = new ProjectInfo {
            TargetFrameworks = new List<string> { "net6.0" }
        };

        // Act
        var result = classifier.IsDotNet5Plus(projectInfo);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsDotNet5Plus_WithNet80_ShouldReturnTrue() {
        // Arrange
        var projectInfo = new ProjectInfo {
            TargetFrameworks = new List<string> { "net8.0" }
        };

        // Act
        var result = classifier.IsDotNet5Plus(projectInfo);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsDotNet5Plus_WithNetCoreApp31_ShouldReturnFalse() {
        // Arrange
        var projectInfo = new ProjectInfo {
            TargetFrameworks = new List<string> { "netcoreapp3.1" }
        };

        // Act
        var result = classifier.IsDotNet5Plus(projectInfo);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsDotNetStandard_WithNetStandard20_ShouldReturnTrue() {
        // Arrange
        var projectInfo = new ProjectInfo {
            TargetFrameworks = new List<string> { "netstandard2.0" }
        };

        // Act
        var result = classifier.IsDotNetStandard(projectInfo);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsAspNet_WithAspNetCore_ShouldReturnTrue() {
        // Arrange
        var projectInfo = new ProjectInfo {
            ProjectType = ProjectType.AspNetCore
        };

        // Act
        var result = classifier.IsAspNet(projectInfo);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsAspNet_WithBlazor_ShouldReturnTrue() {
        // Arrange
        var projectInfo = new ProjectInfo {
            ProjectType = ProjectType.Blazor
        };

        // Act
        var result = classifier.IsAspNet(projectInfo);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsAspNet_WithRazorPages_ShouldReturnTrue() {
        // Arrange
        var projectInfo = new ProjectInfo {
            ProjectType = ProjectType.RazorPages
        };

        // Act
        var result = classifier.IsAspNet(projectInfo);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsAspNet_WithConsole_ShouldReturnFalse() {
        // Arrange
        var projectInfo = new ProjectInfo {
            ProjectType = ProjectType.Console
        };

        // Act
        var result = classifier.IsAspNet(projectInfo);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsAspNetCore_WithAspNetCore_ShouldReturnTrue() {
        // Arrange
        var projectInfo = new ProjectInfo {
            ProjectType = ProjectType.AspNetCore
        };

        // Act
        var result = classifier.IsAspNetCore(projectInfo);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsAspNetCore_WithLegacyAspNetMvc_ShouldReturnFalse() {
        // Arrange
        var projectInfo = new ProjectInfo {
            ProjectType = ProjectType.AspNetMvc
        };

        // Act
        var result = classifier.IsAspNetCore(projectInfo);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsWebApplication_WithAspNetCore_ShouldReturnTrue() {
        // Arrange
        var projectInfo = new ProjectInfo {
            ProjectType = ProjectType.AspNetCore
        };

        // Act
        var result = classifier.IsWebApplication(projectInfo);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsWebApplication_WithWorkerService_ShouldReturnTrue() {
        // Arrange
        var projectInfo = new ProjectInfo {
            ProjectType = ProjectType.WorkerService
        };

        // Act
        var result = classifier.IsWebApplication(projectInfo);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void GetProjectDescription_WithAspNetCore_ShouldReturnCorrectDescription() {
        // Arrange
        var projectInfo = new ProjectInfo {
            ProjectType = ProjectType.AspNetCore,
            TargetFrameworks = new List<string> { "net8.0" }
        };

        // Act
        var description = classifier.GetProjectDescription(projectInfo);

        // Assert
        Assert.Contains("ASP.NET Core Web Application", description);
        Assert.Contains("net8.0", description);
    }

    [Fact]
    public void GetProjectDescription_WithMultiTargeting_ShouldIncludeAllFrameworks() {
        // Arrange
        var projectInfo = new ProjectInfo {
            ProjectType = ProjectType.Library,
            TargetFrameworks = new List<string> { "net6.0", "net7.0", "net8.0" }
        };

        // Act
        var description = classifier.GetProjectDescription(projectInfo);

        // Assert
        Assert.Contains("multi-targeting", description);
        Assert.Contains("net6.0", description);
        Assert.Contains("net7.0", description);
        Assert.Contains("net8.0", description);
    }

    [Fact]
    public void GetRecommendedBuildTool_WithSdkStyleProject_ShouldReturnDotnetCli() {
        // Arrange
        var projectInfo = new ProjectInfo {
            Sdk = "Microsoft.NET.Sdk",
            Format = ProjectFormat.SdkStyle
        };

        // Act
        var buildTool = classifier.GetRecommendedBuildTool(projectInfo);

        // Assert
        Assert.Equal(BuildTool.DotnetCli, buildTool);
    }

    [Fact]
    public void GetRecommendedBuildTool_WithLegacyFrameworkProject_ShouldReturnMSBuild() {
        // Arrange
        var projectInfo = new ProjectInfo {
            Format = ProjectFormat.LegacyFramework,
            TargetFrameworks = new List<string> { "net472" }
        };

        // Act
        var buildTool = classifier.GetRecommendedBuildTool(projectInfo);

        // Assert
        Assert.Equal(BuildTool.MsBuild, buildTool);
    }

    [Fact]
    public void GetRecommendedBuildTool_WithNullProjectInfo_ShouldThrowArgumentNullException() {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => classifier.GetRecommendedBuildTool(null!));
    }

    [Fact]
    public void IsDotNetFramework_WithNullProjectInfo_ShouldThrowArgumentNullException() {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => classifier.IsDotNetFramework(null!));
    }

    [Fact]
    public void IsDotNetCore_WithNullProjectInfo_ShouldThrowArgumentNullException() {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => classifier.IsDotNetCore(null!));
    }

    [Fact]
    public void IsDotNet5Plus_WithNullProjectInfo_ShouldThrowArgumentNullException() {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => classifier.IsDotNet5Plus(null!));
    }
}
