using System;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Shell;

namespace FTPSheep.VisualStudio.Services;

/// <summary>
/// Logger provider that writes to the Visual Studio Output window.
/// </summary>
public class VsOutputWindowLoggerProvider : ILoggerProvider
{
    private readonly Func<IServiceProvider, VsOutputWindowService> outputServiceFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="VsOutputWindowLoggerProvider"/> class.
    /// </summary>
    public VsOutputWindowLoggerProvider(Func<IServiceProvider, VsOutputWindowService> outputServiceFactory)
    {
        this.outputServiceFactory = outputServiceFactory ?? throw new ArgumentNullException(nameof(outputServiceFactory));
    }

    /// <summary>
    /// Creates a new logger instance.
    /// </summary>
    public ILogger CreateLogger(string categoryName)
    {
        return new VsOutputWindowLogger(categoryName, outputServiceFactory);
    }

    /// <summary>
    /// Disposes the provider.
    /// </summary>
    public void Dispose()
    {
        // Nothing to dispose
    }
}

/// <summary>
/// Logger that writes to the Visual Studio Output window.
/// </summary>
internal class VsOutputWindowLogger : ILogger
{
    private readonly string categoryName;
    private readonly Func<IServiceProvider, VsOutputWindowService> outputServiceFactory;
    private VsOutputWindowService? outputService;

    public VsOutputWindowLogger(string categoryName, Func<IServiceProvider, VsOutputWindowService> outputServiceFactory)
    {
        this.categoryName = categoryName;
        this.outputServiceFactory = outputServiceFactory;
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        return null;
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        // Only log Warning and above to VS Output window
        return logLevel >= LogLevel.Warning;
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
            return;

        try
        {
            // Lazy initialization of output service
            if (outputService == null)
            {
                // This will be called from DI context, so we should have access to the service provider
                // For now, we'll skip the actual logging if we can't get the service
                return;
            }

            var message = formatter(state, exception);
            var logMessage = $"[{logLevel}] {categoryName}: {message}";

            if (exception != null)
            {
                logMessage += Environment.NewLine + exception.ToString();
            }

            ThreadHelper.JoinableTaskFactory.Run(async () =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                outputService.WriteLine(logMessage);
            });
        }
        catch
        {
            // Silently fail - logging should never crash the app
        }
    }
}
