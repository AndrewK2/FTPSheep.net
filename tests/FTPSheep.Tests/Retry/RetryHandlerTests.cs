using FTPSheep.Core.Exceptions;
using FTPSheep.Core.Retry;
using Microsoft.Extensions.Logging;

namespace FTPSheep.Tests.Retry;

public class RetryHandlerTests {
    [Fact]
    public async Task ExecuteAsync_WithSuccessfulOperation_ShouldReturnResult() {
        // Arrange
        var policy = RetryPolicy.Default;
        var handler = new RetryHandler(policy);
        var expectedResult = 42;

        // Act
        var result = await handler.ExecuteAsync<int>(async () => await Task.FromResult(expectedResult), "TestOperation");

        // Assert
        Assert.Equal(expectedResult, result);
    }

    [Fact]
    public async Task ExecuteAsync_WithNullOperation_ShouldThrowArgumentNullException() {
        // Arrange
        var policy = RetryPolicy.Default;
        var handler = new RetryHandler(policy);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            handler.ExecuteAsync<int>((Func<Task<int>>)null!, "TestOperation"));
    }

    [Fact]
    public async Task ExecuteAsync_WithTransientFailureThenSuccess_ShouldRetryAndSucceed() {
        // Arrange
        var policy = RetryPolicy.Default;
        var handler = new RetryHandler(policy);
        var attemptCount = 0;

        async Task<int> Operation() {
            attemptCount++;
            if(attemptCount < 3) {
                throw new TimeoutException("Transient error");
            }
            return 42;
        }

        // Act
        var result = await handler.ExecuteAsync(Operation, "TestOperation");

        // Assert
        Assert.Equal(42, result);
        Assert.Equal(3, attemptCount);
    }

    [Fact]
    public async Task ExecuteAsync_WithNonRetryableException_ShouldThrowImmediately() {
        // Arrange
        var policy = RetryPolicy.Default;
        var handler = new RetryHandler(policy);
        var attemptCount = 0;

        async Task<int> Operation() {
            attemptCount++;
            throw new AuthenticationException("Non-retryable error");
        }

        // Act & Assert
        await Assert.ThrowsAsync<AuthenticationException>(() =>
            handler.ExecuteAsync(Operation, "TestOperation"));
        Assert.Equal(1, attemptCount); // Should only attempt once
    }

    [Fact]
    public async Task ExecuteAsync_WithAllRetriesExhausted_ShouldThrowLastException() {
        // Arrange
        var policy = new RetryPolicy { MaxRetryCount = 2 };
        var handler = new RetryHandler(policy);
        var attemptCount = 0;

        async Task<int> Operation() {
            attemptCount++;
            throw new TimeoutException($"Transient error {attemptCount}");
        }

        // Act & Assert
        var exception = await Assert.ThrowsAsync<TimeoutException>(() =>
            handler.ExecuteAsync(Operation, "TestOperation"));
        Assert.Equal("Transient error 3", exception.Message); // Last attempt message
        Assert.Equal(3, attemptCount); // Initial + 2 retries
    }

    [Fact]
    public async Task ExecuteAsync_WithCancellationToken_ShouldRespectCancellation() {
        // Arrange
        var policy = new RetryPolicy { MaxRetryCount = 5, InitialDelay = TimeSpan.FromSeconds(10) };
        var handler = new RetryHandler(policy);
        var cts = new CancellationTokenSource();
        var attemptCount = 0;

        async Task<int> Operation() {
            attemptCount++;
            if(attemptCount == 2) {
                cts.Cancel(); // Cancel on second attempt
            }
            throw new TimeoutException("Transient error");
        }

        // Act & Assert
        await Assert.ThrowsAsync<TaskCanceledException>(() =>
            handler.ExecuteAsync(Operation, "TestOperation", cts.Token));
    }

    [Fact]
    public async Task ExecuteAsync_VoidOperation_ShouldExecuteSuccessfully() {
        // Arrange
        var policy = RetryPolicy.Default;
        var handler = new RetryHandler(policy);
        var executionCount = 0;

        async Task Operation() {
            executionCount++;
            await Task.CompletedTask;
        }

        // Act
        await handler.ExecuteAsync(Operation, "TestOperation");

        // Assert
        Assert.Equal(1, executionCount);
    }

    [Fact]
    public async Task ExecuteAsync_VoidOperationWithRetries_ShouldRetryAndSucceed() {
        // Arrange
        var policy = RetryPolicy.Default;
        var handler = new RetryHandler(policy);
        var attemptCount = 0;

        async Task Operation() {
            attemptCount++;
            if(attemptCount < 2) {
                throw new TimeoutException("Transient error");
            }
            await Task.CompletedTask;
        }

        // Act
        await handler.ExecuteAsync(Operation, "TestOperation");

        // Assert
        Assert.Equal(2, attemptCount);
    }

    [Fact]
    public async Task ExecuteAsync_SynchronousOperation_ShouldExecuteSuccessfully() {
        // Arrange
        var policy = RetryPolicy.Default;
        var handler = new RetryHandler(policy);

        // Act
        var result = await handler.ExecuteAsync(() => 42, "TestOperation");

        // Assert
        Assert.Equal(42, result);
    }

    [Fact]
    public async Task ExecuteAsync_SynchronousOperationWithRetries_ShouldRetryAndSucceed() {
        // Arrange
        var policy = RetryPolicy.Default;
        var handler = new RetryHandler(policy);
        var attemptCount = 0;

        int Operation() {
            attemptCount++;
            if(attemptCount < 2) {
                throw new TimeoutException("Transient error");
            }
            return 42;
        }

        // Act
        var result = await handler.ExecuteAsync(Operation, "TestOperation");

        // Assert
        Assert.Equal(42, result);
        Assert.Equal(2, attemptCount);
    }

    [Fact]
    public async Task ExecuteAsync_SynchronousVoidOperation_ShouldExecuteSuccessfully() {
        // Arrange
        var policy = RetryPolicy.Default;
        var handler = new RetryHandler(policy);
        var executionCount = 0;

        void Operation() {
            executionCount++;
        }

        // Act
        await handler.ExecuteAsync(Operation, "TestOperation");

        // Assert
        Assert.Equal(1, executionCount);
    }

    [Fact]
    public async Task ExecuteAsync_WithLogger_ShouldLogRetryAttempts() {
        // Arrange
        var policy = new RetryPolicy { MaxRetryCount = 2, InitialDelay = TimeSpan.FromMilliseconds(1) };
        var logger = new TestLogger();
        var handler = new RetryHandler(policy, logger);
        var attemptCount = 0;

        async Task<int> Operation() {
            attemptCount++;
            if(attemptCount < 3) {
                throw new TimeoutException("Transient error");
            }
            return 42;
        }

        // Act
        var result = await handler.ExecuteAsync(Operation, "TestOperation");

        // Assert
        Assert.Equal(42, result);
        Assert.True(logger.LoggedMessages.Any(m => m.Contains("Attempt 1/3")));
        Assert.True(logger.LoggedMessages.Any(m => m.Contains("Attempt 2/3")));
        Assert.True(logger.LoggedMessages.Any(m => m.Contains("Attempt 3/3")));
        Assert.True(logger.LoggedMessages.Any(m => m.Contains("Transient error") && m.Contains("Retrying")));
    }

    [Fact]
    public async Task ExecuteAsync_WithAllRetriesExhaustedAndLogger_ShouldLogFailure() {
        // Arrange
        var policy = new RetryPolicy { MaxRetryCount = 1, InitialDelay = TimeSpan.FromMilliseconds(1) };
        var logger = new TestLogger();
        var handler = new RetryHandler(policy, logger);

        async Task<int> Operation() {
            throw new TimeoutException("Persistent error");
        }

        // Act & Assert
        await Assert.ThrowsAsync<TimeoutException>(() =>
            handler.ExecuteAsync(Operation, "TestOperation"));
        Assert.True(logger.LoggedMessages.Any(m => m.Contains("All 1 retry attempts exhausted")));
    }

    [Fact]
    public void Constructor_WithNullPolicy_ShouldThrowArgumentNullException() {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new RetryHandler(null!));
    }

    // Test logger implementation
    private class TestLogger : ILogger {
        public List<string> LoggedMessages { get; } = new();

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) {
            LoggedMessages.Add(formatter(state, exception));
        }
    }
}
