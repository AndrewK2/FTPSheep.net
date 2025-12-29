using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Extensibility;

namespace FTPSheep.VisualStudio.Modern.Services;

/// <summary>
/// Service for writing messages to the Visual Studio Output window.
/// Provides integration with VS Output pane for deployment logging.
/// NOTE: Currently uses logging until VS Extensibility Output window APIs are finalized.
/// </summary>
public class VsOutputWindowService
{
    private readonly ILogger<VsOutputWindowService> logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="VsOutputWindowService"/> class.
    /// </summary>
    /// <param name="logger">Logger for output messages.</param>
    public VsOutputWindowService(ILogger<VsOutputWindowService> logger)
    {
        this.logger = logger;
    }

    /// <summary>
    /// Writes a message to the FTPSheep output pane.
    /// </summary>
    /// <param name="message">The message to write.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public Task WriteLineAsync(string message, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("[{Time}] {Message}", DateTime.Now.ToString("HH:mm:ss"), message);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Writes an error message to the FTPSheep output pane.
    /// </summary>
    /// <param name="message">The error message to write.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public Task WriteErrorAsync(string message, CancellationToken cancellationToken = default)
    {
        logger.LogError("ERROR: {Message}", message);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Writes a warning message to the FTPSheep output pane.
    /// </summary>
    /// <param name="message">The warning message to write.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public Task WriteWarningAsync(string message, CancellationToken cancellationToken = default)
    {
        logger.LogWarning("WARNING: {Message}", message);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Writes a success message to the FTPSheep output pane.
    /// </summary>
    /// <param name="message">The success message to write.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public Task WriteSuccessAsync(string message, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("SUCCESS: {Message}", message);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Clears the FTPSheep output pane.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    public Task ClearAsync(CancellationToken cancellationToken = default)
    {
        // No-op for now
        return Task.CompletedTask;
    }
}
