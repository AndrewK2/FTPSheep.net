using FTPSheep.Core.Models;
using FTPSheep.BuildTools.Models;

namespace FTPSheep.Core.Services;

/// <summary>
/// Coordinates the entire deployment workflow from build to upload to finalization.
/// </summary>
public class DeploymentCoordinator {
    private readonly DeploymentState state;
    private readonly AppOfflineManager appOfflineManager;
    private readonly ExclusionPatternMatcher exclusionMatcher;
    private readonly FileComparisonService fileComparisonService;
    private CancellationTokenSource? cancellationTokenSource;

    // Deployment context (populated during execution)
    private object? ftpClient;  // Will be IFtpClient when interface is defined
    private List<FileMetadata>? publishedFiles;
    private string? publishOutputPath;
    private DeploymentProfile? currentProfile;

    /// <summary>
    /// Event raised when the deployment stage changes.
    /// </summary>
    public event EventHandler<DeploymentStageChangedEventArgs>? StageChanged;

    /// <summary>
    /// Event raised when deployment progress is updated.
    /// </summary>
    public event EventHandler<DeploymentProgressEventArgs>? ProgressUpdated;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeploymentCoordinator"/> class.
    /// </summary>
    /// <param name="appOfflineManager">The app_offline.htm manager (optional).</param>
    /// <param name="exclusionMatcher">The exclusion pattern matcher (optional).</param>
    public DeploymentCoordinator(
        AppOfflineManager? appOfflineManager = null,
        ExclusionPatternMatcher? exclusionMatcher = null) {
        state = new DeploymentState();
        this.appOfflineManager = appOfflineManager ?? new AppOfflineManager();
        this.exclusionMatcher = exclusionMatcher ?? new ExclusionPatternMatcher();
        this.fileComparisonService = new FileComparisonService(this.exclusionMatcher);
    }

    /// <summary>
    /// Gets the current deployment state.
    /// </summary>
    public DeploymentState State => state;

    /// <summary>
    /// Executes the complete deployment workflow.
    /// </summary>
    /// <param name="options">The deployment options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The deployment result.</returns>
    public async Task<DeploymentResult> ExecuteDeploymentAsync(
        DeploymentOptions options,
        CancellationToken cancellationToken = default) {
        if(options == null) {
            throw new ArgumentNullException(nameof(options));
        }

        // Create linked cancellation token
        cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var token = cancellationTokenSource.Token;

        try {
            // Initialize deployment state
            InitializeDeployment(options);

            // Stage 1: Load profile and validate configuration
            await ExecuteStageAsync(DeploymentStage.LoadingProfile,
                () => LoadProfileAsync(options, token), token);

            // Stage 2: Build and publish project
            await ExecuteStageAsync(DeploymentStage.BuildingProject,
                () => BuildProjectAsync(options, token), token);

            // Stage 3: Connect to server and validate connection
            await ExecuteStageAsync(DeploymentStage.ConnectingToServer,
                () => ConnectToServerAsync(options, token), token);

            // Stage 4: Display pre-deployment summary and confirm
            await ExecuteStageAsync(DeploymentStage.PreDeploymentSummary,
                () => DisplayPreDeploymentSummaryAsync(options, token), token);

            // Stage 5: Upload app_offline.htm (if enabled)
            if(options.UseAppOffline) {
                await ExecuteStageAsync(DeploymentStage.UploadingAppOffline,
                    () => UploadAppOfflineAsync(options, token), token);
            }

            // Stage 6: Upload all published files (concurrent)
            await ExecuteStageAsync(DeploymentStage.UploadingFiles,
                () => UploadFilesAsync(options, token), token);

            // Stage 7: Clean up obsolete files (if cleanup mode enabled)
            if(options.CleanupMode) {
                await ExecuteStageAsync(DeploymentStage.CleaningUpObsoleteFiles,
                    () => CleanupObsoleteFilesAsync(options, token), token);
            }

            // Stage 8: Delete app_offline.htm (if deployment succeeded)
            if(options.UseAppOffline) {
                await ExecuteStageAsync(DeploymentStage.DeletingAppOffline,
                    () => DeleteAppOfflineAsync(options, token), token);
            }

            // Stage 9: Record deployment history and display summary
            await ExecuteStageAsync(DeploymentStage.RecordingHistory,
                () => RecordHistoryAsync(options, token), token);

            // Complete successfully
            CompleteDeployment(DeploymentStage.Completed);
            return DeploymentResult.FromSuccess(state);
        } catch(OperationCanceledException) {
            CompleteDeployment(DeploymentStage.Cancelled);
            return DeploymentResult.FromCancellation(state);
        } catch(Exception ex) {
            var errorMessage = $"Deployment failed at stage {state.CurrentStage}: {ex.Message}";
            CompleteDeployment(DeploymentStage.Failed, errorMessage, ex);
            return DeploymentResult.FromFailure(state, errorMessage, ex);
        } finally {
            cancellationTokenSource?.Dispose();
            cancellationTokenSource = null;
        }
    }

    /// <summary>
    /// Cancels the deployment if it's currently in progress.
    /// </summary>
    public void CancelDeployment() {
        if(state.IsInProgress && state.CanCancel) {
            state.CancellationRequested = true;
            cancellationTokenSource?.Cancel();
        }
    }

    /// <summary>
    /// Initializes the deployment state.
    /// </summary>
    private void InitializeDeployment(DeploymentOptions options) {
        state.StartedAt = DateTime.UtcNow;
        state.CurrentStage = DeploymentStage.NotStarted;
        state.ProfileName = options.ProfileName;
        state.ProjectPath = options.ProjectPath;
        state.TargetHost = options.TargetHost;
    }

    /// <summary>
    /// Executes a deployment stage with error handling and state management.
    /// </summary>
    private async Task ExecuteStageAsync(
        DeploymentStage stage,
        Func<Task> stageAction,
        CancellationToken cancellationToken) {
        cancellationToken.ThrowIfCancellationRequested();

        // Update stage
        state.CurrentStage = stage;
        state.CurrentStageStartedAt = DateTime.UtcNow;
        OnStageChanged(stage);

        // Execute stage
        await stageAction();
    }

    /// <summary>
    /// Stage 1: Load profile and validate configuration.
    /// </summary>
    private Task LoadProfileAsync(DeploymentOptions options, CancellationToken cancellationToken) {
        // TODO: Implement profile loading and validation
        // This will be implemented when profile management is added
        return Task.CompletedTask;
    }

    /// <summary>
    /// Stage 2: Build and publish project.
    /// </summary>
    private Task BuildProjectAsync(DeploymentOptions options, CancellationToken cancellationToken) {
        // TODO: Implement build and publish using BuildTools services
        // This will use MSBuildWrapper or DotnetCliExecutor
        return Task.CompletedTask;
    }

    /// <summary>
    /// Stage 3: Connect to server and validate connection.
    /// </summary>
    private Task ConnectToServerAsync(DeploymentOptions options, CancellationToken cancellationToken) {
        // TODO: Implement connection validation using FTP/SFTP services
        // This will use the Protocol services from Section 4
        return Task.CompletedTask;
    }

    /// <summary>
    /// Stage 4: Display pre-deployment summary and wait for confirmation.
    /// </summary>
    private Task DisplayPreDeploymentSummaryAsync(DeploymentOptions options, CancellationToken cancellationToken) {
        // TODO: Implement pre-deployment summary display
        // This will calculate file counts, sizes, and prompt for confirmation
        return Task.CompletedTask;
    }

    /// <summary>
    /// Stage 5: Upload app_offline.htm.
    /// </summary>
    private async Task UploadAppOfflineAsync(DeploymentOptions options, CancellationToken cancellationToken) {
        if(ftpClient == null) {
            throw new InvalidOperationException("FTP client not initialized. Connect to server first.");
        }

        // Generate app_offline.htm content
        var appOfflineContent = currentProfile?.AppOfflineTemplate != null
            ? appOfflineManager.GenerateAppOfflineContent()
            : AppOfflineManager.DefaultContent;

        // Create temporary file
        var tempPath = Path.Combine(Path.GetTempPath(), "app_offline.htm");
        await File.WriteAllTextAsync(tempPath, appOfflineContent, cancellationToken);

        try {
            // Upload to server (actual implementation will use ftpClient when IFtpClient is available)
            // For now, this is a placeholder that creates the structure
            // TODO: Upload temp file to server root as app_offline.htm
            // await ftpClient.UploadFileAsync(tempPath, "app_offline.htm", cancellationToken);
        } finally {
            // Clean up temp file
            if(File.Exists(tempPath)) {
                File.Delete(tempPath);
            }
        }
    }

    /// <summary>
    /// Stage 6: Upload all published files.
    /// </summary>
    private async Task UploadFilesAsync(DeploymentOptions options, CancellationToken cancellationToken) {
        if(ftpClient == null) {
            throw new InvalidOperationException("FTP client not initialized. Connect to server first.");
        }

        if(publishedFiles == null || publishedFiles.Count == 0) {
            throw new InvalidOperationException("No published files found. Build project first.");
        }

        // Update state with total files and size
        state.TotalFiles = publishedFiles.Count;
        state.TotalSize = publishedFiles.Sum(f => f.Size);
        OnProgressUpdated();

        // TODO: Implement concurrent file upload when Section 4.4 is completed
        // For now, this is a placeholder for the upload logic
        // The actual implementation will:
        // 1. Create upload queue with all files
        // 2. Upload files concurrently (respecting MaxConcurrentUploads)
        // 3. Update progress after each file upload
        // 4. Handle upload failures and retries
        // 5. Create remote directories as needed

        foreach(var file in publishedFiles) {
            cancellationToken.ThrowIfCancellationRequested();

            // TODO: Upload file to server
            // await ftpClient.UploadFileAsync(file.AbsolutePath, remotePath, cancellationToken);

            // Update progress
            state.FilesUploaded++;
            state.SizeUploaded += file.Size;
            OnProgressUpdated();
        }
    }

    /// <summary>
    /// Stage 7: Clean up obsolete files.
    /// </summary>
    private async Task CleanupObsoleteFilesAsync(DeploymentOptions options, CancellationToken cancellationToken) {
        if(ftpClient == null) {
            throw new InvalidOperationException("FTP client not initialized. Connect to server first.");
        }

        if(publishedFiles == null) {
            throw new InvalidOperationException("No published files found. Build project first.");
        }

        if(currentProfile == null) {
            throw new InvalidOperationException("No profile loaded.");
        }

        // TODO: List all files on server
        // var remoteFiles = await ftpClient.ListAllFilesAsync(remotePath, cancellationToken);

        // For now, use empty list as placeholder
        var remoteFiles = new List<string>();

        // Compare local and remote files to find obsolete files
        var comparisonResult = fileComparisonService.CompareFiles(publishedFiles, remoteFiles);

        if(!comparisonResult.HasObsoleteFiles) {
            // No obsolete files to delete
            return;
        }

        // Update state with cleanup info
        state.ObsoleteFilesCount = comparisonResult.ObsoleteFileCount;
        OnProgressUpdated();

        // Apply cleanup mode based on profile settings
        if(currentProfile.CleanupMode == CleanupMode.DeleteObsolete) {
            // Delete obsolete files
            foreach(var obsoleteFile in comparisonResult.ObsoleteFiles) {
                cancellationToken.ThrowIfCancellationRequested();

                // TODO: Delete file from server
                // await ftpClient.DeleteFileAsync(obsoleteFile, cancellationToken);

                // Update progress
                state.ObsoleteFilesDeleted++;
                OnProgressUpdated();
            }

            // Identify and delete empty directories
            var emptyDirectories = fileComparisonService.IdentifyEmptyDirectories(
                comparisonResult.ObsoleteFiles,
                remoteFiles);

            foreach(var emptyDir in emptyDirectories) {
                cancellationToken.ThrowIfCancellationRequested();

                // TODO: Delete directory from server
                // await ftpClient.DeleteDirectoryAsync(emptyDir, cancellationToken);
            }
        } else if(currentProfile.CleanupMode == CleanupMode.DeleteAll) {
            // Delete all files before uploading (implemented in pre-upload stage)
            // This mode is handled before the upload stage
        }
    }

    /// <summary>
    /// Stage 8: Delete app_offline.htm.
    /// </summary>
    private async Task DeleteAppOfflineAsync(DeploymentOptions options, CancellationToken cancellationToken) {
        if(ftpClient == null) {
            throw new InvalidOperationException("FTP client not initialized. Connect to server first.");
        }

        // TODO: Delete app_offline.htm from server
        // await ftpClient.DeleteFileAsync("app_offline.htm", cancellationToken);

        await Task.CompletedTask;
    }

    /// <summary>
    /// Stage 9: Record deployment history.
    /// </summary>
    private Task RecordHistoryAsync(DeploymentOptions options, CancellationToken cancellationToken) {
        // TODO: Implement deployment history recording
        // This will save deployment results to history
        return Task.CompletedTask;
    }

    /// <summary>
    /// Completes the deployment.
    /// </summary>
    private void CompleteDeployment(DeploymentStage finalStage, string? errorMessage = null, Exception? exception = null) {
        state.CurrentStage = finalStage;
        state.CompletedAt = DateTime.UtcNow;
        state.ErrorMessage = errorMessage;
        state.Exception = exception;
        OnStageChanged(finalStage);
    }

    /// <summary>
    /// Raises the StageChanged event.
    /// </summary>
    private void OnStageChanged(DeploymentStage stage) {
        StageChanged?.Invoke(this, new DeploymentStageChangedEventArgs(stage, state));
    }

    /// <summary>
    /// Raises the ProgressUpdated event.
    /// </summary>
    private void OnProgressUpdated() {
        ProgressUpdated?.Invoke(this, new DeploymentProgressEventArgs(state));
    }
}

/// <summary>
/// Event args for deployment stage changes.
/// </summary>
public class DeploymentStageChangedEventArgs : EventArgs {
    /// <summary>
    /// Gets the new deployment stage.
    /// </summary>
    public DeploymentStage Stage { get; }

    /// <summary>
    /// Gets the deployment state.
    /// </summary>
    public DeploymentState State { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="DeploymentStageChangedEventArgs"/> class.
    /// </summary>
    public DeploymentStageChangedEventArgs(DeploymentStage stage, DeploymentState state) {
        Stage = stage;
        State = state;
    }
}

/// <summary>
/// Event args for deployment progress updates.
/// </summary>
public class DeploymentProgressEventArgs : EventArgs {
    /// <summary>
    /// Gets the deployment state.
    /// </summary>
    public DeploymentState State { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="DeploymentProgressEventArgs"/> class.
    /// </summary>
    public DeploymentProgressEventArgs(DeploymentState state) {
        State = state;
    }
}

/// <summary>
/// Options for deployment execution.
/// </summary>
public class DeploymentOptions {
    /// <summary>
    /// Gets or sets the profile name to use for deployment.
    /// </summary>
    public string? ProfileName { get; set; }

    /// <summary>
    /// Gets or sets the project path to deploy.
    /// </summary>
    public string? ProjectPath { get; set; }

    /// <summary>
    /// Gets or sets the target server host.
    /// </summary>
    public string? TargetHost { get; set; }

    /// <summary>
    /// Gets or sets whether to use app_offline.htm during deployment.
    /// </summary>
    public bool UseAppOffline { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to enable cleanup mode (delete obsolete files).
    /// </summary>
    public bool CleanupMode { get; set; }

    /// <summary>
    /// Gets or sets whether to skip user confirmation prompts.
    /// </summary>
    public bool SkipConfirmation { get; set; }

    /// <summary>
    /// Gets or sets whether to skip connection validation.
    /// </summary>
    public bool SkipConnectionTest { get; set; }

    /// <summary>
    /// Gets or sets whether this is a dry-run (no actual changes).
    /// </summary>
    public bool DryRun { get; set; }

    /// <summary>
    /// Gets or sets the build configuration (e.g., "Release").
    /// </summary>
    public string BuildConfiguration { get; set; } = "Release";

    /// <summary>
    /// Gets or sets the maximum number of concurrent uploads.
    /// </summary>
    public int MaxConcurrentUploads { get; set; } = 4;
}
