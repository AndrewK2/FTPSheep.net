using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Extensibility.Documents;

namespace FTPSheep.VisualStudio.Modern.Services;

/// <summary>
/// Logger provider that creates loggers which write to Visual Studio Output Window.
/// </summary>
[Experimental("VSEXTPREVIEW_OUTPUTWINDOW")]
internal sealed class OutputWindowLoggerProvider : ILoggerProvider {
    private readonly OutputChannel outputChannel;
    private readonly ConcurrentDictionary<string, OutputWindowLogger> loggers = new();
    private bool disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="OutputWindowLoggerProvider"/> class.
    /// </summary>
    /// <param name="outputChannel">The Visual Studio output channel to write to.</param>
    public OutputWindowLoggerProvider(OutputChannel outputChannel) {
        this.outputChannel = outputChannel ?? throw new ArgumentNullException(nameof(outputChannel));
    }

    /// <inheritdoc />
    public ILogger CreateLogger(string categoryName) {
        if(disposed) {
            throw new ObjectDisposedException(nameof(OutputWindowLoggerProvider));
        }

        return loggers.GetOrAdd(categoryName, name => new OutputWindowLogger(name, outputChannel));
    }

    /// <inheritdoc />
    public void Dispose() {
        if(!disposed) {
            loggers.Clear();
            disposed = true;
        }
    }
}