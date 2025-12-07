using FTPSheep.Core.Exceptions;

namespace FTPSheep.Tests.Exceptions;

public class ExceptionTests
{
    #region BuildException Tests

    [Fact]
    public void BuildException_DefaultConstructor_ShouldCreateException()
    {
        // Act
        var exception = new BuildException();

        // Assert
        Assert.NotNull(exception);
    }

    [Fact]
    public void BuildException_WithMessage_ShouldSetMessage()
    {
        // Act
        var exception = new BuildException("Build failed");

        // Assert
        Assert.Equal("Build failed", exception.Message);
    }

    [Fact]
    public void BuildException_WithMessageAndInnerException_ShouldSetBoth()
    {
        // Arrange
        var innerException = new InvalidOperationException("Inner error");

        // Act
        var exception = new BuildException("Build failed", innerException);

        // Assert
        Assert.Equal("Build failed", exception.Message);
        Assert.Same(innerException, exception.InnerException);
    }

    [Fact]
    public void BuildException_WithProjectPathAndConfiguration_ShouldSetProperties()
    {
        // Act
        var exception = new BuildException("Build failed", "/path/to/project.csproj", "Release");

        // Assert
        Assert.Equal("Build failed", exception.Message);
        Assert.Equal("/path/to/project.csproj", exception.ProjectPath);
        Assert.Equal("Release", exception.BuildConfiguration);
    }

    [Fact]
    public void BuildCompilationException_WithBuildErrors_ShouldSetErrors()
    {
        // Arrange
        var errors = new[] { "Error CS1001", "Error CS1002" };

        // Act
        var exception = new BuildCompilationException(errors);

        // Assert
        Assert.Contains("Build compilation failed with 2 error(s)", exception.Message);
        Assert.Equal(errors, exception.BuildErrors);
    }

    [Fact]
    public void BuildToolNotFoundException_WithToolName_ShouldSetToolName()
    {
        // Act
        var exception = new BuildToolNotFoundException("MSBuild");

        // Assert
        Assert.Contains("MSBuild", exception.Message);
        Assert.Equal("MSBuild", exception.ToolName);
    }

    #endregion

    #region ConnectionException Tests

    [Fact]
    public void ConnectionException_DefaultConstructor_ShouldCreateException()
    {
        // Act
        var exception = new ConnectionException();

        // Assert
        Assert.NotNull(exception);
    }

    [Fact]
    public void ConnectionException_WithMessage_ShouldSetMessage()
    {
        // Act
        var exception = new ConnectionException("Connection failed");

        // Assert
        Assert.Equal("Connection failed", exception.Message);
    }

    [Fact]
    public void ConnectionException_WithHostAndPort_ShouldSetProperties()
    {
        // Act
        var exception = new ConnectionException("Connection failed", "server.com", 21, isTransient: true);

        // Assert
        Assert.Equal("server.com", exception.Host);
        Assert.Equal(21, exception.Port);
        Assert.True(exception.IsTransient);
    }

    [Fact]
    public void ConnectionTimeoutException_WithParameters_ShouldSetAllProperties()
    {
        // Act
        var exception = new ConnectionTimeoutException("server.com", 21, TimeSpan.FromSeconds(30));

        // Assert
        Assert.Contains("server.com", exception.Message);
        Assert.Contains("21", exception.Message);
        Assert.Contains("30", exception.Message);
        Assert.Equal("server.com", exception.Host);
        Assert.Equal(21, exception.Port);
        Assert.True(exception.IsTransient);
    }

    [Fact]
    public void ConnectionRefusedException_WithHostAndPort_ShouldSetProperties()
    {
        // Act
        var exception = new ConnectionRefusedException("ftp.example.com", 21);

        // Assert
        Assert.Contains("ftp.example.com", exception.Message);
        Assert.Contains("21", exception.Message);
        Assert.Equal("ftp.example.com", exception.Host);
        Assert.Equal(21, exception.Port);
        Assert.False(exception.IsTransient);
    }

    [Fact]
    public void SslCertificateException_WithHostAndReason_ShouldSetProperties()
    {
        // Act
        var exception = new SslCertificateException("secure.server.com", "Certificate expired");

        // Assert
        Assert.Contains("secure.server.com", exception.Message);
        Assert.Contains("Certificate expired", exception.Message);
        Assert.Equal("secure.server.com", exception.Host);
        Assert.False(exception.IsTransient);
    }

    #endregion

    #region AuthenticationException Tests

    [Fact]
    public void AuthenticationException_DefaultConstructor_ShouldCreateException()
    {
        // Act
        var exception = new AuthenticationException();

        // Assert
        Assert.NotNull(exception);
    }

    [Fact]
    public void AuthenticationException_WithMessage_ShouldSetMessage()
    {
        // Act
        var exception = new AuthenticationException("Auth failed");

        // Assert
        Assert.Equal("Auth failed", exception.Message);
    }

    [Fact]
    public void AuthenticationException_WithUserAndHost_ShouldSetProperties()
    {
        // Act
        var exception = new AuthenticationException("Auth failed", "testuser", "server.com");

        // Assert
        Assert.Equal("Auth failed", exception.Message);
        Assert.Equal("testuser", exception.Username);
        Assert.Equal("server.com", exception.Host);
    }

    [Fact]
    public void AuthenticationException_ShouldAllowSettingIsCredentialError()
    {
        // Act
        var exception = new AuthenticationException("Auth failed")
        {
            IsCredentialError = true
        };

        // Assert
        Assert.True(exception.IsCredentialError);
    }

    [Fact]
    public void InvalidCredentialsException_WithUserAndHost_ShouldSetProperties()
    {
        // Act
        var exception = new InvalidCredentialsException("testuser", "server.com");

        // Assert
        Assert.Contains("Authentication failed for user 'testuser' on server.com", exception.Message);
        Assert.Equal("testuser", exception.Username);
        Assert.Equal("server.com", exception.Host);
        Assert.True(exception.IsCredentialError);
    }

    [Fact]
    public void InsufficientPermissionsException_WithAllParameters_ShouldSetProperties()
    {
        // Act
        var exception = new InsufficientPermissionsException("testuser", "server.com", "WRITE");

        // Assert
        Assert.Contains("User 'testuser' on server.com does not have the required 'WRITE' permission", exception.Message);
        Assert.Equal("testuser", exception.Username);
        Assert.Equal("server.com", exception.Host);
        Assert.Equal("WRITE", exception.RequiredPermission);
    }

    #endregion

    #region DeploymentException Tests

    [Fact]
    public void DeploymentException_DefaultConstructor_ShouldCreateException()
    {
        // Act
        var exception = new DeploymentException();

        // Assert
        Assert.NotNull(exception);
    }

    [Fact]
    public void DeploymentException_WithMessage_ShouldSetMessage()
    {
        // Act
        var exception = new DeploymentException("Deploy failed");

        // Assert
        Assert.Equal("Deploy failed", exception.Message);
    }

    [Fact]
    public void DeploymentException_WithProfileAndPhase_ShouldSetProperties()
    {
        // Act
        var exception = new DeploymentException("Deploy failed", "profile1", DeploymentPhase.Upload, isRetryable: true);

        // Assert
        Assert.Equal("Deploy failed", exception.Message);
        Assert.Equal("profile1", exception.ProfileName);
        Assert.Equal(DeploymentPhase.Upload, exception.Phase);
        Assert.True(exception.IsRetryable);
    }

    [Fact]
    public void DeploymentException_WithInnerException_ShouldSetInnerException()
    {
        // Arrange
        var innerException = new InvalidOperationException("Inner error");

        // Act
        var exception = new DeploymentException("Deploy failed", "profile1", DeploymentPhase.Build, innerException, isRetryable: false);

        // Assert
        Assert.Equal("Deploy failed", exception.Message);
        Assert.Same(innerException, exception.InnerException);
        Assert.False(exception.IsRetryable);
    }

    [Fact]
    public void DeploymentPhase_ShouldHaveExpectedValues()
    {
        // Assert
        Assert.Equal(0, (int)DeploymentPhase.Unknown);
        Assert.Equal(1, (int)DeploymentPhase.Initialization);
        Assert.Equal(2, (int)DeploymentPhase.Build);
        Assert.Equal(3, (int)DeploymentPhase.Connection);
        Assert.Equal(4, (int)DeploymentPhase.Authentication);
        Assert.Equal(5, (int)DeploymentPhase.Upload);
        Assert.Equal(6, (int)DeploymentPhase.Verification);
        Assert.Equal(7, (int)DeploymentPhase.Cleanup);
    }

    [Fact]
    public void FileTransferException_WithPaths_ShouldSetProperties()
    {
        // Act
        var exception = new FileTransferException("/local/file.txt", "/remote/file.txt");

        // Assert
        Assert.Contains("/local/file.txt", exception.Message);
        Assert.Contains("/remote/file.txt", exception.Message);
        Assert.Equal("/local/file.txt", exception.FilePath);
        Assert.Equal("/remote/file.txt", exception.RemotePath);
        Assert.Equal(DeploymentPhase.Upload, exception.Phase);
        Assert.True(exception.IsRetryable);
    }

    [Fact]
    public void FileTransferException_WithInnerException_ShouldIncludeInnerMessage()
    {
        // Arrange
        var innerException = new System.IO.IOException("Disk full");

        // Act
        var exception = new FileTransferException("/local/file.txt", "/remote/file.txt", innerException);

        // Assert
        Assert.Contains("Disk full", exception.Message);
        Assert.Same(innerException, exception.InnerException);
        Assert.True(exception.IsRetryable);
    }

    [Fact]
    public void InsufficientDiskSpaceException_WithSpaceValues_ShouldSetProperties()
    {
        // Arrange
        var requiredBytes = 1000L * 1024 * 1024; // 1000 MB
        var availableBytes = 500L * 1024 * 1024;  // 500 MB

        // Act
        var exception = new InsufficientDiskSpaceException(requiredBytes, availableBytes);

        // Assert
        Assert.Contains("1000 MB", exception.Message);
        Assert.Contains("500 MB", exception.Message);
        Assert.Equal(requiredBytes, exception.RequiredBytes);
        Assert.Equal(availableBytes, exception.AvailableBytes);
        Assert.Equal(DeploymentPhase.Upload, exception.Phase);
        Assert.False(exception.IsRetryable);
    }

    #endregion

    #region ProfileException Tests

    [Fact]
    public void ProfileException_WithProfileName_ShouldSetProfileName()
    {
        // Act
        var exception = new ProfileException("Error", "profile1");

        // Assert
        Assert.Equal("Error", exception.Message);
        Assert.Equal("profile1", exception.ProfileName);
    }

    [Fact]
    public void ProfileNotFoundException_WithProfileName_ShouldIncludeNameInMessage()
    {
        // Act
        var exception = new ProfileNotFoundException("myprofile");

        // Assert
        Assert.Contains("myprofile", exception.Message);
        Assert.Equal("myprofile", exception.ProfileName);
    }

    [Fact]
    public void ProfileValidationException_WithErrors_ShouldSetValidationErrors()
    {
        // Arrange
        var errors = new[] { "Error 1", "Error 2", "Error 3" };

        // Act
        var exception = new ProfileValidationException("profile1", errors);

        // Assert
        Assert.Contains("Profile 'profile1' validation failed", exception.Message);
        Assert.Contains("Error 1", exception.Message);
        Assert.Equal(errors, exception.ValidationErrors);
    }

    [Fact]
    public void ProfileValidationException_WithSingleError_ShouldIncludeErrorMessage()
    {
        // Arrange
        var errors = new[] { "Single error" };

        // Act
        var exception = new ProfileValidationException("profile1", errors);

        // Assert
        Assert.Contains("Profile 'profile1' validation failed", exception.Message);
        Assert.Contains("Single error", exception.Message);
    }

    #endregion

    #region ConfigurationException Tests

    [Fact]
    public void ConfigurationException_DefaultConstructor_ShouldCreateException()
    {
        // Act
        var exception = new ConfigurationException();

        // Assert
        Assert.NotNull(exception);
    }

    [Fact]
    public void ConfigurationException_WithMessage_ShouldSetMessage()
    {
        // Act
        var exception = new ConfigurationException("Config error");

        // Assert
        Assert.Equal("Config error", exception.Message);
    }

    [Fact]
    public void ConfigurationException_WithMessageAndInnerException_ShouldSetBoth()
    {
        // Arrange
        var innerException = new System.Text.Json.JsonException("Invalid JSON");

        // Act
        var exception = new ConfigurationException("Config error", innerException);

        // Assert
        Assert.Equal("Config error", exception.Message);
        Assert.Same(innerException, exception.InnerException);
    }

    #endregion
}
