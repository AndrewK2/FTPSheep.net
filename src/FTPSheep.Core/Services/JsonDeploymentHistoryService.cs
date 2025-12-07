using System.Text.Json;
using FTPSheep.Core.Interfaces;
using FTPSheep.Core.Models;
using FTPSheep.Core.Utils;

namespace FTPSheep.Core.Services;

/// <summary>
/// JSON file-based implementation of deployment history storage.
/// </summary>
public sealed class JsonDeploymentHistoryService : IDeploymentHistoryService
{
    private readonly string _historyFilePath;
    private readonly SemaphoreSlim _fileLock = new(1, 1);
    private const int MaxHistoryEntries = 1000;

    /// <summary>
    /// Initializes a new instance of the <see cref="JsonDeploymentHistoryService"/> class.
    /// </summary>
    public JsonDeploymentHistoryService()
    {
        var appDataPath = PathResolver.GetApplicationDataPath();
        _historyFilePath = Path.Combine(appDataPath, "deployment-history.json");
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="JsonDeploymentHistoryService"/> class.
    /// </summary>
    /// <param name="historyFilePath">Custom path for the history file.</param>
    public JsonDeploymentHistoryService(string historyFilePath)
    {
        _historyFilePath = historyFilePath;
    }

    /// <inheritdoc />
    public async Task AddEntryAsync(DeploymentHistoryEntry entry, CancellationToken cancellationToken = default)
    {
        if (entry == null)
        {
            throw new ArgumentNullException(nameof(entry));
        }

        await _fileLock.WaitAsync(cancellationToken);
        try
        {
            var entries = await LoadEntriesAsync(cancellationToken);
            entries.Insert(0, entry); // Add to beginning (most recent first)

            // Trim to max entries
            if (entries.Count > MaxHistoryEntries)
            {
                entries = entries.Take(MaxHistoryEntries).ToList();
            }

            await SaveEntriesAsync(entries, cancellationToken);
        }
        finally
        {
            _fileLock.Release();
        }
    }

    /// <inheritdoc />
    public async Task<List<DeploymentHistoryEntry>> GetRecentEntriesAsync(
        int count = 10,
        CancellationToken cancellationToken = default)
    {
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
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(profileName))
        {
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
        CancellationToken cancellationToken = default)
    {
        var entries = await LoadEntriesAsync(cancellationToken);
        return entries
            .Where(e => e.Timestamp >= from && e.Timestamp <= to)
            .ToList();
    }

    /// <inheritdoc />
    public async Task ClearHistoryAsync(CancellationToken cancellationToken = default)
    {
        await _fileLock.WaitAsync(cancellationToken);
        try
        {
            if (File.Exists(_historyFilePath))
            {
                File.Delete(_historyFilePath);
            }
        }
        finally
        {
            _fileLock.Release();
        }
    }

    private async Task<List<DeploymentHistoryEntry>> LoadEntriesAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(_historyFilePath))
        {
            return new List<DeploymentHistoryEntry>();
        }

        try
        {
            var json = await File.ReadAllTextAsync(_historyFilePath, cancellationToken);
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            var entries = JsonSerializer.Deserialize<List<DeploymentHistoryEntry>>(json, options);
            return entries ?? new List<DeploymentHistoryEntry>();
        }
        catch (JsonException)
        {
            // If file is corrupted, return empty list
            return new List<DeploymentHistoryEntry>();
        }
    }

    private async Task SaveEntriesAsync(List<DeploymentHistoryEntry> entries, CancellationToken cancellationToken)
    {
        var directory = Path.GetDirectoryName(_historyFilePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        var json = JsonSerializer.Serialize(entries, options);

        // Atomic write using temp file
        var tempFile = _historyFilePath + ".tmp";
        await File.WriteAllTextAsync(tempFile, json, cancellationToken);
        File.Move(tempFile, _historyFilePath, true);
    }
}
