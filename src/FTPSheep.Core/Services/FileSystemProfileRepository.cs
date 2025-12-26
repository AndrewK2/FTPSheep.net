using System.Text.Json;
using System.Text.Json.Serialization;
using FTPSheep.Core.Interfaces;
using FTPSheep.Core.Models;
using FTPSheep.Utilities;
using FTPSheep.Utilities.Exceptions;
using Microsoft.Extensions.Logging;

namespace FTPSheep.Core.Services;

public class FileSystemProfileRepository(ILogger<FileSystemProfileRepository> logger) : IProfileRepository {
    private readonly JsonSerializerOptions jsonOptions = new() {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() }
    };

    public async Task SaveAsync(string filePath, DeploymentProfile profile, CancellationToken cancellationToken = default) {
        try {
            var json = JsonSerializer.Serialize(profile, jsonOptions);

            logger.LogTrace("Profile JSON:\n{p}", json);
            logger.LogDebug("Writing profile to: {p}", filePath);

            await File.WriteAllTextAsync(filePath, json, cancellationToken);
        } catch(Exception ex) {
            throw "Failed to save profile"
                .ToException(ex)
                .Add("Path", filePath);
        }
    }

    public async Task<DeploymentProfile> LoadFromPathAsync(string filePath, CancellationToken cancellationToken = default) {
        string? json = null;
        try {
            json = await File.ReadAllTextAsync(filePath, cancellationToken);

            return JsonUtils.DeserializeExpectedObject<DeploymentProfile>(json, jsonOptions);
        } catch(Exception ex) {
            throw "Failed to load profile by path: \"{0}\""
                .F(filePath)
                .ToException(ex)
                .Add("Path", filePath)
                .Add("JSON length", json?.Length)
                .Add("JSON", json?.Truncate(1024));
        }
    }

    public Task<List<string>> ListProfileNamesAsync(CancellationToken cancellationToken = default) {
        throw new NotImplementedException();
    }

    public Task<bool> DeleteAsync(string filePath, CancellationToken cancellationToken = default) {
        throw new NotImplementedException();
    }

    public Task<bool> ExistsAsync(string filePath, CancellationToken cancellationToken = default) {
        throw new NotImplementedException();
    }
}
