using FTPSheep.Core.Models;
using FTPSheep.Core.Services;

namespace FTPSheep.Tests.Core;

public class DeploymentOrchestrationTests {
    #region DeploymentStage Tests

    [Fact]
    public void DeploymentStage_EnumValues_AreCorrect() {
        Assert.Equal(0, (int)DeploymentStage.NotStarted);
        Assert.Equal(1, (int)DeploymentStage.LoadingProfile);
        Assert.Equal(2, (int)DeploymentStage.BuildingProject);
        Assert.Equal(3, (int)DeploymentStage.ConnectingToServer);
        Assert.Equal(4, (int)DeploymentStage.PreDeploymentSummary);
        Assert.Equal(5, (int)DeploymentStage.UploadingAppOffline);
        Assert.Equal(6, (int)DeploymentStage.UploadingFiles);
        Assert.Equal(7, (int)DeploymentStage.CleaningUpObsoleteFiles);
        Assert.Equal(8, (int)DeploymentStage.DeletingAppOffline);
        Assert.Equal(9, (int)DeploymentStage.RecordingHistory);
        Assert.Equal(10, (int)DeploymentStage.Completed);
        Assert.Equal(11, (int)DeploymentStage.Failed);
        Assert.Equal(12, (int)DeploymentStage.Cancelled);
    }

    #endregion

    #region DeploymentState Tests

    [Fact]
    public void DeploymentState_Constructor_InitializesDefaults() {
        var state = new DeploymentState();

        Assert.NotEqual(Guid.Empty, state.DeploymentId);
        Assert.Equal(DeploymentStage.NotStarted, state.CurrentStage);
        Assert.Null(state.StartedAt);
        Assert.Null(state.CompletedAt);
        Assert.Equal(0, state.TotalFiles);
        Assert.Equal(0, state.FilesUploaded);
        Assert.Equal(0, state.FilesFailed);
        Assert.True(state.CanCancel);
        Assert.False(state.CancellationRequested);
    }

    [Fact]
    public void DeploymentState_ProgressPercentage_CalculatesCorrectly() {
        var state = new DeploymentState {
            TotalFiles = 100,
            FilesUploaded = 50
        };

        Assert.Equal(50.0, state.ProgressPercentage);
    }

    [Fact]
    public void DeploymentState_ProgressPercentage_WithZeroFiles_ReturnsZero() {
        var state = new DeploymentState {
            TotalFiles = 0,
            FilesUploaded = 0
        };

        Assert.Equal(0.0, state.ProgressPercentage);
    }

    [Fact]
    public void DeploymentState_ElapsedTime_CalculatesCorrectly() {
        var startTime = DateTime.UtcNow.AddMinutes(-5);
        var state = new DeploymentState {
            StartedAt = startTime
        };

        var elapsed = state.ElapsedTime;

        Assert.NotNull(elapsed);
        Assert.True(elapsed.Value.TotalMinutes >= 4.9 && elapsed.Value.TotalMinutes <= 5.1);
    }

    [Fact]
    public void DeploymentState_ElapsedTime_WithCompletedAt_UsesCompletedTime() {
        var startTime = DateTime.UtcNow.AddMinutes(-10);
        var completedTime = startTime.AddMinutes(5);
        var state = new DeploymentState {
            StartedAt = startTime,
            CompletedAt = completedTime
        };

        var elapsed = state.ElapsedTime;

        Assert.NotNull(elapsed);
        Assert.Equal(5, Math.Round(elapsed.Value.TotalMinutes));
    }

    [Fact]
    public void DeploymentState_IsInProgress_ReturnsTrueForActiveStages() {
        var state = new DeploymentState { CurrentStage = DeploymentStage.UploadingFiles };
        Assert.True(state.IsInProgress);

        state.CurrentStage = DeploymentStage.BuildingProject;
        Assert.True(state.IsInProgress);
    }

    [Fact]
    public void DeploymentState_IsInProgress_ReturnsFalseForFinalStages() {
        var state = new DeploymentState { CurrentStage = DeploymentStage.Completed };
        Assert.False(state.IsInProgress);

        state.CurrentStage = DeploymentStage.Failed;
        Assert.False(state.IsInProgress);

        state.CurrentStage = DeploymentStage.Cancelled;
        Assert.False(state.IsInProgress);

        state.CurrentStage = DeploymentStage.NotStarted;
        Assert.False(state.IsInProgress);
    }

    [Fact]
    public void DeploymentState_IsCompleted_ReturnsTrueForFinalStages() {
        var state = new DeploymentState { CurrentStage = DeploymentStage.Completed };
        Assert.True(state.IsCompleted);
        Assert.True(state.IsSuccess);
        Assert.False(state.IsFailed);
        Assert.False(state.IsCancelled);
    }

    [Fact]
    public void DeploymentState_IsFailed_ReturnsTrueWhenFailed() {
        var state = new DeploymentState { CurrentStage = DeploymentStage.Failed };
        Assert.True(state.IsCompleted);
        Assert.False(state.IsSuccess);
        Assert.True(state.IsFailed);
        Assert.False(state.IsCancelled);
    }

    [Fact]
    public void DeploymentState_IsCancelled_ReturnsTrueWhenCancelled() {
        var state = new DeploymentState { CurrentStage = DeploymentStage.Cancelled };
        Assert.True(state.IsCompleted);
        Assert.False(state.IsSuccess);
        Assert.False(state.IsFailed);
        Assert.True(state.IsCancelled);
    }

    #endregion

    #region DeploymentResult Tests

    [Fact]
    public void DeploymentResult_Constructor_InitializesDefaults() {
        var result = new DeploymentResult();

        Assert.NotEqual(Guid.Empty, result.DeploymentId);
        Assert.False(result.Success);
        Assert.NotNull(result.ErrorMessages);
        Assert.Empty(result.ErrorMessages);
        Assert.NotNull(result.WarningMessages);
        Assert.Empty(result.WarningMessages);
        Assert.NotNull(result.FailedFiles);
        Assert.Empty(result.FailedFiles);
    }

    [Fact]
    public void DeploymentResult_Duration_CalculatesCorrectly() {
        var startTime = DateTime.UtcNow;
        var endTime = startTime.AddMinutes(5);
        var result = new DeploymentResult {
            StartTime = startTime,
            EndTime = endTime
        };

        Assert.Equal(5, Math.Round(result.Duration!.Value.TotalMinutes));
    }

    [Fact]
    public void DeploymentResult_AverageSpeedBytesPerSecond_CalculatesCorrectly() {
        var startTime = DateTime.UtcNow;
        var endTime = startTime.AddSeconds(10);
        var result = new DeploymentResult {
            StartTime = startTime,
            EndTime = endTime,
            SizeUploaded = 1000
        };

        Assert.Equal(100.0, result.AverageSpeedBytesPerSecond);
    }

    [Fact]
    public void DeploymentResult_AverageSpeedBytesPerSecond_WithNoDuration_ReturnsNull() {
        var result = new DeploymentResult {
            SizeUploaded = 1000
        };

        Assert.Null(result.AverageSpeedBytesPerSecond);
    }

    [Fact]
    public void DeploymentResult_FormattedUploadSpeed_FormatsCorrectly() {
        var startTime = DateTime.UtcNow;
        var endTime = startTime.AddSeconds(10);

        // Test bytes per second
        var result1 = new DeploymentResult {
            StartTime = startTime,
            EndTime = endTime,
            SizeUploaded = 500
        };
        Assert.Contains("B/s", result1.FormattedUploadSpeed);

        // Test kilobytes per second
        var result2 = new DeploymentResult {
            StartTime = startTime,
            EndTime = endTime,
            SizeUploaded = 10240
        };
        Assert.Contains("KB/s", result2.FormattedUploadSpeed);

        // Test megabytes per second
        var result3 = new DeploymentResult {
            StartTime = startTime,
            EndTime = endTime,
            SizeUploaded = 10485760
        };
        Assert.Contains("MB/s", result3.FormattedUploadSpeed);
    }

    [Fact]
    public void DeploymentResult_AddError_AddsErrorAndMarksFailed() {
        var result = new DeploymentResult { Success = true };

        result.AddError("Test error");

        Assert.False(result.Success);
        Assert.Single(result.ErrorMessages);
        Assert.Equal("Test error", result.ErrorMessages[0]);
    }

    [Fact]
    public void DeploymentResult_AddWarning_AddsWarningWithoutAffectingSuccess() {
        var result = new DeploymentResult { Success = true };

        result.AddWarning("Test warning");

        Assert.True(result.Success);
        Assert.Single(result.WarningMessages);
        Assert.Equal("Test warning", result.WarningMessages[0]);
    }

    [Fact]
    public void DeploymentResult_Complete_SetsEndTimeAndFinalStage() {
        var result = new DeploymentResult {
            StartTime = DateTime.UtcNow
        };

        result.Complete(success: true);

        Assert.NotNull(result.EndTime);
        Assert.True(result.Success);
        Assert.Equal(DeploymentStage.Completed, result.FinalStage);
    }

    [Fact]
    public void DeploymentResult_Complete_WithErrors_MarksFailed() {
        var result = new DeploymentResult {
            StartTime = DateTime.UtcNow
        };
        result.AddError("Test error");

        result.Complete(success: true);

        Assert.False(result.Success);
        Assert.Equal(DeploymentStage.Failed, result.FinalStage);
    }

    [Fact]
    public void DeploymentResult_FromSuccess_CreatesSuccessfulResult() {
        var state = new DeploymentState {
            DeploymentId = Guid.NewGuid(),
            StartedAt = DateTime.UtcNow.AddMinutes(-5),
            CompletedAt = DateTime.UtcNow,
            ProfileName = "TestProfile",
            ProjectPath = "C:\\Test\\Project.csproj",
            TargetHost = "ftp.example.com",
            TotalFiles = 100,
            FilesUploaded = 100,
            FilesFailed = 0,
            TotalSize = 1000000,
            SizeUploaded = 1000000,
            ObsoleteFilesDeleted = 5
        };

        var result = DeploymentResult.FromSuccess(state);

        Assert.True(result.Success);
        Assert.Equal(DeploymentStage.Completed, result.FinalStage);
        Assert.Equal(state.DeploymentId, result.DeploymentId);
        Assert.Equal(state.ProfileName, result.ProfileName);
        Assert.Equal(state.ProjectPath, result.ProjectPath);
        Assert.Equal(state.TargetHost, result.TargetHost);
        Assert.Equal(100, result.FilesUploaded);
        Assert.Equal(0, result.FilesFailed);
    }

    [Fact]
    public void DeploymentResult_FromFailure_CreatesFailedResult() {
        var state = new DeploymentState {
            DeploymentId = Guid.NewGuid(),
            StartedAt = DateTime.UtcNow.AddMinutes(-5),
            CompletedAt = DateTime.UtcNow,
            TotalFiles = 100,
            FilesUploaded = 50
        };
        var exception = new Exception("Test exception");

        var result = DeploymentResult.FromFailure(state, "Deployment failed", exception);

        Assert.False(result.Success);
        Assert.Equal(DeploymentStage.Failed, result.FinalStage);
        Assert.Equal(exception, result.Exception);
        Assert.Single(result.ErrorMessages);
        Assert.Equal("Deployment failed", result.ErrorMessages[0]);
        Assert.Equal(50, result.FilesUploaded);
    }

    [Fact]
    public void DeploymentResult_FromCancellation_CreatesCancelledResult() {
        var state = new DeploymentState {
            DeploymentId = Guid.NewGuid(),
            StartedAt = DateTime.UtcNow.AddMinutes(-5),
            CompletedAt = DateTime.UtcNow,
            TotalFiles = 100,
            FilesUploaded = 30
        };

        var result = DeploymentResult.FromCancellation(state);

        Assert.False(result.Success);
        Assert.Equal(DeploymentStage.Cancelled, result.FinalStage);
        Assert.True(result.WasCancelled);
        Assert.Single(result.ErrorMessages);
        Assert.Contains("cancelled by user", result.ErrorMessages[0].ToLowerInvariant());
        Assert.Equal(30, result.FilesUploaded);
    }

    #endregion

    #region DeploymentCoordinator Tests

    [Fact]
    public void DeploymentCoordinator_Constructor_InitializesState() {
        var coordinator = new DeploymentCoordinator();

        Assert.NotNull(coordinator.State);
        Assert.Equal(DeploymentStage.NotStarted, coordinator.State.CurrentStage);
    }

    [Fact]
    public async Task DeploymentCoordinator_ExecuteDeploymentAsync_WithNullOptions_ThrowsArgumentNullException() {
        var coordinator = new DeploymentCoordinator();

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => coordinator.ExecuteDeploymentAsync(null!));
    }

    [Fact]
    public async Task DeploymentCoordinator_ExecuteDeploymentAsync_InitializesState() {
        var coordinator = new DeploymentCoordinator();
        var options = new DeploymentOptions {
            ProfileName = "TestProfile",
            ProjectPath = "C:\\Test\\Project.csproj",
            TargetHost = "ftp.example.com"
        };

        var result = await coordinator.ExecuteDeploymentAsync(options);

        Assert.NotNull(coordinator.State.StartedAt);
        Assert.Equal("TestProfile", coordinator.State.ProfileName);
        Assert.Equal("C:\\Test\\Project.csproj", coordinator.State.ProjectPath);
        Assert.Equal("ftp.example.com", coordinator.State.TargetHost);
    }

    [Fact]
    public async Task DeploymentCoordinator_ExecuteDeploymentAsync_CompletesSuccessfully() {
        var coordinator = new DeploymentCoordinator();
        var options = new DeploymentOptions {
            UseAppOffline = false,
            CleanupMode = false
        };

        var result = await coordinator.ExecuteDeploymentAsync(options);

        Assert.True(result.Success);
        Assert.Equal(DeploymentStage.Completed, result.FinalStage);
        Assert.Equal(DeploymentStage.Completed, coordinator.State.CurrentStage);
    }

    [Fact]
    public async Task DeploymentCoordinator_ExecuteDeploymentAsync_RaisesStageChangedEvents() {
        var coordinator = new DeploymentCoordinator();
        var options = new DeploymentOptions {
            UseAppOffline = false,
            CleanupMode = false
        };
        var stageChanges = new List<DeploymentStage>();

        coordinator.StageChanged += (sender, args) => stageChanges.Add(args.Stage);

        await coordinator.ExecuteDeploymentAsync(options);

        Assert.Contains(DeploymentStage.LoadingProfile, stageChanges);
        Assert.Contains(DeploymentStage.BuildingProject, stageChanges);
        Assert.Contains(DeploymentStage.ConnectingToServer, stageChanges);
        Assert.Contains(DeploymentStage.Completed, stageChanges);
    }

    [Fact]
    public async Task DeploymentCoordinator_ExecuteDeploymentAsync_WithAppOffline_IncludesAppOfflineStages() {
        var coordinator = new DeploymentCoordinator();
        var options = new DeploymentOptions {
            UseAppOffline = true,
            CleanupMode = false
        };
        var stageChanges = new List<DeploymentStage>();

        coordinator.StageChanged += (sender, args) => stageChanges.Add(args.Stage);

        await coordinator.ExecuteDeploymentAsync(options);

        Assert.Contains(DeploymentStage.UploadingAppOffline, stageChanges);
        Assert.Contains(DeploymentStage.DeletingAppOffline, stageChanges);
    }

    [Fact]
    public async Task DeploymentCoordinator_ExecuteDeploymentAsync_WithCleanupMode_IncludesCleanupStage() {
        var coordinator = new DeploymentCoordinator();
        var options = new DeploymentOptions {
            UseAppOffline = false,
            CleanupMode = true
        };
        var stageChanges = new List<DeploymentStage>();

        coordinator.StageChanged += (sender, args) => stageChanges.Add(args.Stage);

        await coordinator.ExecuteDeploymentAsync(options);

        Assert.Contains(DeploymentStage.CleaningUpObsoleteFiles, stageChanges);
    }

    [Fact]
    public async Task DeploymentCoordinator_ExecuteDeploymentAsync_WithCancellation_ReturnsCancelledResult() {
        var coordinator = new DeploymentCoordinator();
        var options = new DeploymentOptions();
        var cts = new CancellationTokenSource();

        // Cancel immediately
        cts.Cancel();

        var result = await coordinator.ExecuteDeploymentAsync(options, cts.Token);

        Assert.False(result.Success);
        Assert.Equal(DeploymentStage.Cancelled, result.FinalStage);
        Assert.True(result.WasCancelled);
    }

    [Fact]
    public void DeploymentCoordinator_CancelDeployment_CanBeCalledSafely() {
        var coordinator = new DeploymentCoordinator();

        // Should not throw even when not in progress
        coordinator.CancelDeployment();

        // Verify state is still valid
        Assert.NotNull(coordinator.State);
        Assert.Equal(DeploymentStage.NotStarted, coordinator.State.CurrentStage);
    }

    #endregion

    #region DeploymentOptions Tests

    [Fact]
    public void DeploymentOptions_Constructor_SetsDefaults() {
        var options = new DeploymentOptions();

        Assert.True(options.UseAppOffline);
        Assert.False(options.CleanupMode);
        Assert.False(options.SkipConfirmation);
        Assert.False(options.SkipConnectionTest);
        Assert.False(options.DryRun);
        Assert.Equal("Release", options.BuildConfiguration);
        Assert.Equal(4, options.MaxConcurrentUploads);
    }

    #endregion
}
