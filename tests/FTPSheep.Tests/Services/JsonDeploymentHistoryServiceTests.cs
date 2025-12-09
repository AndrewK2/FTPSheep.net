using FTPSheep.Core.Models;
using FTPSheep.Core.Services;

namespace FTPSheep.Tests.Services;

public class JsonDeploymentHistoryServiceTests : IDisposable {
    private readonly string _testHistoryFile;
    private readonly JsonDeploymentHistoryService _service;

    public JsonDeploymentHistoryServiceTests() {
        _testHistoryFile = Path.Combine(Path.GetTempPath(), $"test-history-{Guid.NewGuid()}.json");
        _service = new JsonDeploymentHistoryService(_testHistoryFile);
    }

    public void Dispose() {
        if(File.Exists(_testHistoryFile)) {
            File.Delete(_testHistoryFile);
        }
    }

    [Fact]
    public async Task AddEntryAsync_ValidEntry_AddsToHistory() {
        // Arrange
        var entry = new DeploymentHistoryEntry {
            ProfileName = "test-profile",
            ServerHost = "ftp.example.com",
            Success = true,
            FilesUploaded = 10,
            TotalBytes = 1024,
            DurationSeconds = 5.5
        };

        // Act
        await _service.AddEntryAsync(entry);

        // Assert
        var entries = await _service.GetRecentEntriesAsync(10);
        Assert.Single(entries);
        Assert.Equal("test-profile", entries[0].ProfileName);
        Assert.Equal("ftp.example.com", entries[0].ServerHost);
        Assert.True(entries[0].Success);
    }

    [Fact]
    public async Task GetRecentEntriesAsync_MultipleEntries_ReturnsInDescendingOrder() {
        // Arrange
        for(int i = 0; i < 5; i++) {
            var entry = new DeploymentHistoryEntry {
                ProfileName = $"profile-{i}",
                ServerHost = "ftp.example.com",
                Success = true,
                Timestamp = DateTime.UtcNow.AddMinutes(-i)
            };
            await _service.AddEntryAsync(entry);
        }

        // Act
        var entries = await _service.GetRecentEntriesAsync(10);

        // Assert
        Assert.Equal(5, entries.Count);
        Assert.Equal("profile-0", entries[0].ProfileName); // Most recent first
        Assert.Equal("profile-4", entries[4].ProfileName); // Oldest last
    }

    [Fact]
    public async Task GetRecentEntriesAsync_WithCount_ReturnsLimitedEntries() {
        // Arrange
        for(int i = 0; i < 10; i++) {
            var entry = new DeploymentHistoryEntry {
                ProfileName = $"profile-{i}",
                ServerHost = "ftp.example.com",
                Success = true
            };
            await _service.AddEntryAsync(entry);
        }

        // Act
        var entries = await _service.GetRecentEntriesAsync(5);

        // Assert
        Assert.Equal(5, entries.Count);
    }

    [Fact]
    public async Task GetProfileEntriesAsync_FiltersByProfileName() {
        // Arrange
        await _service.AddEntryAsync(new DeploymentHistoryEntry {
            ProfileName = "profile-a",
            ServerHost = "ftp.example.com",
            Success = true
        });
        await _service.AddEntryAsync(new DeploymentHistoryEntry {
            ProfileName = "profile-b",
            ServerHost = "ftp.example.com",
            Success = true
        });
        await _service.AddEntryAsync(new DeploymentHistoryEntry {
            ProfileName = "profile-a",
            ServerHost = "ftp.example.com",
            Success = false
        });

        // Act
        var entries = await _service.GetProfileEntriesAsync("profile-a");

        // Assert
        Assert.Equal(2, entries.Count);
        Assert.All(entries, e => Assert.Equal("profile-a", e.ProfileName));
    }

    [Fact]
    public async Task GetEntriesByDateRangeAsync_FiltersByDate() {
        // Arrange
        var now = DateTime.UtcNow;
        await _service.AddEntryAsync(new DeploymentHistoryEntry {
            ProfileName = "test",
            ServerHost = "ftp.example.com",
            Success = true,
            Timestamp = now.AddDays(-5)
        });
        await _service.AddEntryAsync(new DeploymentHistoryEntry {
            ProfileName = "test",
            ServerHost = "ftp.example.com",
            Success = true,
            Timestamp = now.AddDays(-2)
        });
        await _service.AddEntryAsync(new DeploymentHistoryEntry {
            ProfileName = "test",
            ServerHost = "ftp.example.com",
            Success = true,
            Timestamp = now
        });

        // Act
        var entries = await _service.GetEntriesByDateRangeAsync(now.AddDays(-3), now.AddDays(1));

        // Assert
        Assert.Equal(2, entries.Count);
    }

    [Fact]
    public async Task ClearHistoryAsync_RemovesAllEntries() {
        // Arrange
        await _service.AddEntryAsync(new DeploymentHistoryEntry {
            ProfileName = "test",
            ServerHost = "ftp.example.com",
            Success = true
        });

        // Act
        await _service.ClearHistoryAsync();

        // Assert
        var entries = await _service.GetRecentEntriesAsync(10);
        Assert.Empty(entries);
    }

    [Fact]
    public async Task AddEntryAsync_WithNullEntry_ThrowsArgumentNullException() {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _service.AddEntryAsync(null!));
    }

    [Fact]
    public async Task GetProfileEntriesAsync_WithEmptyProfileName_ThrowsArgumentException() {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _service.GetProfileEntriesAsync(""));
    }

    [Fact]
    public void DeploymentHistoryEntry_FromResult_CreatesCorrectEntry() {
        // Arrange
        var startTime = DateTime.UtcNow;
        var result = new DeploymentResult {
            Success = true,
            StartTime = startTime,
            EndTime = startTime.AddSeconds(10),
            FilesUploaded = 5,
            TotalBytes = 2048,
            SizeUploaded = 2048,
            ErrorMessages = new List<string> { "Error 1" },
            WarningMessages = new List<string> { "Warning 1" }
        };

        // Act
        var entry = DeploymentHistoryEntry.FromResult(
            "test-profile",
            "ftp.example.com",
            result,
            "Release");

        // Assert
        Assert.Equal("test-profile", entry.ProfileName);
        Assert.Equal("ftp.example.com", entry.ServerHost);
        Assert.True(entry.Success);
        Assert.Equal(10.0, entry.DurationSeconds);
        Assert.Equal(5, entry.FilesUploaded);
        Assert.Equal(2048, entry.TotalBytes);
        Assert.Equal(204.8, entry.AverageSpeedBytesPerSecond); // Calculated from TotalBytes / Duration
        Assert.Single(entry.ErrorMessages);
        Assert.Single(entry.WarningMessages);
        Assert.Equal("Release", entry.BuildConfiguration);
    }
}
