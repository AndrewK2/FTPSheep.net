using Microsoft.Extensions.Logging;

namespace FTPSheep.VisualStudio.Modern.Services;

/// <summary>
/// Service for updating the Visual Studio Status bar.
/// Provides deployment progress feedback in the VS status bar.
/// NOTE: Currently uses logging until VS Extensibility Status bar APIs are finalized.
/// </summary>
public class VsStatusBarService
{
    private readonly ILogger<VsStatusBarService> logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="VsStatusBarService"/> class.
    /// </summary>
    /// <param name="logger">Logger for status messages.</param>
    public VsStatusBarService(ILogger<VsStatusBarService> logger)
    {
        this.logger = logger;
    }

    /// <summary>
    /// Sets the status bar text.
    /// </summary>
    /// <param name="text">The text to display in the status bar.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public Task SetTextAsync(string text, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Status: {Text}", text);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Shows deployment progress in the status bar.
    /// </summary>
    /// <param name="current">Current progress value.</param>
    /// <param name="total">Total progress value.</param>
    /// <param name="message">Optional message to display.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public Task ShowProgressAsync(int current, int total, string? message = null, CancellationToken cancellationToken = default)
    {
        var percentage = total > 0 ? (int)((current / (double)total) * 100) : 0;
        var statusText = message != null
            ? $"FTPSheep: {message} ({current}/{total} - {percentage}%)"
            : $"FTPSheep: {current}/{total} files ({percentage}%)";

        return SetTextAsync(statusText, cancellationToken);
    }

    /// <summary>
    /// Clears the status bar text.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    public Task ClearAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Shows a success message in the status bar.
    /// </summary>
    /// <param name="message">The success message to display.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public Task ShowSuccessAsync(string message, CancellationToken cancellationToken = default)
    {
        return SetTextAsync($"✓ FTPSheep: {message}", cancellationToken);
    }

    /// <summary>
    /// Shows an error message in the status bar.
    /// </summary>
    /// <param name="message">The error message to display.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public Task ShowErrorAsync(string message, CancellationToken cancellationToken = default)
    {
        return SetTextAsync($"✗ FTPSheep: {message}", cancellationToken);
    }
}
