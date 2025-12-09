using FTPSheep.Core.Exceptions;
using FTPSheep.Core.Retry;

namespace FTPSheep.Tests.Retry;

public class RetryPolicyTests {
    [Fact]
    public void Default_ShouldHaveExpectedConfiguration() {
        // Arrange & Act
        var policy = RetryPolicy.Default;

        // Assert
        Assert.Equal(3, policy.MaxRetryCount);
        Assert.Equal(TimeSpan.FromSeconds(1), policy.InitialDelay);
        Assert.Equal(TimeSpan.FromSeconds(30), policy.MaxDelay);
        Assert.Equal(2.0, policy.BackoffMultiplier);
        Assert.True(policy.UseExponentialBackoff);
        Assert.NotNull(policy.IsRetryableException);
    }

    [Fact]
    public void NoRetry_ShouldHaveZeroRetries() {
        // Arrange & Act
        var policy = RetryPolicy.NoRetry;

        // Assert
        Assert.Equal(0, policy.MaxRetryCount);
    }

    [Theory]
    [InlineData(0, 1000)]      // First retry: 1s
    [InlineData(1, 2000)]      // Second retry: 2s
    [InlineData(2, 4000)]      // Third retry: 4s
    [InlineData(3, 8000)]      // Fourth retry: 8s
    [InlineData(4, 16000)]     // Fifth retry: 16s
    [InlineData(5, 30000)]     // Sixth retry: 30s (capped at MaxDelay)
    [InlineData(10, 30000)]    // Large retry: still 30s (capped)
    public void CalculateDelay_WithExponentialBackoff_ShouldReturnExpectedDelay(int retryAttempt, int expectedMilliseconds) {
        // Arrange
        var policy = new RetryPolicy {
            InitialDelay = TimeSpan.FromSeconds(1),
            MaxDelay = TimeSpan.FromSeconds(30),
            BackoffMultiplier = 2.0,
            UseExponentialBackoff = true
        };

        // Act
        var delay = policy.CalculateDelay(retryAttempt);

        // Assert
        Assert.Equal(expectedMilliseconds, delay.TotalMilliseconds);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(10)]
    public void CalculateDelay_WithoutExponentialBackoff_ShouldReturnConstantDelay(int retryAttempt) {
        // Arrange
        var policy = new RetryPolicy {
            InitialDelay = TimeSpan.FromSeconds(2),
            UseExponentialBackoff = false
        };

        // Act
        var delay = policy.CalculateDelay(retryAttempt);

        // Assert
        Assert.Equal(TimeSpan.FromSeconds(2), delay);
    }

    [Fact]
    public void CalculateDelay_WithNegativeAttempt_ShouldThrowArgumentOutOfRangeException() {
        // Arrange
        var policy = RetryPolicy.Default;

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => policy.CalculateDelay(-1));
    }

    [Fact]
    public void DefaultIsRetryable_WithAuthenticationException_ShouldReturnFalse() {
        // Arrange
        var exception = new AuthenticationException("Auth failed");

        // Act
        var isRetryable = RetryPolicy.DefaultIsRetryable(exception);

        // Assert
        Assert.False(isRetryable);
    }

    [Fact]
    public void DefaultIsRetryable_WithBuildException_ShouldReturnFalse() {
        // Arrange
        var exception = new BuildException("Build failed");

        // Act
        var isRetryable = RetryPolicy.DefaultIsRetryable(exception);

        // Assert
        Assert.False(isRetryable);
    }

    [Fact]
    public void DefaultIsRetryable_WithProfileValidationException_ShouldReturnFalse() {
        // Arrange
        var exception = new ProfileValidationException("profile1", new[] { "Error 1" });

        // Act
        var isRetryable = RetryPolicy.DefaultIsRetryable(exception);

        // Assert
        Assert.False(isRetryable);
    }

    [Fact]
    public void DefaultIsRetryable_WithTransientConnectionException_ShouldReturnTrue() {
        // Arrange
        var exception = new ConnectionException("Connection failed") { IsTransient = true };

        // Act
        var isRetryable = RetryPolicy.DefaultIsRetryable(exception);

        // Assert
        Assert.True(isRetryable);
    }

    [Fact]
    public void DefaultIsRetryable_WithNonTransientConnectionException_ShouldReturnFalse() {
        // Arrange
        var exception = new ConnectionException("Connection failed") { IsTransient = false };

        // Act
        var isRetryable = RetryPolicy.DefaultIsRetryable(exception);

        // Assert
        Assert.False(isRetryable);
    }

    [Fact]
    public void DefaultIsRetryable_WithRetryableDeploymentException_ShouldReturnTrue() {
        // Arrange
        var exception = new DeploymentException("Deployment failed", "profile1", DeploymentPhase.Upload, isRetryable: true);

        // Act
        var isRetryable = RetryPolicy.DefaultIsRetryable(exception);

        // Assert
        Assert.True(isRetryable);
    }

    [Fact]
    public void DefaultIsRetryable_WithNonRetryableDeploymentException_ShouldReturnFalse() {
        // Arrange
        var exception = new DeploymentException("Deployment failed", "profile1", DeploymentPhase.Build, isRetryable: false);

        // Act
        var isRetryable = RetryPolicy.DefaultIsRetryable(exception);

        // Assert
        Assert.False(isRetryable);
    }

    [Fact]
    public void DefaultIsRetryable_WithTimeoutException_ShouldReturnTrue() {
        // Arrange
        var exception = new TimeoutException("Operation timed out");

        // Act
        var isRetryable = RetryPolicy.DefaultIsRetryable(exception);

        // Assert
        Assert.True(isRetryable);
    }

    [Fact]
    public void DefaultIsRetryable_WithIOException_ShouldReturnTrue() {
        // Arrange
        var exception = new System.IO.IOException("I/O error");

        // Act
        var isRetryable = RetryPolicy.DefaultIsRetryable(exception);

        // Assert
        Assert.True(isRetryable);
    }

    [Fact]
    public void DefaultIsRetryable_WithSocketException_ShouldReturnTrue() {
        // Arrange
        var exception = new System.Net.Sockets.SocketException();

        // Act
        var isRetryable = RetryPolicy.DefaultIsRetryable(exception);

        // Assert
        Assert.True(isRetryable);
    }

    [Fact]
    public void DefaultIsRetryable_WithUnknownException_ShouldReturnFalse() {
        // Arrange
        var exception = new InvalidOperationException("Unknown error");

        // Act
        var isRetryable = RetryPolicy.DefaultIsRetryable(exception);

        // Assert
        Assert.False(isRetryable);
    }

    [Fact]
    public void CustomPolicy_WithCustomRetryableCheck_ShouldUseCustomLogic() {
        // Arrange
        var policy = new RetryPolicy {
            MaxRetryCount = 5,
            IsRetryableException = ex => ex is InvalidOperationException
        };

        // Act
        var isRetryableForInvalidOp = policy.IsRetryableException!(new InvalidOperationException());
        var isRetryableForTimeout = policy.IsRetryableException!(new TimeoutException());

        // Assert
        Assert.True(isRetryableForInvalidOp);
        Assert.False(isRetryableForTimeout);
    }
}
