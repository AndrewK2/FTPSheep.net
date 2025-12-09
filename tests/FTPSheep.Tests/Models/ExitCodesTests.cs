using FTPSheep.Core.Exceptions;
using FTPSheep.Core.Models;

namespace FTPSheep.Tests.Models;

public class ExitCodesTests {
    [Fact]
    public void ExitCodes_ShouldHaveExpectedValues() {
        // Assert
        Assert.Equal(0, ExitCodes.Success);
        Assert.Equal(1, ExitCodes.GeneralError);
        Assert.Equal(2, ExitCodes.BuildFailure);
        Assert.Equal(3, ExitCodes.ConnectionFailure);
        Assert.Equal(4, ExitCodes.AuthenticationFailure);
        Assert.Equal(5, ExitCodes.DeploymentFailure);
        Assert.Equal(6, ExitCodes.ConfigurationError);
        Assert.Equal(7, ExitCodes.ProfileNotFound);
        Assert.Equal(8, ExitCodes.InvalidArguments);
        Assert.Equal(9, ExitCodes.OperationCancelled);
    }

    [Fact]
    public void FromException_WithBuildException_ShouldReturnBuildFailure() {
        // Arrange
        var exception = new BuildException("Build failed");

        // Act
        var exitCode = ExitCodes.FromException(exception);

        // Assert
        Assert.Equal(ExitCodes.BuildFailure, exitCode);
    }

    [Fact]
    public void FromException_WithBuildCompilationException_ShouldReturnBuildFailure() {
        // Arrange
        var exception = new BuildCompilationException(new[] { "Error 1", "Error 2" });

        // Act
        var exitCode = ExitCodes.FromException(exception);

        // Assert
        Assert.Equal(ExitCodes.BuildFailure, exitCode);
    }

    [Fact]
    public void FromException_WithConnectionException_ShouldReturnConnectionFailure() {
        // Arrange
        var exception = new ConnectionException("Connection failed");

        // Act
        var exitCode = ExitCodes.FromException(exception);

        // Assert
        Assert.Equal(ExitCodes.ConnectionFailure, exitCode);
    }

    [Fact]
    public void FromException_WithConnectionTimeoutException_ShouldReturnConnectionFailure() {
        // Arrange
        var exception = new ConnectionTimeoutException("server.com", 21, TimeSpan.FromSeconds(30));

        // Act
        var exitCode = ExitCodes.FromException(exception);

        // Assert
        Assert.Equal(ExitCodes.ConnectionFailure, exitCode);
    }

    [Fact]
    public void FromException_WithAuthenticationException_ShouldReturnAuthenticationFailure() {
        // Arrange
        var exception = new AuthenticationException("Auth failed");

        // Act
        var exitCode = ExitCodes.FromException(exception);

        // Assert
        Assert.Equal(ExitCodes.AuthenticationFailure, exitCode);
    }

    [Fact]
    public void FromException_WithInvalidCredentialsException_ShouldReturnAuthenticationFailure() {
        // Arrange
        var exception = new InvalidCredentialsException("user", "server.com");

        // Act
        var exitCode = ExitCodes.FromException(exception);

        // Assert
        Assert.Equal(ExitCodes.AuthenticationFailure, exitCode);
    }

    [Fact]
    public void FromException_WithDeploymentException_ShouldReturnDeploymentFailure() {
        // Arrange
        var exception = new DeploymentException("Deployment failed");

        // Act
        var exitCode = ExitCodes.FromException(exception);

        // Assert
        Assert.Equal(ExitCodes.DeploymentFailure, exitCode);
    }

    [Fact]
    public void FromException_WithFileTransferException_ShouldReturnDeploymentFailure() {
        // Arrange
        var exception = new FileTransferException("/local/file.txt", "/remote/file.txt");

        // Act
        var exitCode = ExitCodes.FromException(exception);

        // Assert
        Assert.Equal(ExitCodes.DeploymentFailure, exitCode);
    }

    [Fact]
    public void FromException_WithConfigurationException_ShouldReturnConfigurationError() {
        // Arrange
        var exception = new ConfigurationException("Config error");

        // Act
        var exitCode = ExitCodes.FromException(exception);

        // Assert
        Assert.Equal(ExitCodes.ConfigurationError, exitCode);
    }

    [Fact]
    public void FromException_WithProfileNotFoundException_ShouldReturnProfileNotFound() {
        // Arrange
        var exception = new ProfileNotFoundException("profile1");

        // Act
        var exitCode = ExitCodes.FromException(exception);

        // Assert
        Assert.Equal(ExitCodes.ProfileNotFound, exitCode);
    }

    [Fact]
    public void FromException_WithProfileValidationException_ShouldReturnConfigurationError() {
        // Arrange
        var exception = new ProfileValidationException("profile1", new[] { "Error 1" });

        // Act
        var exitCode = ExitCodes.FromException(exception);

        // Assert
        Assert.Equal(ExitCodes.ConfigurationError, exitCode);
    }

    [Fact]
    public void FromException_WithOperationCanceledException_ShouldReturnOperationCancelled() {
        // Arrange
        var exception = new OperationCanceledException("Cancelled");

        // Act
        var exitCode = ExitCodes.FromException(exception);

        // Assert
        Assert.Equal(ExitCodes.OperationCancelled, exitCode);
    }

    [Fact]
    public void FromException_WithArgumentException_ShouldReturnInvalidArguments() {
        // Arrange
        var exception = new ArgumentException("Invalid argument");

        // Act
        var exitCode = ExitCodes.FromException(exception);

        // Assert
        Assert.Equal(ExitCodes.InvalidArguments, exitCode);
    }

    [Fact]
    public void FromException_WithUnknownException_ShouldReturnGeneralError() {
        // Arrange
        var exception = new InvalidOperationException("Unknown error");

        // Act
        var exitCode = ExitCodes.FromException(exception);

        // Assert
        Assert.Equal(ExitCodes.GeneralError, exitCode);
    }

    [Fact]
    public void GetDescription_WithSuccess_ShouldReturnSuccessMessage() {
        // Act
        var description = ExitCodes.GetDescription(ExitCodes.Success);

        // Assert
        Assert.Equal("Success", description);
    }

    [Fact]
    public void GetDescription_WithGeneralError_ShouldReturnGeneralErrorMessage() {
        // Act
        var description = ExitCodes.GetDescription(ExitCodes.GeneralError);

        // Assert
        Assert.Equal("General Error", description);
    }

    [Fact]
    public void GetDescription_WithBuildFailure_ShouldReturnBuildFailureMessage() {
        // Act
        var description = ExitCodes.GetDescription(ExitCodes.BuildFailure);

        // Assert
        Assert.Equal("Build Failure", description);
    }

    [Fact]
    public void GetDescription_WithConnectionFailure_ShouldReturnConnectionFailureMessage() {
        // Act
        var description = ExitCodes.GetDescription(ExitCodes.ConnectionFailure);

        // Assert
        Assert.Equal("Connection Failure", description);
    }

    [Fact]
    public void GetDescription_WithAuthenticationFailure_ShouldReturnAuthenticationFailureMessage() {
        // Act
        var description = ExitCodes.GetDescription(ExitCodes.AuthenticationFailure);

        // Assert
        Assert.Equal("Authentication Failure", description);
    }

    [Fact]
    public void GetDescription_WithDeploymentFailure_ShouldReturnDeploymentFailureMessage() {
        // Act
        var description = ExitCodes.GetDescription(ExitCodes.DeploymentFailure);

        // Assert
        Assert.Equal("Deployment Failure", description);
    }

    [Fact]
    public void GetDescription_WithConfigurationError_ShouldReturnConfigurationErrorMessage() {
        // Act
        var description = ExitCodes.GetDescription(ExitCodes.ConfigurationError);

        // Assert
        Assert.Equal("Configuration Error", description);
    }

    [Fact]
    public void GetDescription_WithProfileNotFound_ShouldReturnProfileNotFoundMessage() {
        // Act
        var description = ExitCodes.GetDescription(ExitCodes.ProfileNotFound);

        // Assert
        Assert.Equal("Profile Not Found", description);
    }

    [Fact]
    public void GetDescription_WithInvalidArguments_ShouldReturnInvalidArgumentsMessage() {
        // Act
        var description = ExitCodes.GetDescription(ExitCodes.InvalidArguments);

        // Assert
        Assert.Equal("Invalid Arguments", description);
    }

    [Fact]
    public void GetDescription_WithOperationCancelled_ShouldReturnOperationCancelledMessage() {
        // Act
        var description = ExitCodes.GetDescription(ExitCodes.OperationCancelled);

        // Assert
        Assert.Equal("Operation Cancelled", description);
    }

    [Fact]
    public void GetDescription_WithUnknownExitCode_ShouldReturnUnknownErrorMessage() {
        // Act
        var description = ExitCodes.GetDescription(999);

        // Assert
        Assert.Equal("Unknown Error (999)", description);
    }
}
