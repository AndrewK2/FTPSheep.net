using FluentFTP;
using FTPSheep.BuildTools.Models;
using FTPSheep.BuildTools.Services;
using FTPSheep.Core.Models;
using FTPSheep.Protocols.Models;
using FTPSheep.Protocols.Services;
using FTPSheep.Utilities;
using FTPSheep.Utilities.Exceptions;
using FTPSheep.Utilities.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using IFtpClient = FTPSheep.Protocols.Interfaces.IFtpClient;

namespace FTPSheep.Core.Services;

/// <summary>
/// Coordinates the entire deployment workflow from build to upload to finalization.
/// </summary>
public class DeploymentCoordinator {
    private readonly DeploymentState state;
    private readonly AppOfflineManager appOfflineManager;
    private readonly ExclusionPatternMatcher exclusionMatcher;
    private readonly FileComparisonService fileComparisonService;
    private readonly ProfileService? profileService;
    private readonly BuildService? buildService;
    private readonly JsonDeploymentHistoryService? historyService;
    private readonly ILogger logger;
    private CancellationTokenSource? cancellationTokenSource;

    // Deployment context (populated during execution)
    private IFtpClient? ftpClient;
    private ConcurrentUploadEngine? uploadEngine;
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
    /// <param name="profileService">The profile service (optional).</param>
    /// <param name="buildService">The build service (optional).</param>
    /// <param name="historyService">The deployment history service (optional).</param>
    /// <param name="appOfflineManager">The app_offline.htm manager (optional).</param>
    /// <param name="exclusionMatcher">The exclusion pattern matcher (optional).</param>
    /// <param name="logger"></param>
    public DeploymentCoordinator(ProfileService? profileService = null,
        BuildService? buildService = null,
        JsonDeploymentHistoryService? historyService = null,
        AppOfflineManager? appOfflineManager = null,
        ExclusionPatternMatcher? exclusionMatcher = null, ILogger? logger = null) {
        state = new DeploymentState();
        this.profileService = profileService;
        this.buildService = buildService;
        this.historyService = historyService;
        this.logger = logger ?? NullLogger.Instance;
        this.appOfflineManager = appOfflineManager ?? new AppOfflineManager();
        this.exclusionMatcher = exclusionMatcher ?? new ExclusionPatternMatcher();
        fileComparisonService = new FileComparisonService(this.exclusionMatcher);
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

            var nex = errorMessage
                .ToException(ex)
                .Add("Profile", options.ProfileName)
                .Add("Profile Path", options.ProjectPath);

            logger.LogException(nex);

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
    private async Task LoadProfileAsync(DeploymentOptions options, CancellationToken cancellationToken) {
        // Use pre-loaded profile if available
        if(options.Profile != null) {
            currentProfile = options.Profile;
            state.ProfileName = currentProfile.Name;
            state.TargetHost = currentProfile.Connection.Host;
            return;
        }

        // Otherwise, load from service
        if(profileService == null) {
            throw new InvalidOperationException("ProfileService is required when profile is not pre-loaded.");
        }

        if(string.IsNullOrWhiteSpace(options.ProfileName)) {
            throw new ArgumentException("Profile name is required when profile is not pre-loaded.", nameof(options));
        }

        // Load profile from storage
        currentProfile = await profileService.LoadProfileAsync(options.ProfileName, cancellationToken);

        if(currentProfile == null) {
            throw new InvalidOperationException($"Profile '{options.ProfileName}' not found.");
        }

        // Update state with profile information
        state.ProfileName = currentProfile.Name;
        state.TargetHost = currentProfile.Connection.Host;
    }

    /// <summary>
    /// Stage 2: Build and publish project.
    /// </summary>
    private async Task BuildProjectAsync(DeploymentOptions options, CancellationToken cancellationToken) {
        // Use pre-loaded publish output if available
        if(options.PublishOutput != null) {
            publishedFiles = options.PublishOutput.Files;
            publishOutputPath = options.PublishOutput.RootPath;

            if(publishedFiles == null || publishedFiles.Count == 0) {
                throw new InvalidOperationException("Pre-loaded publish output contains no files.");
            }
            return;
        }

        // Otherwise, build via service
        if(buildService == null) {
            throw new InvalidOperationException("BuildService is required when publish output is not pre-loaded.");
        }

        if(currentProfile == null) {
            throw new InvalidOperationException("Profile must be loaded before building.");
        }

        // Determine project path (from options or profile)
        var projectPath = options.ProjectPath ?? currentProfile.ProjectPath;

        if(string.IsNullOrWhiteSpace(projectPath)) {
            throw new InvalidOperationException("Project path is required for building.");
        }

        // Determine output path (use temp directory)
        var outputPath = Path.Combine(Path.GetTempPath(), "FTPSheep", Guid.NewGuid().ToString());

        // Build and publish the project
        var buildResult = await buildService.PublishAsync(
            projectPath,
            outputPath,
            currentProfile.Build.Configuration ?? "Release",
            cancellationToken);

        if(!buildResult.Success) {
            var errorMessage = buildResult.HasErrors
                ? string.Join(Environment.NewLine, buildResult.Errors)
                : buildResult.ErrorOutput;
            throw new InvalidOperationException(
                $"Build failed: {errorMessage}");
        }

        // Store publish output path and scan for files
        publishOutputPath = buildResult.OutputPath;

        if(string.IsNullOrWhiteSpace(publishOutputPath)) {
            throw new InvalidOperationException("Build did not produce an output path.");
        }

        // Scan published files
        var scanner = new PublishOutputScanner();
        var publishOutput = await scanner.ScanPublishOutputAsync(
            publishOutputPath,
            exclusionPatterns: null,
            validateOutput: true,
            cancellationToken);
        publishedFiles = publishOutput.Files;

        if(publishedFiles == null || publishedFiles.Count == 0) {
            throw new InvalidOperationException("No files found in publish output directory.");
        }
    }

    /// <summary>
    /// Stage 3: Connect to server and validate connection.
    /// </summary>
    private async Task ConnectToServerAsync(DeploymentOptions options, CancellationToken cancellationToken) {
        try {
            if(currentProfile == null) {
                throw new InvalidOperationException("Profile must be loaded before connecting.");
            }

            // Create FTP connection configuration from profile
            var conn = currentProfile.Connection;

            var ftpConfig = new FtpConnectionConfig {
                Host = conn.Host,
                Port = conn.Port,
                Username = currentProfile.Username ?? string.Empty,
                Password = currentProfile.Password ?? string.Empty,
                RemoteRootPath = currentProfile.RemotePath,
                EncryptionMode = conn.UseSsl
                    ? FtpEncryptionMode.Explicit
                    : FtpEncryptionMode.None
            };

            logger.LogDebug("Connecting to {0}:{1}", conn.Host, conn.Port);

            // Create FTP client and connect
            ftpClient = FtpClientFactory.CreateClient(ftpConfig);
            await ftpClient.ConnectAsync(cancellationToken);

            // Test connection and write permissions
            var canWrite = await ftpClient.TestConnectionAsync(
                currentProfile.RemotePath,
                cancellationToken);

            if(!canWrite) {
                throw new InvalidOperationException(
                    $"Cannot write to remote path: {currentProfile.RemotePath}");
            }

            // Initialize concurrent upload engine
            var maxConcurrency = currentProfile.Concurrency > 0
                ? currentProfile.Concurrency
                : 4; // default

            uploadEngine = new ConcurrentUploadEngine(ftpConfig, maxConcurrency, maxRetries: currentProfile.RetryCount);
        } catch(Exception ex) {
            throw "Failed to connect to server \"{0}\""
                .F(currentProfile?.Connection.Host)
                .ToException(ex);
        }
    }

    /// <summary>
    /// Stage 4: Display pre-deployment summary and wait for confirmation.
    /// </summary>
    private Task DisplayPreDeploymentSummaryAsync(DeploymentOptions options, CancellationToken cancellationToken) {
        // Summary information is already tracked in state
        // This stage is primarily for displaying to the user (handled by CLI layer)
        // The coordinator just ensures state is populated correctly

        if(publishedFiles == null || publishedFiles.Count == 0) {
            throw new InvalidOperationException("No files to deploy.");
        }

        // State is already updated with file counts and sizes
        // CLI will display this information to the user
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
            // Upload to server root as app_offline.htm
            await ftpClient.UploadFileAsync(tempPath, "app_offline.htm",
                overwrite: true, createRemoteDir: false, cancellationToken);
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
        if(uploadEngine == null) {
            throw new InvalidOperationException("Upload engine not initialized. Connect to server first.");
        }

        if(publishedFiles == null || publishedFiles.Count == 0) {
            throw new InvalidOperationException("No published files found. Build project first.");
        }

        if(publishOutputPath == null) {
            throw new InvalidOperationException("Publish output path not set. Build project first.");
        }

        // Update state with total files and size
        state.TotalFiles = publishedFiles.Count;
        state.TotalSize = publishedFiles.Sum(f => f.Size);
        OnProgressUpdated();

        // Create upload tasks from published files
        var uploadTasks = publishedFiles.Select(file => {
            // Calculate remote path relative to the remote root
            var relativePath = Path.GetRelativePath(publishOutputPath, file.AbsolutePath);
            var remotePath = relativePath.Replace('\\', '/'); // Normalize to forward slashes for FTP

            return new UploadTask {
                LocalPath = file.AbsolutePath,
                RemotePath = remotePath,
                FileSize = file.Size,
                Priority = 0, // Could be set based on file type or size
                Overwrite = true,
                CreateRemoteDir = true
            };
        }).ToList();

        // Subscribe to upload events for progress tracking
        uploadEngine.ProgressUpdated += OnUploadProgressUpdated;
        uploadEngine.FileUploaded += OnFileUploaded;

        try {
            // Execute concurrent uploads
            var results = await uploadEngine.UploadFilesAsync(uploadTasks, cancellationToken);

            // Update state with final results
            var successfulUploads = results.Count(r => r.Success);
            var failedUploads = results.Count(r => !r.Success);

            if(failedUploads > 0) {
                var failedFiles = results.Where(r => !r.Success)
                    .Select(r => r.Task.RemotePath)
                    .ToList();
                throw new InvalidOperationException(
                    $"Failed to upload {failedUploads} file(s): {string.Join(", ", failedFiles)}");
            }
        } finally {
            // Unsubscribe from events
            uploadEngine.ProgressUpdated -= OnUploadProgressUpdated;
            uploadEngine.FileUploaded -= OnFileUploaded;
        }
    }

    /// <summary>
    /// Handles upload progress updates from the concurrent upload engine.
    /// </summary>
    private void OnUploadProgressUpdated(object? sender, UploadProgress progress) {
        state.FilesUploaded = progress.CompletedFiles;
        state.SizeUploaded = progress.UploadedBytes;
        OnProgressUpdated();
    }

    /// <summary>
    /// Handles individual file upload completion from the concurrent upload engine.
    /// </summary>
    private void OnFileUploaded(object? sender, UploadResult result) {
        // Individual file upload completed - progress is tracked via OnUploadProgressUpdated
        // This can be used for detailed logging if needed
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

        // Delete app_offline.htm from server
        await ftpClient.DeleteFileAsync("app_offline.htm", cancellationToken);
    }

    /// <summary>
    /// Stage 9: Record deployment history.
    /// </summary>
    private async Task RecordHistoryAsync(DeploymentOptions options, CancellationToken cancellationToken) {
        if(historyService == null) {
            // History recording is optional, skip if service not provided
            return;
        }

        if(currentProfile == null) {
            return;
        }

        // Create deployment history record
        TimeSpan duration = (state.CompletedAt.HasValue && state.StartedAt.HasValue)
            ? state.CompletedAt.Value - state.StartedAt.Value
            : TimeSpan.Zero;

        var historyEntry = new DeploymentHistoryEntry {
            Timestamp = DateTime.UtcNow,
            ProfileName = currentProfile.Name,
            ServerHost = currentProfile.Connection.Host,
            FilesUploaded = state.FilesUploaded,
            TotalBytes = state.SizeUploaded,
            DurationSeconds = duration.TotalSeconds,
            Success = true,
            BuildConfiguration = currentProfile.Build.Configuration
        };

        await historyService.AddEntryAsync(historyEntry);
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