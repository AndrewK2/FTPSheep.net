using System.Text.Json;
using System.Text.Json.Serialization;
using FTPSheep.Core.Exceptions;
using FTPSheep.Core.Interfaces;
using FTPSheep.Core.Models;
using FTPSheep.Core.Utils;
using Microsoft.Extensions.Logging;

namespace FTPSheep.Core.Services;

/// <summary>
/// JSON-based implementation of the configuration service.
/// </summary>
public sealed class JsonConfigurationService : IConfigurationService {
    private readonly ILogger<JsonConfigurationService> logger;
    private readonly JsonSerializerOptions jsonOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="JsonConfigurationService"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    public JsonConfigurationService(ILogger<JsonConfigurationService> logger) {
        this.logger = logger;
        jsonOptions = new JsonSerializerOptions {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters = { new JsonStringEnumConverter() }
        };
    }

    /// <inheritdoc/>
    public async Task<GlobalConfiguration> LoadConfigurationAsync(CancellationToken cancellationToken = default) {
        var configPath = PathResolver.GetGlobalConfigPath();

        if(!File.Exists(configPath)) {
            logger.LogInformation("Global configuration not found, creating default configuration");
            var defaultConfig = GlobalConfiguration.CreateDefault();
            await SaveConfigurationAsync(defaultConfig, cancellationToken);
            return defaultConfig;
        }

        try {
            logger.LogDebug("Loading global configuration from {ConfigPath}", configPath);

            var json = await File.ReadAllTextAsync(configPath, cancellationToken);
            var config = JsonSerializer.Deserialize<GlobalConfiguration>(json, jsonOptions);

            if(config == null) {
                logger.LogWarning("Deserialized configuration was null, creating default");
                return GlobalConfiguration.CreateDefault();
            }

            logger.LogInformation("Successfully loaded global configuration");
            return config;
        } catch(JsonException ex) {
            logger.LogError(ex, "Failed to deserialize global configuration, using default");
            throw new ConfigurationException("Global configuration file is corrupted or invalid.", ex);
        } catch(IOException ex) {
            logger.LogError(ex, "I/O error while loading global configuration");
            throw new ConfigurationException("Failed to load global configuration due to I/O error.", ex);
        }
    }

    /// <inheritdoc/>
    public async Task SaveConfigurationAsync(GlobalConfiguration configuration, CancellationToken cancellationToken = default) {
        try {
            PathResolver.EnsureDirectoriesExist();

            var configPath = PathResolver.GetGlobalConfigPath();
            var tempPath = configPath + ".tmp";

            logger.LogDebug("Saving global configuration to {ConfigPath}", configPath);

            // Write to temp file first (atomic write)
            var json = JsonSerializer.Serialize(configuration, jsonOptions);
            await File.WriteAllTextAsync(tempPath, json, cancellationToken);

            // Move temp file to final location
            File.Move(tempPath, configPath, overwrite: true);

            logger.LogInformation("Successfully saved global configuration");
        } catch(JsonException ex) {
            logger.LogError(ex, "Failed to serialize global configuration");
            throw new ConfigurationException("Failed to serialize global configuration.", ex);
        } catch(IOException ex) {
            logger.LogError(ex, "I/O error while saving global configuration");
            throw new ConfigurationException("Failed to save global configuration due to I/O error.", ex);
        }
    }

    /// <inheritdoc/>
    public async Task ApplyDefaultsAsync(DeploymentProfile profile, CancellationToken cancellationToken = default) {
        var config = await LoadConfigurationAsync(cancellationToken);

        logger.LogDebug("Applying global defaults to profile '{ProfileName}'", profile.Name);

        // Apply numeric defaults (0 means "use default from global config")
        if(profile.Concurrency == 0 || profile.Concurrency == 4) // 4 is the hardcoded default
        {
            profile.Concurrency = config.DefaultConcurrency;
        }

        if(profile.RetryCount == 0 || profile.RetryCount == 3) // 3 is the hardcoded default
        {
            profile.RetryCount = config.DefaultRetryCount;
        }

        if(profile.Connection.TimeoutSeconds == 0 || profile.Connection.TimeoutSeconds == 30) // 30 is the hardcoded default
        {
            profile.Connection.TimeoutSeconds = config.DefaultTimeoutSeconds;
        }

        // Apply build configuration default
        if(string.IsNullOrWhiteSpace(profile.Build.Configuration) || profile.Build.Configuration == "Release") {
            if(!string.IsNullOrWhiteSpace(config.DefaultBuildConfiguration)) {
                profile.Build.Configuration = config.DefaultBuildConfiguration;
            }
        }

        // Merge exclusion patterns (additive - add global patterns that aren't already in the profile)
        if(config.DefaultExclusionPatterns.Count > 0) {
            foreach(var pattern in config.DefaultExclusionPatterns) {
                if(!profile.ExclusionPatterns.Contains(pattern)) {
                    profile.ExclusionPatterns.Add(pattern);
                }
            }
        }

        logger.LogDebug("Applied global defaults to profile '{ProfileName}'", profile.Name);
    }
}
