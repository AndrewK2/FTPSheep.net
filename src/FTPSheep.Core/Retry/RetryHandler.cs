using Microsoft.Extensions.Logging;

namespace FTPSheep.Core.Retry;

/// <summary>
/// Handles retry logic for operations that may fail transiently.
/// </summary>
public sealed class RetryHandler {
    private readonly RetryPolicy _policy;
    private readonly ILogger? _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="RetryHandler"/> class.
    /// </summary>
    /// <param name="policy">The retry policy to use.</param>
    /// <param name="logger">Optional logger for logging retry attempts.</param>
    public RetryHandler(RetryPolicy policy, ILogger? logger = null) {
        _policy = policy ?? throw new ArgumentNullException(nameof(policy));
        _logger = logger;
    }

    /// <summary>
    /// Executes an operation with retry logic.
    /// </summary>
    /// <typeparam name="T">The return type of the operation.</typeparam>
    /// <param name="operation">The operation to execute.</param>
    /// <param name="operationName">The name of the operation (for logging).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The result of the operation.</returns>
    /// <exception cref="Exception">Throws the last exception if all retries are exhausted.</exception>
    public async Task<T> ExecuteAsync<T>(
        Func<Task<T>> operation,
        string operationName = "Operation",
        CancellationToken cancellationToken = default) {
        if(operation == null) {
            throw new ArgumentNullException(nameof(operation));
        }

        Exception? lastException = null;
        var attempt = 0;

        while(attempt <= _policy.MaxRetryCount) {
            try {
                _logger?.LogDebug("Executing {OperationName} (Attempt {Attempt}/{MaxAttempts})",
                    operationName, attempt + 1, _policy.MaxRetryCount + 1);

                return await operation();
            } catch(Exception ex) when(attempt < _policy.MaxRetryCount) {
                lastException = ex;

                // Check if the exception is retryable
                var isRetryable = _policy.IsRetryableException?.Invoke(ex) ?? RetryPolicy.DefaultIsRetryable(ex);

                if(!isRetryable) {
                    _logger?.LogWarning("Exception is not retryable. Aborting retry attempts for {OperationName}: {ExceptionMessage}",
                        operationName, ex.Message);
                    throw;
                }

                // Calculate delay for this retry attempt
                var delay = _policy.CalculateDelay(attempt);

                _logger?.LogWarning("Transient error on attempt {Attempt}/{MaxAttempts} for {OperationName}. " +
                    "Retrying in {DelaySeconds:F1}s. Error: {ExceptionMessage}",
                    attempt + 1, _policy.MaxRetryCount + 1, operationName, delay.TotalSeconds, ex.Message);

                // Wait before retrying
                await Task.Delay(delay, cancellationToken);

                attempt++;
            } catch(Exception ex) {
                // Last attempt failed or non-retryable exception
                lastException = ex;
                break;
            }
        }

        // All retries exhausted
        _logger?.LogError("All {MaxRetryCount} retry attempts exhausted for {OperationName}. Last error: {ExceptionMessage}",
            _policy.MaxRetryCount, operationName, lastException?.Message);

        throw lastException ?? new InvalidOperationException($"Operation '{operationName}' failed with no exception captured.");
    }

    /// <summary>
    /// Executes an operation with retry logic (without return value).
    /// </summary>
    /// <param name="operation">The operation to execute.</param>
    /// <param name="operationName">The name of the operation (for logging).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="Exception">Throws the last exception if all retries are exhausted.</exception>
    public async Task ExecuteAsync(
        Func<Task> operation,
        string operationName = "Operation",
        CancellationToken cancellationToken = default) {
        await ExecuteAsync(async () => {
            await operation();
            return 0; // Dummy return value
        }, operationName, cancellationToken);
    }

    /// <summary>
    /// Executes a synchronous operation with retry logic.
    /// </summary>
    /// <typeparam name="T">The return type of the operation.</typeparam>
    /// <param name="operation">The operation to execute.</param>
    /// <param name="operationName">The name of the operation (for logging).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The result of the operation.</returns>
    public async Task<T> ExecuteAsync<T>(
        Func<T> operation,
        string operationName = "Operation",
        CancellationToken cancellationToken = default) {
        return await ExecuteAsync(() => Task.FromResult(operation()), operationName, cancellationToken);
    }

    /// <summary>
    /// Executes a synchronous operation with retry logic (without return value).
    /// </summary>
    /// <param name="operation">The operation to execute.</param>
    /// <param name="operationName">The name of the operation (for logging).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task ExecuteAsync(
        Action operation,
        string operationName = "Operation",
        CancellationToken cancellationToken = default) {
        await ExecuteAsync(() => {
            operation();
            return Task.CompletedTask;
        }, operationName, cancellationToken);
    }
}
