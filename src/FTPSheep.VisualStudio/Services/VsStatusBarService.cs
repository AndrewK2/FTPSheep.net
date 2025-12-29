using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace FTPSheep.VisualStudio.Services;

/// <summary>
/// Service for updating the Visual Studio status bar.
/// </summary>
public class VsStatusBarService
{
    private readonly IVsStatusbar statusBar;
    private uint cookie;

    /// <summary>
    /// Initializes a new instance of the <see cref="VsStatusBarService"/> class.
    /// </summary>
    public VsStatusBarService(IVsStatusbar statusBar)
    {
        this.statusBar = statusBar ?? throw new ArgumentNullException(nameof(statusBar));
    }

    /// <summary>
    /// Sets the status bar text.
    /// </summary>
    public void SetText(string text)
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        statusBar?.SetText(text);
    }

    /// <summary>
    /// Sets the status bar text asynchronously.
    /// </summary>
    public async Task SetTextAsync(string text)
    {
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
        SetText(text);
    }

    /// <summary>
    /// Shows a progress indicator in the status bar.
    /// </summary>
    public void ShowProgress(string label, int complete, int total)
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        if (statusBar != null)
        {
            statusBar.Progress(ref cookie, 1, label, (uint)complete, (uint)total);
        }
    }

    /// <summary>
    /// Shows a progress indicator in the status bar asynchronously.
    /// </summary>
    public async Task ShowProgressAsync(string label, int complete, int total)
    {
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
        ShowProgress(label, complete, total);
    }

    /// <summary>
    /// Hides the progress indicator from the status bar.
    /// </summary>
    public void HideProgress()
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        if (statusBar != null)
        {
            statusBar.Progress(ref cookie, 0, "", 0, 0);
        }
    }

    /// <summary>
    /// Hides the progress indicator from the status bar asynchronously.
    /// </summary>
    public async Task HideProgressAsync()
    {
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
        HideProgress();
    }

    /// <summary>
    /// Sets the deployment animation on the status bar.
    /// </summary>
    public void SetAnimation(bool on)
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        if (statusBar != null)
        {
            object icon = (short)Microsoft.VisualStudio.Shell.Interop.Constants.SBAI_Deploy;
            statusBar.Animation(on ? 1 : 0, ref icon);
        }
    }

    /// <summary>
    /// Sets the deployment animation on the status bar asynchronously.
    /// </summary>
    public async Task SetAnimationAsync(bool on)
    {
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
        SetAnimation(on);
    }

    /// <summary>
    /// Clears the status bar text.
    /// </summary>
    public void Clear()
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        statusBar?.Clear();
    }

    /// <summary>
    /// Clears the status bar text asynchronously.
    /// </summary>
    public async Task ClearAsync()
    {
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
        Clear();
    }
}
