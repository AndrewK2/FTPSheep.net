using System.Text.Json;
using System.Text.Json.Serialization;
using FTPSheep.Core.Exceptions;
using FTPSheep.Core.Interfaces;
using FTPSheep.Core.Models;
using FTPSheep.Core.Utils;
using Microsoft.Extensions.Logging;

namespace FTPSheep.Core.Services;

/// <summary>
/// JSON-based implementation of the profile repository.
/// </summary>
public sealed class JsonProfileRepository : IProfileRepository
{
    private readonly ILogger<JsonProfileRepository> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="JsonProfileRepository"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    public JsonProfileRepository(ILogger<JsonProfileRepository> logger)
    {
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters = { new JsonStringEnumConverter() }
        };
    }

    /// <inheritdoc/>
    public async Task SaveAsync(DeploymentProfile profile, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(profile.Name))
        {
            throw new ProfileStorageException("Profile name cannot be empty.");
        }

        if (!PathResolver.ValidateProfileName(profile.Name, out var errors))
        {
            throw new ProfileStorageException($"Invalid profile name: {string.Join(", ", errors)}");
        }

        try
        {
            PathResolver.EnsureDirectoriesExist();

            var filePath = PathResolver.GetProfileFilePath(profile.Name);
            var tempPath = filePath + ".tmp";

            _logger.LogDebug("Saving profile '{ProfileName}' to {FilePath}", profile.Name, filePath);

            // Write to temp file first (atomic write)
            var json = JsonSerializer.Serialize(profile, _jsonOptions);
            await File.WriteAllTextAsync(tempPath, json, cancellationToken);

            // Move temp file to final location
            File.Move(tempPath, filePath, overwrite: true);

            _logger.LogInformation("Successfully saved profile '{ProfileName}'", profile.Name);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to serialize profile '{ProfileName}'", profile.Name);
            throw new ProfileStorageException($"Failed to serialize profile '{profile.Name}'.", profile.Name, ex);
        }
        catch (IOException ex)
        {
            _logger.LogError(ex, "I/O error while saving profile '{ProfileName}'", profile.Name);
            throw new ProfileStorageException($"Failed to save profile '{profile.Name}' due to I/O error.", profile.Name, ex);
        }
    }

    /// <inheritdoc/>
    public async Task<DeploymentProfile?> LoadAsync(string profileName, CancellationToken cancellationToken = default)
    {
        var filePath = PathResolver.GetProfileFilePath(profileName);

        if (!File.Exists(filePath))
        {
            _logger.LogDebug("Profile '{ProfileName}' not found at {FilePath}", profileName, filePath);
            return null;
        }

        try
        {
            _logger.LogDebug("Loading profile '{ProfileName}' from {FilePath}", profileName, filePath);

            var json = await File.ReadAllTextAsync(filePath, cancellationToken);
            var profile = JsonSerializer.Deserialize<DeploymentProfile>(json, _jsonOptions);

            if (profile == null)
            {
                throw new ProfileStorageException($"Deserialized profile '{profileName}' was null.", profileName);
            }

            _logger.LogInformation("Successfully loaded profile '{ProfileName}'", profileName);
            return profile;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize profile '{ProfileName}' from {FilePath}", profileName, filePath);
            throw new ProfileStorageException($"Profile '{profileName}' file is corrupted or invalid.", profileName, ex);
        }
        catch (IOException ex)
        {
            _logger.LogError(ex, "I/O error while loading profile '{ProfileName}'", profileName);
            throw new ProfileStorageException($"Failed to load profile '{profileName}' due to I/O error.", profileName, ex);
        }
    }

    /// <inheritdoc/>
    public async Task<DeploymentProfile> LoadFromPathAsync(string filePath, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(filePath))
        {
            _logger.LogError("Profile file not found: {FilePath}", filePath);
            throw new FileNotFoundException($"Profile file not found: {filePath}", filePath);
        }

        try
        {
            _logger.LogDebug("Loading profile from {FilePath}", filePath);

            var json = await File.ReadAllTextAsync(filePath, cancellationToken);
            var profile = JsonSerializer.Deserialize<DeploymentProfile>(json, _jsonOptions);

            if (profile == null)
            {
                throw new ProfileStorageException($"Deserialized profile from '{filePath}' was null.");
            }

            _logger.LogInformation("Successfully loaded profile from {FilePath}", filePath);
            return profile;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize profile from {FilePath}", filePath);
            throw new ProfileStorageException($"Profile file '{filePath}' is corrupted or invalid.", ex);
        }
        catch (IOException ex)
        {
            _logger.LogError(ex, "I/O error while loading profile from {FilePath}", filePath);
            throw new ProfileStorageException($"Failed to load profile from '{filePath}' due to I/O error.", ex);
        }
    }

    /// <inheritdoc/>
    public Task<List<string>> ListProfileNamesAsync(CancellationToken cancellationToken = default)
    {
        var profilesPath = PathResolver.GetProfilesDirectoryPath();

        if (!Directory.Exists(profilesPath))
        {
            _logger.LogDebug("Profiles directory does not exist: {ProfilesPath}", profilesPath);
            return Task.FromResult(new List<string>());
        }

        try
        {
            var files = Directory.GetFiles(profilesPath, "*.json");
            var profileNames = files
                .Select(Path.GetFileNameWithoutExtension)
                .Where(name => !string.IsNullOrEmpty(name))
                .Cast<string>() // Cast to non-nullable string after filtering
                .OrderBy(name => name)
                .ToList();

            _logger.LogDebug("Found {Count} profiles", profileNames.Count);
            return Task.FromResult(profileNames);
        }
        catch (IOException ex)
        {
            _logger.LogError(ex, "I/O error while listing profiles");
            throw new ProfileStorageException("Failed to list profiles due to I/O error.", ex);
        }
    }

    /// <inheritdoc/>
    public Task<bool> DeleteAsync(string profileName, CancellationToken cancellationToken = default)
    {
        var filePath = PathResolver.GetProfileFilePath(profileName);

        if (!File.Exists(filePath))
        {
            _logger.LogDebug("Profile '{ProfileName}' not found for deletion", profileName);
            return Task.FromResult(false);
        }

        try
        {
            File.Delete(filePath);
            _logger.LogInformation("Successfully deleted profile '{ProfileName}'", profileName);
            return Task.FromResult(true);
        }
        catch (IOException ex)
        {
            _logger.LogError(ex, "I/O error while deleting profile '{ProfileName}'", profileName);
            throw new ProfileStorageException($"Failed to delete profile '{profileName}' due to I/O error.", profileName, ex);
        }
    }

    /// <inheritdoc/>
    public Task<bool> ExistsAsync(string profileName, CancellationToken cancellationToken = default)
    {
        var filePath = PathResolver.GetProfileFilePath(profileName);
        var exists = File.Exists(filePath);

        _logger.LogDebug("Profile '{ProfileName}' exists: {Exists}", profileName, exists);
        return Task.FromResult(exists);
    }

    /// <inheritdoc/>
    public string GetProfilePath(string profileName)
    {
        return PathResolver.GetProfileFilePath(profileName);
    }
}
