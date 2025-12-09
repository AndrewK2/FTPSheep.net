using FTPSheep.BuildTools.Models;
using FTPSheep.BuildTools.Services;

namespace FTPSheep.Tests.BuildTools;

public class MsBuildWrapperTests {
    private readonly MsBuildWrapper wrapper;

    public MsBuildWrapperTests() {
        wrapper = new MsBuildWrapper();
    }

    [Fact]
    public void BuildArguments_WithMinimalOptions_GeneratesCorrectCommand() {
        // Arrange
        var options = new MsBuildOptions {
            ProjectPath = @"C:\Projects\MyApp\MyApp.csproj",
            Configuration = "Release"
        };

        // Act
        var args = wrapper.BuildArguments(options);

        // Assert
        Assert.Contains(@"""C:\Projects\MyApp\MyApp.csproj""", args);
        Assert.Contains("/t:Build", args);
        Assert.Contains("/p:Configuration=Release", args);
        Assert.Contains("/v:minimal", args);
        Assert.Contains("/m", args);
        Assert.Contains("/restore", args);
        Assert.Contains("/nologo", args);
        Assert.Contains("/consoleloggerparameters:NoSummary", args);
    }

    [Fact]
    public void BuildArguments_WithPlatform_IncludesPlatformProperty() {
        // Arrange
        var options = new MsBuildOptions {
            ProjectPath = @"C:\Projects\MyApp\MyApp.csproj",
            Configuration = "Release",
            Platform = "Any CPU"
        };

        // Act
        var args = wrapper.BuildArguments(options);

        // Assert
        Assert.Contains(@"/p:Platform=""Any CPU""", args);
    }

    [Fact]
    public void BuildArguments_WithOutputPath_IncludesOutputPathProperty() {
        // Arrange
        var options = new MsBuildOptions {
            ProjectPath = @"C:\Projects\MyApp\MyApp.csproj",
            Configuration = "Release",
            OutputPath = @"C:\Output\MyApp"
        };

        // Act
        var args = wrapper.BuildArguments(options);

        // Assert
        Assert.Contains(@"/p:OutputPath=""C:\Output\MyApp""", args);
    }

    [Fact]
    public void BuildArguments_WithTargetFramework_IncludesTargetFrameworkProperty() {
        // Arrange
        var options = new MsBuildOptions {
            ProjectPath = @"C:\Projects\MyApp\MyApp.csproj",
            Configuration = "Release",
            TargetFramework = "net472"
        };

        // Act
        var args = wrapper.BuildArguments(options);

        // Assert
        Assert.Contains("/p:TargetFramework=net472", args);
    }

    [Fact]
    public void BuildArguments_WithPublishProfile_IncludesPublishProfileProperty() {
        // Arrange
        var options = new MsBuildOptions {
            ProjectPath = @"C:\Projects\MyApp\MyApp.csproj",
            Configuration = "Release",
            PublishProfile = "FolderProfile"
        };

        // Act
        var args = wrapper.BuildArguments(options);

        // Assert
        Assert.Contains("/p:PublishProfile=FolderProfile", args);
    }

    [Fact]
    public void BuildArguments_WithCustomProperties_IncludesAllProperties() {
        // Arrange
        var options = new MsBuildOptions {
            ProjectPath = @"C:\Projects\MyApp\MyApp.csproj",
            Configuration = "Release",
            Properties = new Dictionary<string, string>
            {
                { "DeployOnBuild", "true" },
                { "PublishUrl", @"C:\Deploy" },
                { "WebPublishMethod", "FileSystem" }
            }
        };

        // Act
        var args = wrapper.BuildArguments(options);

        // Assert
        Assert.Contains("/p:DeployOnBuild=true", args);
        Assert.Contains("/p:PublishUrl=", args);
        Assert.Contains(@"C:\Deploy", args);
        Assert.Contains("/p:WebPublishMethod=FileSystem", args);
    }

    [Fact]
    public void BuildArguments_WithMultipleTargets_IncludesAllTargets() {
        // Arrange
        var options = new MsBuildOptions {
            ProjectPath = @"C:\Projects\MyApp\MyApp.csproj",
            Configuration = "Release",
            Targets = ["Clean", "Build", "Publish"]
        };

        // Act
        var args = wrapper.BuildArguments(options);

        // Assert
        Assert.Contains("/t:Clean;Build;Publish", args);
    }

    [Theory]
    [InlineData(MsBuildVerbosity.Quiet, "/v:quiet")]
    [InlineData(MsBuildVerbosity.Minimal, "/v:minimal")]
    [InlineData(MsBuildVerbosity.Normal, "/v:normal")]
    [InlineData(MsBuildVerbosity.Detailed, "/v:detailed")]
    [InlineData(MsBuildVerbosity.Diagnostic, "/v:diagnostic")]
    public void BuildArguments_WithVerbosity_IncludesCorrectVerbosityFlag(MsBuildVerbosity verbosity, string expectedFlag) {
        // Arrange
        var options = new MsBuildOptions {
            ProjectPath = @"C:\Projects\MyApp\MyApp.csproj",
            Configuration = "Release",
            Verbosity = verbosity
        };

        // Act
        var args = wrapper.BuildArguments(options);

        // Assert
        Assert.Contains(expectedFlag, args);
    }

    [Fact]
    public void BuildArguments_WithMaxCpuCount_IncludesMaxCpuCountFlag() {
        // Arrange
        var options = new MsBuildOptions {
            ProjectPath = @"C:\Projects\MyApp\MyApp.csproj",
            Configuration = "Release",
            MaxCpuCount = 4
        };

        // Act
        var args = wrapper.BuildArguments(options);

        // Assert
        Assert.Contains("/m:4", args);
    }

    [Fact]
    public void BuildArguments_WithoutMaxCpuCount_UsesParallelBuildByDefault() {
        // Arrange
        var options = new MsBuildOptions {
            ProjectPath = @"C:\Projects\MyApp\MyApp.csproj",
            Configuration = "Release"
        };

        // Act
        var args = wrapper.BuildArguments(options);

        // Assert
        Assert.Contains("/m", args);
        Assert.DoesNotContain("/m:", args);
    }

    [Fact]
    public void BuildArguments_WithRestorePackagesFalse_DoesNotIncludeRestoreFlag() {
        // Arrange
        var options = new MsBuildOptions {
            ProjectPath = @"C:\Projects\MyApp\MyApp.csproj",
            Configuration = "Release",
            RestorePackages = false
        };

        // Act
        var args = wrapper.BuildArguments(options);

        // Assert
        Assert.DoesNotContain("/restore", args);
    }

    [Fact]
    public void BuildArguments_WithTreatWarningsAsErrors_IncludesTreatWarningsAsErrorsProperty() {
        // Arrange
        var options = new MsBuildOptions {
            ProjectPath = @"C:\Projects\MyApp\MyApp.csproj",
            Configuration = "Release",
            TreatWarningsAsErrors = true
        };

        // Act
        var args = wrapper.BuildArguments(options);

        // Assert
        Assert.Contains("/p:TreatWarningsAsErrors=true", args);
    }

    [Fact]
    public void BuildArguments_WithNullOptions_ThrowsArgumentNullException() {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => wrapper.BuildArguments(null!));
    }

    [Fact]
    public void BuildArguments_WithEmptyProjectPath_ThrowsArgumentException() {
        // Arrange
        var options = new MsBuildOptions {
            ProjectPath = "",
            Configuration = "Release"
        };

        // Act & Assert
        Assert.Throws<ArgumentException>(() => wrapper.BuildArguments(options));
    }

    [Fact]
    public void CreateBuildOptions_GeneratesCorrectOptions() {
        // Act
        var options = wrapper.CreateBuildOptions(@"C:\Projects\MyApp\MyApp.csproj", "Debug");

        // Assert
        Assert.Equal(@"C:\Projects\MyApp\MyApp.csproj", options.ProjectPath);
        Assert.Equal("Debug", options.Configuration);
        Assert.Contains("Build", options.Targets);
        Assert.True(options.RestorePackages);
    }

    [Fact]
    public void CreatePublishOptions_GeneratesCorrectOptions() {
        // Act
        var options = wrapper.CreatePublishOptions(
            @"C:\Projects\MyApp\MyApp.csproj",
            @"C:\Output",
            "Release");

        // Assert
        Assert.Equal(@"C:\Projects\MyApp\MyApp.csproj", options.ProjectPath);
        Assert.Equal("Release", options.Configuration);
        Assert.Equal(@"C:\Output", options.OutputPath);
        Assert.Contains("Build", options.Targets);
        Assert.Contains("Publish", options.Targets);
        Assert.True(options.RestorePackages);
        Assert.Equal("true", options.Properties["DeployOnBuild"]);
        Assert.Equal(@"C:\Output", options.Properties["PublishUrl"]);
        Assert.Equal("FileSystem", options.Properties["WebPublishMethod"]);
    }

    [Fact]
    public void CreateCleanOptions_GeneratesCorrectOptions() {
        // Act
        var options = wrapper.CreateCleanOptions(@"C:\Projects\MyApp\MyApp.csproj", "Release");

        // Assert
        Assert.Equal(@"C:\Projects\MyApp\MyApp.csproj", options.ProjectPath);
        Assert.Equal("Release", options.Configuration);
        Assert.Contains("Clean", options.Targets);
        Assert.False(options.RestorePackages);
    }
}
