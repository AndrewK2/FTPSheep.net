using FTPSheep.Core.Logging;
using FTPSheep.Utilities.Logging;
using Microsoft.Extensions.Logging;

namespace FTPSheep.Tests.Logging;

public class ColoredConsoleLoggerTests {
    [Fact]
    public void Constructor_SetsProperties() {
        // Act
        var logger = new ColoredConsoleLogger("TestCategory", enableColors: true, minLevel: LogLevel.Information);

        // Assert
        Assert.NotNull(logger);
        Assert.True(logger.IsEnabled(LogLevel.Information));
        Assert.False(logger.IsEnabled(LogLevel.Debug));
    }

    [Fact]
    public void IsEnabled_RespectsMinLevel() {
        // Arrange
        var logger = new ColoredConsoleLogger("TestCategory", minLevel: LogLevel.Warning);

        // Act & Assert
        Assert.False(logger.IsEnabled(LogLevel.Trace));
        Assert.False(logger.IsEnabled(LogLevel.Debug));
        Assert.False(logger.IsEnabled(LogLevel.Information));
        Assert.True(logger.IsEnabled(LogLevel.Warning));
        Assert.True(logger.IsEnabled(LogLevel.Error));
        Assert.True(logger.IsEnabled(LogLevel.Critical));
    }

    [Fact]
    public void Log_WithBelowMinLevel_DoesNotWrite() {
        // Arrange
        var logger = new ColoredConsoleLogger("TestCategory", minLevel: LogLevel.Warning);
        var originalOut = Console.Out;
        using var writer = new StringWriter();
        Console.SetOut(writer);

        try {
            // Act
            logger.LogDebug("This should not be logged");
            logger.LogInformation("This should also not be logged");

            // Assert
            var output = writer.ToString();
            Assert.Empty(output);
        } finally {
            Console.SetOut(originalOut);
        }
    }

    [Fact]
    public void Log_WithValidLevel_WritesToConsole() {
        // Arrange
        var logger = new ColoredConsoleLogger("TestCategory", enableColors: false, minLevel: LogLevel.Information);
        var originalOut = Console.Out;
        using var writer = new StringWriter();
        Console.SetOut(writer);

        try {
            // Act
            logger.LogInformation("Test message");

            // Assert
            var output = writer.ToString();
            Assert.Contains("INFO", output);
            Assert.Contains("Test message", output);
        } finally {
            Console.SetOut(originalOut);
        }
    }

    [Fact]
    public void Log_WithException_IncludesExceptionDetails() {
        // Arrange
        var logger = new ColoredConsoleLogger("TestCategory", enableColors: false);
        var originalOut = Console.Out;
        using var writer = new StringWriter();
        Console.SetOut(writer);

        try {
            // Act
            var exception = new InvalidOperationException("Test exception");
            logger.LogException(exception, "An error occurred");

            // Assert
            var output = writer.ToString();
            Assert.Contains("An error occurred", output);
            Assert.Contains("Test exception", output);
        } finally {
            Console.SetOut(originalOut);
        }
    }

    [Fact]
    public void Log_IncludesTimestamp() {
        // Arrange
        var logger = new ColoredConsoleLogger("TestCategory", enableColors: false);
        var originalOut = Console.Out;
        using var writer = new StringWriter();
        Console.SetOut(writer);

        try {
            // Act
            logger.LogInformation("Test message");

            // Assert
            var output = writer.ToString();
            // Check for timestamp pattern [HH:mm:ss]
            Assert.Matches(@"\[\d{2}:\d{2}:\d{2}\]", output);
        } finally {
            Console.SetOut(originalOut);
        }
    }

    [Fact]
    public void BeginScope_ReturnsNull() {
        // Arrange
        var logger = new ColoredConsoleLogger("TestCategory");

        // Act
        var scope = logger.BeginScope("test");

        // Assert
        Assert.Null(scope);
    }

    [Theory]
    [InlineData(LogLevel.Trace)]
    [InlineData(LogLevel.Debug)]
    [InlineData(LogLevel.Information)]
    [InlineData(LogLevel.Warning)]
    [InlineData(LogLevel.Error)]
    [InlineData(LogLevel.Critical)]
    public void Log_AllLogLevels_WritesCorrectPrefix(LogLevel logLevel) {
        // Arrange
        var logger = new ColoredConsoleLogger("TestCategory", enableColors: false, minLevel: LogLevel.Trace);
        var originalOut = Console.Out;
        using var writer = new StringWriter();
        Console.SetOut(writer);

        try {
            // Act
            logger.Log(logLevel, "Test message for {Level}", logLevel);

            // Assert
            var output = writer.ToString();
            Assert.NotEmpty(output);
            Assert.Contains("Test message", output);
        } finally {
            Console.SetOut(originalOut);
        }
    }
}
