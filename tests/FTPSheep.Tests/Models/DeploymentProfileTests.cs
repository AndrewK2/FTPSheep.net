using System.Text.Json;
using FTPSheep.Core.Models;

namespace FTPSheep.Tests.Models;

public class DeploymentProfileTests {
    [Fact]
    public void Constructor_Default_SetsDefaultValues() {
        // Act
        var profile = new DeploymentProfile();

        // Assert
        Assert.Equal(string.Empty, profile.Name);
        Assert.NotNull(profile.Connection);
        Assert.NotNull(profile.Build);
        Assert.Equal("/", profile.RemotePath);
        Assert.Equal(4, profile.Concurrency);
        Assert.Equal(3, profile.RetryCount);
        Assert.Equal(CleanupMode.None, profile.CleanupMode);
        Assert.True(profile.AppOfflineEnabled);
    }

    [Fact]
    public void ObsoleteProperty_Server_DelegatesToConnectionHost() {
        // Arrange
        var profile = new DeploymentProfile();

        // Act
#pragma warning disable CS0618 // Type or member is obsolete
        profile.Server = "ftp.example.com";
#pragma warning restore CS0618

        // Assert
        Assert.Equal("ftp.example.com", profile.Connection.Host);
#pragma warning disable CS0618
        Assert.Equal("ftp.example.com", profile.Server);
#pragma warning restore CS0618
    }

    [Fact]
    public void ObsoleteProperty_Port_DelegatesToConnectionPort() {
        // Arrange
        var profile = new DeploymentProfile();

        // Act
#pragma warning disable CS0618
        profile.Port = 2121;
#pragma warning restore CS0618

        // Assert
        Assert.Equal(2121, profile.Connection.Port);
#pragma warning disable CS0618
        Assert.Equal(2121, profile.Port);
#pragma warning restore CS0618
    }

    [Fact]
    public void ObsoleteProperty_Protocol_DelegatesToConnectionProtocol() {
        // Arrange
        var profile = new DeploymentProfile();

        // Act
#pragma warning disable CS0618
        profile.Protocol = ProtocolType.Sftp;
#pragma warning restore CS0618

        // Assert
        Assert.Equal(ProtocolType.Sftp, profile.Connection.Protocol);
#pragma warning disable CS0618
        Assert.Equal(ProtocolType.Sftp, profile.Protocol);
#pragma warning restore CS0618
    }

    [Fact]
    public void ObsoleteProperty_TimeoutSeconds_DelegatesToConnectionTimeoutSeconds() {
        // Arrange
        var profile = new DeploymentProfile();

        // Act
#pragma warning disable CS0618
        profile.TimeoutSeconds = 60;
#pragma warning restore CS0618

        // Assert
        Assert.Equal(60, profile.Connection.TimeoutSeconds);
#pragma warning disable CS0618
        Assert.Equal(60, profile.TimeoutSeconds);
#pragma warning restore CS0618
    }

    [Fact]
    public void ObsoleteProperty_BuildConfiguration_DelegatesToBuildConfiguration() {
        // Arrange
        var profile = new DeploymentProfile();

        // Act
#pragma warning disable CS0618
        profile.BuildConfiguration = "Debug";
#pragma warning restore CS0618

        // Assert
        Assert.Equal("Debug", profile.Build.Configuration);
#pragma warning disable CS0618
        Assert.Equal("Debug", profile.BuildConfiguration);
#pragma warning restore CS0618
    }

    [Fact]
    public void ObsoleteProperty_TargetFramework_DelegatesToBuildTargetFramework() {
        // Arrange
        var profile = new DeploymentProfile();

        // Act
#pragma warning disable CS0618
        profile.TargetFramework = "net8.0";
#pragma warning restore CS0618

        // Assert
        Assert.Equal("net8.0", profile.Build.TargetFramework);
#pragma warning disable CS0618
        Assert.Equal("net8.0", profile.TargetFramework);
#pragma warning restore CS0618
    }

    [Fact]
    public void ObsoleteProperty_RuntimeIdentifier_DelegatesToBuildRuntimeIdentifier() {
        // Arrange
        var profile = new DeploymentProfile();

        // Act
#pragma warning disable CS0618
        profile.RuntimeIdentifier = "win-x64";
#pragma warning restore CS0618

        // Assert
        Assert.Equal("win-x64", profile.Build.RuntimeIdentifier);
#pragma warning disable CS0618
        Assert.Equal("win-x64", profile.RuntimeIdentifier);
#pragma warning restore CS0618
    }

    [Fact]
    public void Serialization_NewFormat_SerializesAndDeserializesCorrectly() {
        // Arrange
        var profile = new DeploymentProfile {
            Name = "Test Profile",
            Connection = new ServerConnection {
                Host = "ftp.example.com",
                Port = 21,
                Protocol = ProtocolType.Ftp,
                TimeoutSeconds = 30
            },
            Build = new BuildConfiguration {
                Configuration = "Release",
                TargetFramework = "net8.0"
            },
            Username = "testuser",
            RemotePath = "/www"
        };

        // Act
        var json = JsonSerializer.Serialize(profile, new JsonSerializerOptions { WriteIndented = true });
        var deserialized = JsonSerializer.Deserialize<DeploymentProfile>(json);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal(profile.Name, deserialized.Name);
        Assert.Equal(profile.Connection.Host, deserialized.Connection.Host);
        Assert.Equal(profile.Build.Configuration, deserialized.Build.Configuration);
        Assert.Equal(profile.Build.TargetFramework, deserialized.Build.TargetFramework);
    }

    // Note: Old format deserialization would require custom JSON converters.
    // The obsolete properties work for programmatic access (tested above),
    // but JSON deserialization focuses on the new nested format.

    [Fact]
    public void NewProperties_DirectAccess_WorksCorrectly() {
        // Arrange
        var profile = new DeploymentProfile();

        // Act
        profile.Connection.Host = "ftp.example.com";
        profile.Connection.Port = 2121;
        profile.Build.Configuration = "Debug";
        profile.Build.TargetFramework = "net8.0";

        // Assert
        Assert.Equal("ftp.example.com", profile.Connection.Host);
        Assert.Equal(2121, profile.Connection.Port);
        Assert.Equal("Debug", profile.Build.Configuration);
        Assert.Equal("net8.0", profile.Build.TargetFramework);
    }
}
