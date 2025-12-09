using FTPSheep.Core.Models;

namespace FTPSheep.Tests.Models;

public class BuildConfigurationTests {
    [Fact]
    public void Constructor_Default_SetsConfigurationToRelease() {
        // Act
        var config = new BuildConfiguration();

        // Assert
        Assert.Equal("Release", config.Configuration);
        Assert.Null(config.TargetFramework);
        Assert.Null(config.RuntimeIdentifier);
        Assert.Null(config.SelfContained);
        Assert.NotNull(config.AdditionalProperties);
        Assert.Empty(config.AdditionalProperties);
    }

    [Fact]
    public void Constructor_WithConfiguration_SetsConfiguration() {
        // Act
        var config = new BuildConfiguration("Debug");

        // Assert
        Assert.Equal("Debug", config.Configuration);
    }

    [Fact]
    public void Constructor_WithNullConfiguration_SetsToRelease() {
        // Act
        var config = new BuildConfiguration(null!);

        // Assert
        Assert.Equal("Release", config.Configuration);
    }

    [Fact]
    public void Validate_ValidConfiguration_ReturnsTrue() {
        // Arrange
        var config = new BuildConfiguration {
            Configuration = "Release",
            TargetFramework = "net8.0",
            RuntimeIdentifier = "win-x64"
        };

        // Act
        var result = config.Validate(out var errors);

        // Assert
        Assert.True(result);
        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_EmptyConfiguration_ReturnsFalse() {
        // Arrange
        var config = new BuildConfiguration { Configuration = "" };

        // Act
        var result = config.Validate(out var errors);

        // Assert
        Assert.False(result);
        Assert.Contains(errors, e => e.Contains("Configuration name cannot be empty"));
    }

    [Fact]
    public void Validate_InvalidRuntimeIdentifier_ReturnsFalse() {
        // Arrange
        var config = new BuildConfiguration {
            Configuration = "Release",
            RuntimeIdentifier = "invalid"
        };

        // Act
        var result = config.Validate(out var errors);

        // Assert
        Assert.False(result);
        Assert.Contains(errors, e => e.Contains("Runtime identifier") && e.Contains("invalid"));
    }

    [Fact]
    public void Validate_ValidRuntimeIdentifier_ReturnsTrue() {
        // Arrange
        var config = new BuildConfiguration {
            Configuration = "Release",
            RuntimeIdentifier = "win-x64"
        };

        // Act
        var result = config.Validate(out var errors);

        // Assert
        Assert.True(result);
        Assert.Empty(errors);
    }

    [Fact]
    public void AdditionalProperties_CanAddAndRetrieve() {
        // Arrange
        var config = new BuildConfiguration();

        // Act
        config.AdditionalProperties["PublishTrimmed"] = "true";
        config.AdditionalProperties["PublishSingleFile"] = "true";

        // Assert
        Assert.Equal(2, config.AdditionalProperties.Count);
        Assert.Equal("true", config.AdditionalProperties["PublishTrimmed"]);
        Assert.Equal("true", config.AdditionalProperties["PublishSingleFile"]);
    }
}
