using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell.TableControl;
using Microsoft.VisualStudio.Shell.TableManager;

namespace FTPSheep.VisualStudio.Services;

/// <summary>
/// Service for managing errors in the Visual Studio Error List.
/// </summary>
public class VsErrorListService
{
    private readonly ErrorListProvider errorProvider;
    private static readonly Guid ProviderGuid = new("3C4D5E6F-7A8B-9C0D-1E2F-3A4B5C6D7E8F");

    /// <summary>
    /// Initializes a new instance of the <see cref="VsErrorListService"/> class.
    /// </summary>
    public VsErrorListService(IServiceProvider serviceProvider)
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        if (serviceProvider == null)
            throw new ArgumentNullException(nameof(serviceProvider));

        errorProvider = new ErrorListProvider(serviceProvider)
        {
            ProviderName = "FTPSheep",
            ProviderGuid = ProviderGuid
        };
    }

    /// <summary>
    /// Adds an error to the Error List.
    /// </summary>
    public void AddError(string message, string? file = null, int line = 0, int column = 0)
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        var task = new ErrorTask
        {
            ErrorCategory = TaskErrorCategory.Error,
            Text = message,
            Document = file ?? string.Empty,
            Line = line,
            Column = column,
            Category = TaskCategory.BuildCompile
        };

        errorProvider.Tasks.Add(task);
    }

    /// <summary>
    /// Adds an error to the Error List asynchronously.
    /// </summary>
    public async Task AddErrorAsync(string message, string? file = null, int line = 0, int column = 0)
    {
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
        AddError(message, file, line, column);
    }

    /// <summary>
    /// Adds a warning to the Error List.
    /// </summary>
    public void AddWarning(string message, string? file = null, int line = 0, int column = 0)
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        var task = new ErrorTask
        {
            ErrorCategory = TaskErrorCategory.Warning,
            Text = message,
            Document = file ?? string.Empty,
            Line = line,
            Column = column,
            Category = TaskCategory.BuildCompile
        };

        errorProvider.Tasks.Add(task);
    }

    /// <summary>
    /// Adds a warning to the Error List asynchronously.
    /// </summary>
    public async Task AddWarningAsync(string message, string? file = null, int line = 0, int column = 0)
    {
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
        AddWarning(message, file, line, column);
    }

    /// <summary>
    /// Clears all errors from the Error List.
    /// </summary>
    public void ClearErrors()
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        errorProvider.Tasks.Clear();
    }

    /// <summary>
    /// Clears all errors from the Error List asynchronously.
    /// </summary>
    public async Task ClearErrorsAsync()
    {
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
        ClearErrors();
    }

    /// <summary>
    /// Shows the Error List window.
    /// </summary>
    public void Show()
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        errorProvider.Show();
    }

    /// <summary>
    /// Shows the Error List window asynchronously.
    /// </summary>
    public async Task ShowAsync()
    {
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
        Show();
    }
}
