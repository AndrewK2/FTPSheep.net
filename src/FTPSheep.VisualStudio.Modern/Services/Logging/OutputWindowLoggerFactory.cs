using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Extensibility;

namespace FTPSheep.VisualStudio.Modern.Services.Logging;

/// <summary>
/// Logger factory that creates loggers which write to Visual Studio Output Window.
/// </summary>
[Experimental("VSEXTPREVIEW_OUTPUTWINDOW")]
internal sealed class OutputWindowLoggerFactory : ILoggerFactory {
    private readonly OutputWindowLoggerProvider provider;
    private bool disposed;

    public OutputWindowLoggerFactory(VisualStudioExtensibility extensibility) {
        var outputChannel = extensibility.Views().Output
            .CreateOutputChannelAsync("FTPSheep", CancellationToken.None)
            .GetAwaiter()
            .GetResult();

        provider = new OutputWindowLoggerProvider(outputChannel);
    }

    /// <inheritdoc />
    public void AddProvider(ILoggerProvider loggerProvider) {
        // Additional providers are not supported in this implementation
        // This factory is specifically for VS Output Window only
    }

    /// <inheritdoc />
    public ILogger CreateLogger(string categoryName) {
        return disposed
            ? throw new ObjectDisposedException(nameof(OutputWindowLoggerFactory))
            : provider.CreateLogger(categoryName);
    }

    /// <inheritdoc />
    public void Dispose() {
        if(disposed) {
            return;
        }

        provider.Dispose();
        disposed = true;
    }
}