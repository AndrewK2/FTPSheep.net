using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FTPSheep.Core.Models;
using FTPSheep.Core.Services;
using Microsoft.VisualStudio.Shell;

namespace FTPSheep.VisualStudio.Services;

/// <summary>
/// Visual Studio-aware deployment orchestrator that wraps DeploymentCoordinator
/// and handles event marshaling to the UI thread.
/// </summary>
public class VsDeploymentOrchestrator
{
    private readonly DeploymentCoordinator coordinator;
    private readonly VsOutputWindowService outputWindow;
    private readonly VsStatusBarService statusBar;
    private readonly VsErrorListService errorList;

    /// <summary>
    /// Initializes a new instance of the <see cref="VsDeploymentOrchestrator"/> class.
    /// </summary>
    public VsDeploymentOrchestrator(
        DeploymentCoordinator coordinator,
        VsOutputWindowService outputWindow,
        VsStatusBarService statusBar,
        VsErrorListService errorList)
    {
        this.coordinator = coordinator ?? throw new ArgumentNullException(nameof(coordinator));
        this.outputWindow = outputWindow ?? throw new ArgumentNullException(nameof(outputWindow));
        this.statusBar = statusBar ?? throw new ArgumentNullException(nameof(statusBar));
        this.errorList = errorList ?? throw new ArgumentNullException(nameof(errorList));

        // Subscribe to DeploymentCoordinator events
        coordinator.StageChanged += OnStageChanged;
        coordinator.ProgressUpdated += OnProgressUpdated;
    }

    /// <summary>
    /// Gets the current deployment state.
    /// </summary>
    public DeploymentState State => coordinator.State;

    /// <summary>
    /// Executes a deployment asynchronously.
    /// </summary>
    public async Task<DeploymentResult> DeployAsync(
        DeploymentOptions options,
        CancellationToken cancellationToken = default)
    {
        // Clear previous errors
        await errorList.ClearErrorsAsync();

        // Activate output window
        await outputWindow.ActivateAsync();
        await outputWindow.WriteLineAsync($"Starting deployment: {options.ProfileName ?? "unnamed profile"}");

        // Start animation
        await statusBar.SetAnimationAsync(true);
        await statusBar.SetTextAsync("FTPSheep: Starting deployment...");

        try
        {
            // Execute deployment on background thread
            var result = await Task.Run(() =>
                coordinator.ExecuteDeploymentAsync(options, cancellationToken), cancellationToken);

            // Marshal back to UI thread for final updates
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            if (result.Success)
            {
                await statusBar.SetTextAsync("FTPSheep: Deployment completed successfully");
                await outputWindow.WriteLineAsync(
                    $"Deployment completed: {result.FilesUploaded} files uploaded in {result.Duration?.ToString(@"mm\:ss")}");
            }
            else if (result.WasCancelled)
            {
                await statusBar.SetTextAsync("FTPSheep: Deployment cancelled");
                await outputWindow.WriteLineAsync("Deployment cancelled by user");
            }
            else
            {
                await statusBar.SetTextAsync("FTPSheep: Deployment failed");
                await outputWindow.WriteLineAsync($"Deployment failed: {result.ErrorMessages.FirstOrDefault() ?? "Unknown error"}");

                // Add errors to Error List
                foreach (var error in result.ErrorMessages)
                {
                    await errorList.AddErrorAsync(error);
                }

                await errorList.ShowAsync();
            }

            return result;
        }
        catch (OperationCanceledException)
        {
            await statusBar.SetTextAsync("FTPSheep: Deployment cancelled");
            await outputWindow.WriteLineAsync("Deployment cancelled");

            return DeploymentResult.FromCancellation(coordinator.State);
        }
        catch (Exception ex)
        {
            await statusBar.SetTextAsync("FTPSheep: Deployment error");
            await outputWindow.WriteLineAsync($"Deployment error: {ex.Message}");

            await errorList.AddErrorAsync($"Deployment error: {ex.Message}");
            await errorList.ShowAsync();

            return DeploymentResult.FromFailure(coordinator.State, ex.Message, ex);
        }
        finally
        {
            // Stop animation
            await statusBar.SetAnimationAsync(false);
            await statusBar.HideProgressAsync();
        }
    }

    /// <summary>
    /// Cancels the current deployment.
    /// </summary>
    public void CancelDeployment()
    {
        coordinator.CancelDeployment();
    }

    /// <summary>
    /// Handles stage changes from DeploymentCoordinator.
    /// </summary>
    private void OnStageChanged(object? sender, DeploymentStageChangedEventArgs e)
    {
        // Marshal to UI thread
        ThreadHelper.JoinableTaskFactory.Run(async () =>
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            var stageDescription = GetStageDescription(e.Stage);
            await outputWindow.WriteLineAsync($"Stage: {stageDescription}");
            await statusBar.SetTextAsync($"FTPSheep: {stageDescription}");
        });
    }

    /// <summary>
    /// Handles progress updates from DeploymentCoordinator.
    /// </summary>
    private void OnProgressUpdated(object? sender, DeploymentProgressEventArgs e)
    {
        // Marshal to UI thread
        ThreadHelper.JoinableTaskFactory.Run(async () =>
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            var state = e.State;

            if (state.TotalFiles > 0)
            {
                var progress = $"{state.FilesUploaded}/{state.TotalFiles} files ({state.ProgressPercentage:F1}%)";
                await statusBar.ShowProgressAsync("FTPSheep", state.FilesUploaded, state.TotalFiles);
                await statusBar.SetTextAsync($"FTPSheep: Uploading {progress}");
            }
        });
    }

    /// <summary>
    /// Gets a user-friendly description for a deployment stage.
    /// </summary>
    private string GetStageDescription(DeploymentStage stage)
    {
        return stage switch
        {
            DeploymentStage.NotStarted => "Not started",
            DeploymentStage.LoadingProfile => "Loading profile",
            DeploymentStage.BuildingProject => "Building project",
            DeploymentStage.ConnectingToServer => "Connecting to server",
            DeploymentStage.PreDeploymentSummary => "Preparing deployment",
            DeploymentStage.UploadingAppOffline => "Uploading app_offline.htm",
            DeploymentStage.UploadingFiles => "Uploading files",
            DeploymentStage.CleaningUpObsoleteFiles => "Cleaning up obsolete files",
            DeploymentStage.DeletingAppOffline => "Removing app_offline.htm",
            DeploymentStage.RecordingHistory => "Recording deployment history",
            DeploymentStage.Completed => "Completed",
            DeploymentStage.Failed => "Failed",
            DeploymentStage.Cancelled => "Cancelled",
            _ => stage.ToString()
        };
    }
}
