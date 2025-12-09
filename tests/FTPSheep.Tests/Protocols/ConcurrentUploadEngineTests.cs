using FTPSheep.Protocols.Models;
using FTPSheep.Protocols.Services;
using Xunit;

namespace FTPSheep.Tests.Protocols;

public class ConcurrentUploadEngineTests {
    #region Constructor Tests

    [Fact]
    public void Constructor_ValidParameters_InitializesCorrectly() {
        // Arrange
        var config = CreateTestConfig();

        // Act
        using var engine = new ConcurrentUploadEngine(config, maxConcurrency: 4, maxRetries: 3);

        // Assert
        Assert.NotNull(engine);
    }

    [Fact]
    public void Constructor_NullConfig_ThrowsArgumentNullException() {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new ConcurrentUploadEngine(null!));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(21)]
    public void Constructor_InvalidMaxConcurrency_ThrowsArgumentException(int maxConcurrency) {
        // Arrange
        var config = CreateTestConfig();

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            new ConcurrentUploadEngine(config, maxConcurrency: maxConcurrency));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(11)]
    public void Constructor_InvalidMaxRetries_ThrowsArgumentException(int maxRetries) {
        // Arrange
        var config = CreateTestConfig();

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            new ConcurrentUploadEngine(config, maxRetries: maxRetries));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(4)]
    [InlineData(8)]
    [InlineData(20)]
    public void Constructor_ValidMaxConcurrency_Succeeds(int maxConcurrency) {
        // Arrange
        var config = CreateTestConfig();

        // Act
        using var engine = new ConcurrentUploadEngine(config, maxConcurrency: maxConcurrency);

        // Assert
        Assert.NotNull(engine);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(3)]
    [InlineData(10)]
    public void Constructor_ValidMaxRetries_Succeeds(int maxRetries) {
        // Arrange
        var config = CreateTestConfig();

        // Act
        using var engine = new ConcurrentUploadEngine(config, maxRetries: maxRetries);

        // Assert
        Assert.NotNull(engine);
    }

    #endregion

    #region UploadFilesAsync Tests

    [Fact]
    public async Task UploadFilesAsync_EmptyTaskList_ReturnsEmptyResults() {
        // Arrange
        var config = CreateTestConfig();
        using var engine = new ConcurrentUploadEngine(config);
        var tasks = new List<UploadTask>();

        // Act
        var results = await engine.UploadFilesAsync(tasks, CancellationToken.None);

        // Assert
        Assert.Empty(results);
    }

    [Fact]
    public async Task UploadFilesAsync_CancellationRequested_ThrowsTaskCanceledException() {
        // Arrange
        var config = CreateTestConfig();
        using var engine = new ConcurrentUploadEngine(config);
        var tasks = CreateTestTasks(10);
        var cts = new CancellationTokenSource();

        // Act
        cts.Cancel(); // Cancel immediately

        // Assert
        // When cancelled before starting, should throw TaskCanceledException
        await Assert.ThrowsAsync<TaskCanceledException>(async () =>
            await engine.UploadFilesAsync(tasks, cts.Token));
    }

    #endregion

    #region Event Tests

    [Fact]
    public void ProgressUpdated_Event_CanBeSubscribed() {
        // Arrange
        var config = CreateTestConfig();
        using var engine = new ConcurrentUploadEngine(config);
        var eventRaised = false;

        // Act
        engine.ProgressUpdated += (sender, progress) => { eventRaised = true; };

        // Assert
        Assert.NotNull(engine);
        // Note: Event subscription succeeds even if we can't raise it without FTP server
    }

    [Fact]
    public void FileUploaded_Event_CanBeSubscribed() {
        // Arrange
        var config = CreateTestConfig();
        using var engine = new ConcurrentUploadEngine(config);
        var eventRaised = false;

        // Act
        engine.FileUploaded += (sender, result) => { eventRaised = true; };

        // Assert
        Assert.NotNull(engine);
        // Note: Event subscription succeeds even if we can't raise it without FTP server
    }

    #endregion

    #region Dispose Tests

    [Fact]
    public void Dispose_MultipleCalls_DoesNotThrow() {
        // Arrange
        var config = CreateTestConfig();
        var engine = new ConcurrentUploadEngine(config);

        // Act & Assert
        engine.Dispose();
        engine.Dispose(); // Should not throw
    }

    #endregion

    #region Task Priority and Ordering Tests

    [Fact]
    public void UploadTasks_OrderedByPriority_SmallFilesFirst() {
        // Arrange
        var tasks = new List<UploadTask> {
            new() { LocalPath = "large.txt", RemotePath = "/large.txt", FileSize = 1024 * 1024, Priority = 0 },
            new() { LocalPath = "small.txt", RemotePath = "/small.txt", FileSize = 1024, Priority = 0 },
            new() { LocalPath = "medium.txt", RemotePath = "/medium.txt", FileSize = 1024 * 100, Priority = 0 }
        };

        // Act
        var ordered = tasks.OrderBy(t => t.Priority).ThenBy(t => t.FileSize).ToList();

        // Assert
        Assert.Equal("small.txt", ordered[0].LocalPath);
        Assert.Equal("medium.txt", ordered[1].LocalPath);
        Assert.Equal("large.txt", ordered[2].LocalPath);
    }

    [Fact]
    public void UploadTasks_OrderedByPriority_HighPriorityFirst() {
        // Arrange
        var tasks = new List<UploadTask> {
            new() { LocalPath = "low.txt", RemotePath = "/low.txt", FileSize = 1024, Priority = 10 },
            new() { LocalPath = "high.txt", RemotePath = "/high.txt", FileSize = 1024, Priority = 0 },
            new() { LocalPath = "medium.txt", RemotePath = "/medium.txt", FileSize = 1024, Priority = 5 }
        };

        // Act
        var ordered = tasks.OrderBy(t => t.Priority).ThenBy(t => t.FileSize).ToList();

        // Assert
        Assert.Equal("high.txt", ordered[0].LocalPath);
        Assert.Equal("medium.txt", ordered[1].LocalPath);
        Assert.Equal("low.txt", ordered[2].LocalPath);
    }

    #endregion

    #region Helper Methods

    private static FtpConnectionConfig CreateTestConfig() {
        return new FtpConnectionConfig {
            Host = "ftp.test.com",
            Port = 21,
            Username = "testuser",
            Password = "testpass"
        };
    }

    private static List<UploadTask> CreateTestTasks(int count) {
        var tasks = new List<UploadTask>();

        for(var i = 0; i < count; i++) {
            tasks.Add(new UploadTask {
                LocalPath = $"C:\\temp\\file{i}.txt",
                RemotePath = $"/remote/file{i}.txt",
                FileSize = 1024 * (i + 1)
            });
        }

        return tasks;
    }

    #endregion
}
