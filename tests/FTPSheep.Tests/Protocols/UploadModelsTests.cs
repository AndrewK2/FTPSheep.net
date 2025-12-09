using FluentFTP;
using FTPSheep.Protocols.Models;
using Xunit;

namespace FTPSheep.Tests.Protocols;

public class UploadModelsTests {
    #region UploadTask Tests

    [Fact]
    public void UploadTask_RequiredProperties_CanBeInitialized() {
        // Arrange & Act
        var task = new UploadTask {
            LocalPath = "C:\\temp\\file.txt",
            RemotePath = "/remote/file.txt",
            FileSize = 1024
        };

        // Assert
        Assert.Equal("C:\\temp\\file.txt", task.LocalPath);
        Assert.Equal("/remote/file.txt", task.RemotePath);
        Assert.Equal(1024, task.FileSize);
        Assert.True(task.Overwrite); // Default
        Assert.True(task.CreateRemoteDir); // Default
        Assert.Equal(0, task.Priority); // Default
        Assert.Empty(task.Metadata);
    }

    [Fact]
    public void UploadTask_WithCustomProperties_StoresCorrectly() {
        // Arrange & Act
        var task = new UploadTask {
            LocalPath = "file.txt",
            RemotePath = "/file.txt",
            FileSize = 2048,
            Overwrite = false,
            CreateRemoteDir = false,
            Priority = 10,
            Metadata = new Dictionary<string, object> { { "key", "value" } }
        };

        // Assert
        Assert.False(task.Overwrite);
        Assert.False(task.CreateRemoteDir);
        Assert.Equal(10, task.Priority);
        Assert.Single(task.Metadata);
        Assert.Equal("value", task.Metadata["key"]);
    }

    #endregion

    #region UploadResult Tests

    [Fact]
    public void UploadResult_Success_CreatesCorrectResult() {
        // Arrange
        var task = new UploadTask {
            LocalPath = "local.txt",
            RemotePath = "remote.txt",
            FileSize = 1024
        };
        var startTime = DateTime.UtcNow.AddSeconds(-2);
        var endTime = DateTime.UtcNow;

        // Act
        var result = UploadResult.FromSuccess(task, FtpStatus.Success, startTime, endTime, retryAttempts: 0);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(FtpStatus.Success, result.Status);
        Assert.Null(result.ErrorMessage);
        Assert.Null(result.Exception);
        Assert.Equal(0, result.RetryAttempts);
        Assert.InRange(result.Duration.TotalSeconds, 1.5, 2.5);
        Assert.True(result.BytesPerSecond > 0);
    }

    [Fact]
    public void UploadResult_Failure_CreatesCorrectResult() {
        // Arrange
        var task = new UploadTask {
            LocalPath = "local.txt",
            RemotePath = "remote.txt",
            FileSize = 1024
        };
        var exception = new Exception("Upload failed");
        var startTime = DateTime.UtcNow.AddSeconds(-1);
        var endTime = DateTime.UtcNow;

        // Act
        var result = UploadResult.FromFailure(task, exception, startTime, endTime, retryAttempts: 3);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(FtpStatus.Failed, result.Status);
        Assert.Equal("Upload failed", result.ErrorMessage);
        Assert.Same(exception, result.Exception);
        Assert.Equal(3, result.RetryAttempts);
        Assert.Equal(0, result.BytesPerSecond);
    }

    [Fact]
    public void UploadResult_FormattedSpeed_DisplaysCorrectly() {
        // Arrange
        var task = new UploadTask {
            LocalPath = "local.txt",
            RemotePath = "remote.txt",
            FileSize = 1024 * 1024 * 2 // 2 MB
        };
        var startTime = DateTime.UtcNow.AddSeconds(-1);
        var endTime = DateTime.UtcNow;

        // Act
        var result = UploadResult.FromSuccess(task, FtpStatus.Success, startTime, endTime);

        // Assert
        Assert.Contains("MB/s", result.FormattedSpeed);
    }

    [Fact]
    public void UploadResult_ZeroSpeed_FormatsCorrectly() {
        // Arrange
        var task = new UploadTask {
            LocalPath = "local.txt",
            RemotePath = "remote.txt",
            FileSize = 0
        };
        var startTime = DateTime.UtcNow;
        var endTime = DateTime.UtcNow;

        // Act
        var result = UploadResult.FromSuccess(task, FtpStatus.Success, startTime, endTime);

        // Assert
        Assert.Equal("0 B/s", result.FormattedSpeed);
    }

    #endregion

    #region UploadProgress Tests

    [Fact]
    public void UploadProgress_ProgressPercentage_CalculatesCorrectly() {
        // Arrange & Act
        var progress = new UploadProgress {
            TotalFiles = 100,
            CompletedFiles = 50,
            TotalBytes = 0,
            UploadedBytes = 0,
            StartedAt = DateTime.UtcNow
        };

        // Assert
        Assert.Equal(50.0, progress.ProgressPercentage);
    }

    [Fact]
    public void UploadProgress_ByteProgressPercentage_CalculatesCorrectly() {
        // Arrange & Act
        var progress = new UploadProgress {
            TotalFiles = 0,
            CompletedFiles = 0,
            TotalBytes = 1000,
            UploadedBytes = 250,
            StartedAt = DateTime.UtcNow
        };

        // Assert
        Assert.Equal(25.0, progress.ByteProgressPercentage);
    }

    [Fact]
    public void UploadProgress_ZeroTotal_ReturnsZeroPercentage() {
        // Arrange & Act
        var progress = new UploadProgress {
            TotalFiles = 0,
            CompletedFiles = 0,
            TotalBytes = 0,
            UploadedBytes = 0,
            StartedAt = DateTime.UtcNow
        };

        // Assert
        Assert.Equal(0.0, progress.ProgressPercentage);
        Assert.Equal(0.0, progress.ByteProgressPercentage);
    }

    [Fact]
    public void UploadProgress_IsComplete_ReturnsTrueWhenAllDone() {
        // Arrange & Act
        var progress = new UploadProgress {
            TotalFiles = 10,
            CompletedFiles = 10,
            TotalBytes = 0,
            UploadedBytes = 0,
            StartedAt = DateTime.UtcNow
        };

        // Assert
        Assert.True(progress.IsComplete);
    }

    [Fact]
    public void UploadProgress_IsComplete_ReturnsFalseWhenNotDone() {
        // Arrange & Act
        var progress = new UploadProgress {
            TotalFiles = 10,
            CompletedFiles = 5,
            TotalBytes = 0,
            UploadedBytes = 0,
            StartedAt = DateTime.UtcNow
        };

        // Assert
        Assert.False(progress.IsComplete);
    }

    [Fact]
    public void UploadProgress_ElapsedTime_CalculatesCorrectly() {
        // Arrange
        var startTime = DateTime.UtcNow.AddSeconds(-5);

        // Act
        var progress = new UploadProgress {
            TotalFiles = 0,
            CompletedFiles = 0,
            TotalBytes = 0,
            UploadedBytes = 0,
            StartedAt = startTime
        };

        // Assert
        Assert.InRange(progress.ElapsedTime.TotalSeconds, 4.5, 5.5);
    }

    [Fact]
    public void UploadProgress_FormattedSpeed_DisplaysCorrectUnits() {
        // Test B/s
        var progress1 = new UploadProgress {
            BytesPerSecond = 100,
            TotalFiles = 0,
            TotalBytes = 0,
            StartedAt = DateTime.UtcNow
        };
        Assert.Contains("B/s", progress1.FormattedSpeed);

        // Test KB/s
        var progress2 = new UploadProgress {
            BytesPerSecond = 1024 * 100, // 100 KB/s
            TotalFiles = 0,
            TotalBytes = 0,
            StartedAt = DateTime.UtcNow
        };
        Assert.Contains("KB/s", progress2.FormattedSpeed);

        // Test MB/s
        var progress3 = new UploadProgress {
            BytesPerSecond = 1024 * 1024 * 2, // 2 MB/s
            TotalFiles = 0,
            TotalBytes = 0,
            StartedAt = DateTime.UtcNow
        };
        Assert.Contains("MB/s", progress3.FormattedSpeed);
    }

    [Fact]
    public void UploadProgress_FormattedAverageSpeed_DisplaysCorrectly() {
        // Arrange & Act
        var progress = new UploadProgress {
            AverageBytesPerSecond = 1024 * 500, // 500 KB/s
            TotalFiles = 0,
            TotalBytes = 0,
            StartedAt = DateTime.UtcNow
        };

        // Assert
        Assert.Contains("KB/s", progress.FormattedAverageSpeed);
    }

    [Fact]
    public void UploadProgress_WithEstimatedTime_StoresCorrectly() {
        // Arrange & Act
        var progress = new UploadProgress {
            EstimatedTimeRemaining = TimeSpan.FromMinutes(5),
            TotalFiles = 0,
            TotalBytes = 0,
            StartedAt = DateTime.UtcNow
        };

        // Assert
        Assert.NotNull(progress.EstimatedTimeRemaining);
        Assert.Equal(5, progress.EstimatedTimeRemaining.Value.TotalMinutes);
    }

    [Fact]
    public void UploadProgress_WithoutEstimatedTime_ReturnsNull() {
        // Arrange & Act
        var progress = new UploadProgress {
            EstimatedTimeRemaining = null,
            TotalFiles = 0,
            TotalBytes = 0,
            StartedAt = DateTime.UtcNow
        };

        // Assert
        Assert.Null(progress.EstimatedTimeRemaining);
    }

    [Fact]
    public void UploadProgress_AllProperties_StoreCorrectly() {
        // Arrange & Act
        var progress = new UploadProgress {
            TotalFiles = 100,
            CompletedFiles = 50,
            ActiveUploads = 4,
            PendingFiles = 46,
            SuccessfulUploads = 45,
            FailedUploads = 5,
            TotalBytes = 1024 * 1024 * 100, // 100 MB
            UploadedBytes = 1024 * 1024 * 50, // 50 MB
            BytesPerSecond = 1024 * 1024 * 2, // 2 MB/s
            AverageBytesPerSecond = 1024 * 1024 * 1.5, // 1.5 MB/s
            EstimatedTimeRemaining = TimeSpan.FromMinutes(3),
            StartedAt = DateTime.UtcNow.AddMinutes(-2)
        };

        // Assert
        Assert.Equal(100, progress.TotalFiles);
        Assert.Equal(50, progress.CompletedFiles);
        Assert.Equal(4, progress.ActiveUploads);
        Assert.Equal(46, progress.PendingFiles);
        Assert.Equal(45, progress.SuccessfulUploads);
        Assert.Equal(5, progress.FailedUploads);
        Assert.Equal(1024 * 1024 * 100, progress.TotalBytes);
        Assert.Equal(1024 * 1024 * 50, progress.UploadedBytes);
        Assert.Equal(50.0, progress.ProgressPercentage);
        Assert.Equal(50.0, progress.ByteProgressPercentage);
        Assert.False(progress.IsComplete);
        Assert.InRange(progress.ElapsedTime.TotalMinutes, 1.9, 2.1);
    }

    #endregion
}
