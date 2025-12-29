using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace FTPSheep.VisualStudio.Services;

/// <summary>
/// Service for writing output to the Visual Studio Output window.
/// </summary>
public class VsOutputWindowService
{
    private readonly IVsOutputWindow outputWindow;
    private Guid ftpSheepPaneGuid = new("2B3C4D5E-6F7A-8B9C-0D1E-2F3A4B5C6D7E");
    private IVsOutputWindowPane? ftpSheepPane;
    private bool isInitialized;

    /// <summary>
    /// Initializes a new instance of the <see cref="VsOutputWindowService"/> class.
    /// </summary>
    public VsOutputWindowService(IVsOutputWindow outputWindow)
    {
        this.outputWindow = outputWindow ?? throw new ArgumentNullException(nameof(outputWindow));
    }

    /// <summary>
    /// Initializes the FTPSheep output pane.
    /// </summary>
    private void EnsurePane()
    {
        if (isInitialized) return;

        ThreadHelper.ThrowIfNotOnUIThread();

        try
        {
            // Try to get existing pane
            var hr = outputWindow.GetPane(ref ftpSheepPaneGuid, out ftpSheepPane);

            if (hr != VSConstants.S_OK || ftpSheepPane == null)
            {
                // Create new pane
                outputWindow.CreatePane(
                    ref ftpSheepPaneGuid,
                    "FTPSheep",
                    1,  // fVisible
                    0); // fClearWithSolution

                outputWindow.GetPane(ref ftpSheepPaneGuid, out ftpSheepPane);
            }

            isInitialized = ftpSheepPane != null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"FTPSheep: Failed to create output pane: {ex.Message}");
        }
    }

    /// <summary>
    /// Writes a line of text to the FTPSheep output pane.
    /// </summary>
    public void WriteLine(string message)
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        EnsurePane();

        if (ftpSheepPane == null) return;

        var timestamp = DateTime.Now.ToString("HH:mm:ss");
        ftpSheepPane.OutputString($"[{timestamp}] {message}\n");
    }

    /// <summary>
    /// Writes a line of text to the FTPSheep output pane asynchronously.
    /// </summary>
    public async Task WriteLineAsync(string message)
    {
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
        WriteLine(message);
    }

    /// <summary>
    /// Activates the FTPSheep output pane, bringing it to focus.
    /// </summary>
    public void Activate()
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        EnsurePane();

        if (ftpSheepPane != null)
        {
            ftpSheepPane.Activate();
        }
    }

    /// <summary>
    /// Activates the FTPSheep output pane asynchronously.
    /// </summary>
    public async Task ActivateAsync()
    {
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
        Activate();
    }

    /// <summary>
    /// Clears all output from the FTPSheep pane.
    /// </summary>
    public void Clear()
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        EnsurePane();

        if (ftpSheepPane != null)
        {
            ftpSheepPane.Clear();
        }
    }

    /// <summary>
    /// Clears all output from the FTPSheep pane asynchronously.
    /// </summary>
    public async Task ClearAsync()
    {
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
        Clear();
    }
}
