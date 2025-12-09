namespace FTPSheep.Core.Retry;

/// <summary>
/// Defines a retry policy for handling transient failures.
/// </summary>
public sealed class RetryPolicy {
    /// <summary>
    /// Gets the maximum number of retry attempts.
    /// </summary>
    public int MaxRetryCount { get; init; } = 3;

    /// <summary>
    /// Gets the initial delay before the first retry.
    /// </summary>
    public TimeSpan InitialDelay { get; init; } = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Gets the maximum delay between retries.
    /// </summary>
    public TimeSpan MaxDelay { get; init; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Gets the backoff multiplier for exponential backoff.
    /// </summary>
    public double BackoffMultiplier { get; init; } = 2.0;

    /// <summary>
    /// Gets a value indicating whether to use exponential backoff.
    /// If false, uses constant delay.
    /// </summary>
    public bool UseExponentialBackoff { get; init; } = true;

    /// <summary>
    /// Gets the function to determine if an exception is retryable.
    /// </summary>
    public Func<Exception, bool>? IsRetryableException { get; init; }

    /// <summary>
    /// Gets the default retry policy with exponential backoff.
    /// </summary>
    public static RetryPolicy Default => new() {
        MaxRetryCount = 3,
        InitialDelay = TimeSpan.FromSeconds(1),
        MaxDelay = TimeSpan.FromSeconds(30),
        BackoffMultiplier = 2.0,
        UseExponentialBackoff = true,
        IsRetryableException = DefaultIsRetryable
    };

    /// <summary>
    /// Gets a retry policy with no retries.
    /// </summary>
    public static RetryPolicy NoRetry => new() {
        MaxRetryCount = 0
    };

    /// <summary>
    /// Calculates the delay for a given retry attempt.
    /// </summary>
    /// <param name="retryAttempt">The retry attempt number (0-based).</param>
    /// <returns>The delay to wait before retrying.</returns>
    public TimeSpan CalculateDelay(int retryAttempt) {
        if(retryAttempt < 0) {
            throw new ArgumentOutOfRangeException(nameof(retryAttempt), "Retry attempt must be non-negative.");
        }

        if(!UseExponentialBackoff) {
            return InitialDelay;
        }

        var delay = InitialDelay.TotalMilliseconds * Math.Pow(BackoffMultiplier, retryAttempt);
        var clampedDelay = Math.Min(delay, MaxDelay.TotalMilliseconds);
        return TimeSpan.FromMilliseconds(clampedDelay);
    }

    /// <summary>
    /// Determines if the given exception should be retried based on default rules.
    /// </summary>
    /// <param name="exception">The exception to check.</param>
    /// <returns>True if the exception should be retried, false otherwise.</returns>
    public static bool DefaultIsRetryable(Exception exception) {
        // Don't retry these specific exceptions - they indicate permanent errors
        if(exception is Exceptions.AuthenticationException) {
            return false; // Authentication failures are typically not transient
        }

        if(exception is Exceptions.BuildException) {
            return false; // Build failures are typically not transient
        }

        if(exception is Exceptions.ProfileValidationException) {
            return false; // Validation errors won't fix themselves
        }

        // Check if the exception explicitly indicates it's retryable
        if(exception is Exceptions.ConnectionException connectionEx) {
            return connectionEx.IsTransient;
        }

        if(exception is Exceptions.DeploymentException deploymentEx) {
            return deploymentEx.IsRetryable;
        }

        // Common transient exception types
        if(exception is TimeoutException or
            System.IO.IOException or
            System.Net.Sockets.SocketException) {
            return true;
        }

        // Default: don't retry unknown exceptions
        return false;
    }
}
