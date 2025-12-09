using System.Text.Json;
using FTPSheep.Core.Interfaces;
using FTPSheep.Core.Models;
using FTPSheep.Core.Utils;

namespace FTPSheep.Core.Services;

/// <summary>
/// JSON file-based implementation of deployment history storage.
/// </summary>
public sealed class JsonDeploymentHistoryService : IDeploymentHistoryService {
    private readonly string historyFilePath;
    private readonly SemaphoreSlim fileLock = new(1, 1);
    private const int MaxHistoryEntries = 1000;

    /// <summary>
    /// Initializes a new instance of the <see cref="JsonDeploymentHistoryService"/> class.
    /// </summary>
    public JsonDeploymentHistoryService() {
        var appDataPath = PathResolver.GetApplicationDataPath();
        historyFilePath = Path.Combine(appDataPath, "deployment-history.json");
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="JsonDeploymentHistoryService"/> class.
    /// </summary>
    /// <param name="historyFilePath">Custom path for the history file.</param>
    public JsonDeploymentHistoryService(string historyFilePath) {
        this.historyFilePath = historyFilePath;
    }

    /// <inheritdoc />
    public async Task AddEntryAsync(DeploymentHistoryEntry entry, CancellationToken cancellationToken = default) {
        if(entry == null) {
            throw new ArgumentNullException(nameof(entry));
        }

        await fileLock.WaitAsync(cancellationToken);
        try {
            var entries = await LoadEntriesAsync(cancellationToken);
            entries.Insert(0, entry); // Add to beginning (most recent first)

            // Trim to max entries
            if(entries.Count > MaxHistoryEntries) {
                entries = entries.Take(MaxHistoryEntries).ToList();
            }

            await SaveEntriesAsync(entries, cancellationToken);
        } finally {
            fileLock.Release();
        }
    }

    /// <inheritdoc />
    public async Task<List<DeploymentHistoryEntry>> GetRecentEntriesAsync(
        int count = 10,
        CancellationToken cancellationToken = default) {
        var entries = await LoadEntriesAsync(cancellationToken);
        return entries
            .OrderByDescending(e => e.Timestamp)
            .Take(count)
            .ToList();
    }

    /// <inheritdoc />
    public async Task<List<DeploymentHistoryEntry>> GetProfileEntriesAsync(
        string profileName,
        int count = 10,
        CancellationToken cancellationToken = default) {
        if(string.IsNullOrWhiteSpace(profileName)) {
            throw new ArgumentException("Profile name cannot be null or empty.", nameof(profileName));
        }

        var entries = await LoadEntriesAsync(cancellationToken);
        return entries
            .Where(e => e.ProfileName.Equals(profileName, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(e => e.Timestamp)
            .Take(count)
            .ToList();
    }

    /// <inheritdoc />
    public async Task<List<DeploymentHistoryEntry>> GetEntriesByDateRangeAsync(
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default) {
        var entries = await LoadEntriesAsync(cancellationToken);
        return entries
            .Where(e => e.Timestamp >= from && e.Timestamp <= to)
            .ToList();
    }

    /// <inheritdoc />
    public async Task ClearHistoryAsync(CancellationToken cancellationToken = default) {
        await fileLock.WaitAsync(cancellationToken);
        try {
            if(File.Exists(historyFilePath)) {
                File.Delete(historyFilePath);
            }
        } finally {
            fileLock.Release();
        }
    }

    private async Task<List<DeploymentHistoryEntry>> LoadEntriesAsync(CancellationToken cancellationToken) {
        if(!File.Exists(historyFilePath)) {
            return [];
        }

        try {
            var json = await File.ReadAllTextAsync(historyFilePath, cancellationToken);
            var options = new JsonSerializerOptions {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            var entries = JsonSerializer.Deserialize<List<DeploymentHistoryEntry>>(json, options);
            return entries ?? [];
        } catch(JsonException) {
            // If file is corrupted, return empty list
            return [];
        }
    }

    private async Task SaveEntriesAsync(List<DeploymentHistoryEntry> entries, CancellationToken cancellationToken) {
        var directory = Path.GetDirectoryName(historyFilePath);
        if(!string.IsNullOrEmpty(directory) && !Directory.Exists(directory)) {
            Directory.CreateDirectory(directory);
        }

        var options = new JsonSerializerOptions {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        var json = JsonSerializer.Serialize(entries, options);

        // Atomic write using temp file
        var tempFile = historyFilePath + ".tmp";
        await File.WriteAllTextAsync(tempFile, json, cancellationToken);
        File.Move(tempFile, historyFilePath, true);
    }
}
