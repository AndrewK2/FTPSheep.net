using FTPSheep.Core.Models;
using FTPSheep.Core.Services;
using Microsoft.Extensions.Logging;

namespace FTPSheep.VisualStudio.Modern.Services;

/// <summary>
/// Orchestrates deployments with Visual Studio integration.
/// Bridges DeploymentCoordinator events with VS Output window and Status bar.
/// </summary>
public class VsDeploymentOrchestrator
{
    private readonly DeploymentCoordinator coordinator;
    private readonly VsOutputWindowService outputWindow;
    private readonly VsStatusBarService statusBar;
    private readonly ILogger<VsDeploymentOrchestrator> logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="VsDeploymentOrchestrator"/> class.
    /// </summary>
    public VsDeploymentOrchestrator(
        DeploymentCoordinator coordinator,
        VsOutputWindowService outputWindow,
        VsStatusBarService statusBar,
        ILogger<VsDeploymentOrchestrator> logger)
    {
        this.coordinator = coordinator;
        this.outputWindow = outputWindow;
        this.statusBar = statusBar;
        this.logger = logger;

        // Subscribe to coordinator events
        coordinator.StageChanged += OnStageChanged;
        coordinator.ProgressUpdated += OnProgressUpdated;
    }

    /// <summary>
    /// Executes a deployment with VS integration.
    /// </summary>
    /// <param name="options">The deployment options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The deployment result.</returns>
    public async Task<DeploymentResult> ExecuteDeploymentAsync(
        DeploymentOptions options,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Clear output window and show start message
            await outputWindow.WriteLineAsync("=".PadRight(80, '='), cancellationToken);
            await outputWindow.WriteLineAsync($"Starting deployment: {options.ProfileName}", cancellationToken);
            await outputWindow.WriteLineAsync("=".PadRight(80, '='), cancellationToken);
            await statusBar.SetTextAsync("FTPSheep: Starting deployment...", cancellationToken);

            // Execute deployment
            var result = await coordinator.ExecuteDeploymentAsync(options, cancellationToken);

            // Show completion message
            if (result.Success)
            {
                var duration = result.Duration ?? TimeSpan.Zero;

                await outputWindow.WriteSuccessAsync(
                    $"Deployment completed successfully in {duration.TotalSeconds:F1}s - {result.FilesUploaded} files uploaded",
                    cancellationToken);
                await statusBar.ShowSuccessAsync(
                    $"Deployment completed - {result.FilesUploaded} files",
                    cancellationToken);
            }
            else if (result.WasCancelled)
            {
                await outputWindow.WriteWarningAsync("Deployment was cancelled", cancellationToken);
                await statusBar.SetTextAsync("FTPSheep: Deployment cancelled", cancellationToken);
            }
            else
            {
                var errorMsg = result.ErrorMessages.Count > 0
                    ? string.Join("; ", result.ErrorMessages)
                    : "Unknown error";
                await outputWindow.WriteErrorAsync($"Deployment failed: {errorMsg}", cancellationToken);
                await statusBar.ShowErrorAsync("Deployment failed", cancellationToken);
            }

            await outputWindow.WriteLineAsync("=".PadRight(80, '='), cancellationToken);

            return result;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Deployment orchestration failed");
            await outputWindow.WriteErrorAsync($"Deployment error: {ex.Message}", cancellationToken);
            await statusBar.ShowErrorAsync("Deployment error", cancellationToken);
            throw;
        }
    }

    /// <summary>
    /// Handles stage change events from the deployment coordinator.
    /// </summary>
    private void OnStageChanged(object? sender, DeploymentStageChangedEventArgs e)
    {
        // Run on background thread to avoid blocking coordinator
        _ = Task.Run(async () =>
        {
            try
            {
                var stageName = GetStageName(e.Stage);
                await outputWindow.WriteLineAsync($"Stage: {stageName}", CancellationToken.None);
                await statusBar.SetTextAsync($"FTPSheep: {stageName}...", CancellationToken.None);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to update stage in VS UI");
            }
        });
    }

    /// <summary>
    /// Handles progress update events from the deployment coordinator.
    /// </summary>
    private void OnProgressUpdated(object? sender, DeploymentProgressEventArgs e)
    {
        // Run on background thread to avoid blocking coordinator
        _ = Task.Run(async () =>
        {
            try
            {
                if (e.State.TotalFiles > 0)
                {
                    await statusBar.ShowProgressAsync(
                        e.State.FilesUploaded,
                        e.State.TotalFiles,
                        "Uploading files",
                        CancellationToken.None);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to update progress in VS UI");
            }
        });
    }

    /// <summary>
    /// Gets a friendly name for a deployment stage.
    /// </summary>
    private static string GetStageName(DeploymentStage stage)
    {
        return stage switch
        {
            DeploymentStage.NotStarted => "Not Started",
            DeploymentStage.LoadingProfile => "Loading Profile",
            DeploymentStage.BuildingProject => "Building Project",
            DeploymentStage.ConnectingToServer => "Connecting to Server",
            DeploymentStage.PreDeploymentSummary => "Pre-Deployment Summary",
            DeploymentStage.UploadingAppOffline => "Uploading app_offline.htm",
            DeploymentStage.UploadingFiles => "Uploading Files",
            DeploymentStage.CleaningUpObsoleteFiles => "Cleaning Up Obsolete Files",
            DeploymentStage.DeletingAppOffline => "Deleting app_offline.htm",
            DeploymentStage.RecordingHistory => "Recording History",
            DeploymentStage.Completed => "Completed",
            DeploymentStage.Failed => "Failed",
            DeploymentStage.Cancelled => "Cancelled",
            _ => stage.ToString()
        };
    }
}
