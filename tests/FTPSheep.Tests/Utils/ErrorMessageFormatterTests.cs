using FTPSheep.Core.Exceptions;
using FTPSheep.Core.Models;
using FTPSheep.Core.Utils;

namespace FTPSheep.Tests.Utils;

public class ErrorMessageFormatterTests {
    [Fact]
    public void FormatException_WithNullException_ShouldThrowArgumentNullException() {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => ErrorMessageFormatter.FormatException(null!));
    }

    [Fact]
    public void FormatConcise_WithNullException_ShouldThrowArgumentNullException() {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => ErrorMessageFormatter.FormatConcise(null!));
    }

    [Fact]
    public void FormatException_WithSimpleException_ShouldIncludeErrorMessage() {
        // Arrange
        var exception = new InvalidOperationException("Test error");

        // Act
        var formatted = ErrorMessageFormatter.FormatException(exception);

        // Assert
        Assert.Contains("ERROR: Test error", formatted);
    }

    [Fact]
    public void FormatConcise_ShouldReturnTypeAndMessage() {
        // Arrange
        var exception = new InvalidOperationException("Test error");

        // Act
        var formatted = ErrorMessageFormatter.FormatConcise(exception);

        // Assert
        Assert.Equal("InvalidOperationException: Test error", formatted);
    }

    [Fact]
    public void FormatException_WithBuildToolNotFoundException_ShouldIncludeSuggestions() {
        // Arrange
        var exception = new BuildToolNotFoundException("MSBuild");

        // Act
        var formatted = ErrorMessageFormatter.FormatException(exception);

        // Assert
        Assert.Contains("Suggestions:", formatted);
        Assert.Contains("Install MSBuild", formatted);
        Assert.Contains("Developer Command Prompt", formatted);
    }

    [Fact]
    public void FormatException_WithBuildCompilationException_ShouldIncludeSuggestions() {
        // Arrange
        var exception = new BuildCompilationException(new[] { "Error CS1001" });

        // Act
        var formatted = ErrorMessageFormatter.FormatException(exception);

        // Assert
        Assert.Contains("Suggestions:", formatted);
        Assert.Contains("Review build errors", formatted);
        Assert.Contains("dotnet restore", formatted);
        Assert.Contains("target framework", formatted);
    }

    [Fact]
    public void FormatException_WithConnectionTimeoutException_ShouldIncludeSuggestions() {
        // Arrange
        var exception = new ConnectionTimeoutException("server.com", 21, TimeSpan.FromSeconds(30));

        // Act
        var formatted = ErrorMessageFormatter.FormatException(exception);

        // Assert
        Assert.Contains("Suggestions:", formatted);
        Assert.Contains("network connection", formatted);
        Assert.Contains("server is running", formatted);
        Assert.Contains("timeout value", formatted);
        Assert.Contains("firewall", formatted);
    }

    [Fact]
    public void FormatException_WithConnectionRefusedException_ShouldIncludeSuggestions() {
        // Arrange
        var exception = new ConnectionRefusedException("ftp.example.com", 21);

        // Act
        var formatted = ErrorMessageFormatter.FormatException(exception);

        // Assert
        Assert.Contains("Suggestions:", formatted);
        Assert.Contains("ftp.example.com:21", formatted);
        Assert.Contains("correct port", formatted);
        Assert.Contains("firewall", formatted);
    }

    [Fact]
    public void FormatException_WithSslCertificateException_ShouldIncludeSuggestions() {
        // Arrange
        var exception = new SslCertificateException("server.com", "Certificate expired");

        // Act
        var formatted = ErrorMessageFormatter.FormatException(exception);

        // Assert
        Assert.Contains("Suggestions:", formatted);
        Assert.Contains("SSL certificate", formatted);
        Assert.Contains("self-signed certificate", formatted);
        Assert.Contains("system date and time", formatted);
    }

    [Fact]
    public void FormatException_WithInvalidCredentialsException_ShouldIncludeSuggestions() {
        // Arrange
        var exception = new InvalidCredentialsException("testuser", "server.com");

        // Act
        var formatted = ErrorMessageFormatter.FormatException(exception);

        // Assert
        Assert.Contains("Suggestions:", formatted);
        Assert.Contains("username and password", formatted);
        Assert.Contains("ftpsheep credentials list", formatted);
        Assert.Contains("FTP_USERNAME, FTP_PASSWORD", formatted);
        Assert.Contains("ftpsheep credentials save", formatted);
    }

    [Fact]
    public void FormatException_WithInsufficientPermissionsException_ShouldIncludeSuggestions() {
        // Arrange
        var exception = new InsufficientPermissionsException("testuser", "server.com", "WRITE");

        // Act
        var formatted = ErrorMessageFormatter.FormatException(exception);

        // Assert
        Assert.Contains("Suggestions:", formatted);
        Assert.Contains("'WRITE' permission", formatted);
        Assert.Contains("server administrator", formatted);
        Assert.Contains("directory exists", formatted);
    }

    [Fact]
    public void FormatException_WithAuthenticationException_ShouldIncludeSuggestions() {
        // Arrange
        var exception = new AuthenticationException("Auth failed", "testuser", "server.com");

        // Act
        var formatted = ErrorMessageFormatter.FormatException(exception);

        // Assert
        Assert.Contains("Suggestions:", formatted);
        Assert.Contains("credentials are correct", formatted);
        Assert.Contains("password/key", formatted);
        Assert.Contains("'testuser' exists", formatted);
    }

    [Fact]
    public void FormatException_WithFileTransferException_ShouldIncludeSuggestions() {
        // Arrange
        var exception = new FileTransferException("/local/file.txt", "/remote/file.txt");

        // Act
        var formatted = ErrorMessageFormatter.FormatException(exception);

        // Assert
        Assert.Contains("Suggestions:", formatted);
        Assert.Contains("network connectivity", formatted);
        Assert.Contains("write permissions", formatted);
        Assert.Contains("disk space", formatted);
        Assert.Contains("transient failures", formatted);
    }

    [Fact]
    public void FormatException_WithInsufficientDiskSpaceException_ShouldIncludeSuggestions() {
        // Arrange
        var requiredBytes = 1000L * 1024 * 1024; // 1000 MB
        var availableBytes = 500L * 1024 * 1024;  // 500 MB
        var exception = new InsufficientDiskSpaceException(requiredBytes, availableBytes);

        // Act
        var formatted = ErrorMessageFormatter.FormatException(exception);

        // Assert
        Assert.Contains("Suggestions:", formatted);
        Assert.Contains("500 MB", formatted); // Free up at least 500 MB
        Assert.Contains("old deployments", formatted);
        Assert.Contains("disk quota", formatted);
    }

    [Fact]
    public void FormatException_WithProfileNotFoundException_ShouldIncludeSuggestions() {
        // Arrange
        var exception = new ProfileNotFoundException("myprofile");

        // Act
        var formatted = ErrorMessageFormatter.FormatException(exception);

        // Assert
        Assert.Contains("Suggestions:", formatted);
        Assert.Contains("ftpsheep profile create myprofile", formatted);
        Assert.Contains("ftpsheep profile list", formatted);
        Assert.Contains("typos", formatted);
    }

    [Fact]
    public void FormatException_WithProfileValidationException_ShouldIncludeSuggestions() {
        // Arrange
        var exception = new ProfileValidationException("profile1", new[] { "Error 1" });

        // Act
        var formatted = ErrorMessageFormatter.FormatException(exception);

        // Assert
        Assert.Contains("Suggestions:", formatted);
        Assert.Contains("Fix the validation errors", formatted);
        Assert.Contains("ftpsheep profile edit", formatted);
        Assert.Contains("ftpsheep profile show", formatted);
    }

    [Fact]
    public void FormatException_WithConfigurationException_ShouldIncludeSuggestions() {
        // Arrange
        var exception = new ConfigurationException("Invalid config");

        // Act
        var formatted = ErrorMessageFormatter.FormatException(exception);

        // Assert
        Assert.Contains("Suggestions:", formatted);
        Assert.Contains("configuration file", formatted);
        Assert.Contains("JSON syntax", formatted);
        Assert.Contains("ftpsheep config reset", formatted);
    }

    [Fact]
    public void FormatException_WithOperationCanceledException_ShouldIncludeSuggestions() {
        // Arrange
        var exception = new OperationCanceledException("Operation cancelled");

        // Act
        var formatted = ErrorMessageFormatter.FormatException(exception);

        // Assert
        Assert.Contains("Suggestions:", formatted);
        Assert.Contains("cancelled by user", formatted);
        Assert.Contains("Ctrl+C", formatted);
    }

    [Fact]
    public void FormatException_WithRetryableDeploymentException_ShouldIncludeSuggestions() {
        // Arrange
        var exception = new DeploymentException("Deploy failed", "profile1", DeploymentPhase.Upload, isRetryable: true);

        // Act
        var formatted = ErrorMessageFormatter.FormatException(exception);

        // Assert
        Assert.Contains("Suggestions:", formatted);
        Assert.Contains("transient error", formatted);
        Assert.Contains("try running the command again", formatted);
    }

    [Fact]
    public void FormatException_WithVerboseVerbosity_ShouldIncludeTechnicalDetails() {
        // Arrange
        var exception = new BuildException("Build failed", "/path/to/project.csproj", "Release");

        // Act
        var formatted = ErrorMessageFormatter.FormatException(exception, LogVerbosity.Verbose);

        // Assert
        Assert.Contains("Technical Details:", formatted);
        Assert.Contains("Exception Type:", formatted);
        Assert.Contains("BuildException", formatted);
        Assert.Contains("Project: /path/to/project.csproj", formatted);
        Assert.Contains("Configuration: Release", formatted);
        Assert.Contains("Stack Trace:", formatted);
    }

    [Fact]
    public void FormatException_WithNormalVerbosity_ShouldNotIncludeTechnicalDetails() {
        // Arrange
        var exception = new BuildException("Build failed");

        // Act
        var formatted = ErrorMessageFormatter.FormatException(exception, LogVerbosity.Normal);

        // Assert
        Assert.DoesNotContain("Technical Details:", formatted);
        Assert.DoesNotContain("Stack Trace:", formatted);
    }

    [Fact]
    public void FormatException_WithInnerException_ShouldIncludeInnerExceptionDetails() {
        // Arrange
        var innerException = new InvalidOperationException("Inner error");
        var exception = new DeploymentException("Deployment failed", innerException);

        // Act
        var formatted = ErrorMessageFormatter.FormatException(exception, LogVerbosity.Verbose);

        // Assert
        Assert.Contains("Inner Exception: InvalidOperationException", formatted);
        Assert.Contains("Message: Inner error", formatted);
    }

    [Fact]
    public void FormatException_WithConnectionException_ShouldIncludeConnectionDetails() {
        // Arrange
        var exception = new ConnectionException("Connection failed", "server.com", 21, isTransient: true);

        // Act
        var formatted = ErrorMessageFormatter.FormatException(exception, LogVerbosity.Verbose);

        // Assert
        Assert.Contains("Host: server.com", formatted);
        Assert.Contains("Port: 21", formatted);
        Assert.Contains("Transient: True", formatted);
    }

    [Fact]
    public void FormatException_WithAuthenticationExceptionInVerboseMode_ShouldIncludeAuthDetails() {
        // Arrange
        var exception = new AuthenticationException("Auth failed", "testuser", "server.com");

        // Act
        var formatted = ErrorMessageFormatter.FormatException(exception, LogVerbosity.Verbose);

        // Assert
        Assert.Contains("Username: testuser", formatted);
        Assert.Contains("Host: server.com", formatted);
    }

    [Fact]
    public void FormatException_WithDeploymentExceptionInVerboseMode_ShouldIncludeDeploymentDetails() {
        // Arrange
        var exception = new DeploymentException("Deploy failed", "profile1", DeploymentPhase.Upload, isRetryable: true);

        // Act
        var formatted = ErrorMessageFormatter.FormatException(exception, LogVerbosity.Verbose);

        // Assert
        Assert.Contains("Profile: profile1", formatted);
        Assert.Contains("Phase: Upload", formatted);
        Assert.Contains("Retryable: True", formatted);
    }

    [Fact]
    public void FormatException_WithProfileException_ShouldIncludeProfileName() {
        // Arrange
        var exception = new ProfileNotFoundException("myprofile");

        // Act
        var formatted = ErrorMessageFormatter.FormatException(exception, LogVerbosity.Verbose);

        // Assert
        Assert.Contains("Profile: myprofile", formatted);
    }
}
