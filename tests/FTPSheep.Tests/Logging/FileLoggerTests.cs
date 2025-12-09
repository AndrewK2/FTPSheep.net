using FTPSheep.Core.Logging;
using Microsoft.Extensions.Logging;

namespace FTPSheep.Tests.Logging;

public class FileLoggerTests : IDisposable {
    private readonly string testLogDirectory = Path.Combine(Path.GetTempPath(), $"ftpsheep-logs-{Guid.NewGuid()}");

    public void Dispose() {
        if(Directory.Exists(testLogDirectory)) {
            Directory.Delete(testLogDirectory, true);
        }
    }

    [Fact]
    public void Log_Information_WritesToFile() {
        // Arrange
        using var logger = new FileLogger("TestCategory", testLogDirectory, minLevel: LogLevel.Information);

        // Act
        logger.LogInformation("Test message");

        // Assert
        var logFiles = Directory.GetFiles(testLogDirectory, "ftpsheep-*.log");
        Assert.Single(logFiles);

        var content = File.ReadAllText(logFiles[0]);
        Assert.Contains("INFO", content);
        Assert.Contains("Test message", content);
        Assert.Contains("TestCategory", content);
    }

    [Fact]
    public void Log_BelowMinLevel_DoesNotWrite() {
        // Arrange
        using var logger = new FileLogger("TestCategory", testLogDirectory, minLevel: LogLevel.Warning);

        // Act
        logger.LogInformation("This should not be logged");
        logger.LogWarning("This should be logged");

        // Assert
        var logFiles = Directory.GetFiles(testLogDirectory, "ftpsheep-*.log");
        Assert.Single(logFiles);

        var content = File.ReadAllText(logFiles[0]);
        Assert.DoesNotContain("should not be logged", content);
        Assert.Contains("should be logged", content);
    }

    [Fact]
    public void Log_WithException_IncludesExceptionDetails() {
        // Arrange
        using var logger = new FileLogger("TestCategory", testLogDirectory);
        var exception = new InvalidOperationException("Test exception");

        // Act
        logger.LogError(exception, "An error occurred");

        // Assert
        var logFiles = Directory.GetFiles(testLogDirectory, "ftpsheep-*.log");
        var content = File.ReadAllText(logFiles[0]);
        Assert.Contains("An error occurred", content);
        Assert.Contains("Test exception", content);
        Assert.Contains("InvalidOperationException", content);
    }

    [Fact]
    public void IsEnabled_RespectsMinLevel() {
        // Arrange
        using var logger = new FileLogger("TestCategory", testLogDirectory, minLevel: LogLevel.Warning);

        // Act & Assert
        Assert.False(logger.IsEnabled(LogLevel.Trace));
        Assert.False(logger.IsEnabled(LogLevel.Debug));
        Assert.False(logger.IsEnabled(LogLevel.Information));
        Assert.True(logger.IsEnabled(LogLevel.Warning));
        Assert.True(logger.IsEnabled(LogLevel.Error));
        Assert.True(logger.IsEnabled(LogLevel.Critical));
    }

    [Fact]
    public void Log_CreatesLogDirectory_IfNotExists() {
        // Arrange
        var nonExistentDir = Path.Combine(testLogDirectory, "subdir");
        Assert.False(Directory.Exists(nonExistentDir));

        // Act
        using var logger = new FileLogger("TestCategory", nonExistentDir);
        logger.LogInformation("Test");

        // Assert
        Assert.True(Directory.Exists(nonExistentDir));
    }

    [Fact]
    public void Log_MultipleMessages_AppendsToSameFile() {
        // Arrange
        using var logger = new FileLogger("TestCategory", testLogDirectory);

        // Act
        logger.LogInformation("Message 1");
        logger.LogInformation("Message 2");
        logger.LogInformation("Message 3");

        // Assert
        var logFiles = Directory.GetFiles(testLogDirectory, "ftpsheep-*.log");
        Assert.Single(logFiles);

        var content = File.ReadAllText(logFiles[0]);
        Assert.Contains("Message 1", content);
        Assert.Contains("Message 2", content);
        Assert.Contains("Message 3", content);
    }

    [Fact]
    public void Log_IncludesTimestamp() {
        // Arrange
        using var logger = new FileLogger("TestCategory", testLogDirectory);

        // Act
        logger.LogInformation("Test message");

        // Assert
        var logFiles = Directory.GetFiles(testLogDirectory, "ftpsheep-*.log");
        var content = File.ReadAllText(logFiles[0]);

        // Check for timestamp pattern [yyyy-MM-dd HH:mm:ss.fff]
        Assert.Matches(@"\[\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}\.\d{3}\]", content);
    }
}
