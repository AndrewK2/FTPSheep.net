using FTPSheep.Core.Exceptions;
using FTPSheep.Core.Interfaces;
using FTPSheep.Core.Models;
using FTPSheep.Core.Utils;
using Microsoft.Extensions.Logging;

namespace FTPSheep.Core.Services;

/// <summary>
/// High-level service for managing deployment profiles with validation and credential management.
/// </summary>
public sealed class ProfileService : IProfileService {
    private readonly IProfileRepository _repository;
    private readonly IConfigurationService _configService;
    private readonly ICredentialStore _credentialStore;
    private readonly ILogger<ProfileService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProfileService"/> class.
    /// </summary>
    /// <param name="repository">The profile repository.</param>
    /// <param name="configService">The configuration service.</param>
    /// <param name="credentialStore">The credential store.</param>
    /// <param name="logger">The logger instance.</param>
    public ProfileService(
        IProfileRepository repository,
        IConfigurationService configService,
        ICredentialStore credentialStore,
        ILogger<ProfileService> logger) {
        _repository = repository;
        _configService = configService;
        _credentialStore = credentialStore;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task CreateProfileAsync(DeploymentProfile profile, CancellationToken cancellationToken = default) {
        _logger.LogInformation("Creating profile '{ProfileName}'", profile.Name);

        // Validate the profile
        var validationResult = ValidateProfile(profile);
        if(!validationResult.IsValid) {
            throw new ProfileValidationException(profile.Name, validationResult.Errors);
        }

        // Check if profile already exists
        if(await _repository.ExistsAsync(profile.Name, cancellationToken)) {
            throw new ProfileAlreadyExistsException(profile.Name);
        }

        // Save the profile
        await _repository.SaveAsync(profile, cancellationToken);

        // Save credentials if provided
        if(!string.IsNullOrWhiteSpace(profile.Username) && !string.IsNullOrWhiteSpace(profile.Password)) {
            await _credentialStore.SaveCredentialsAsync(
                profile.Name,
                profile.Username,
                profile.Password,
                cancellationToken);

            _logger.LogDebug("Saved credentials for profile '{ProfileName}'", profile.Name);
        }

        _logger.LogInformation("Successfully created profile '{ProfileName}'", profile.Name);
    }

    /// <inheritdoc/>
    public async Task<DeploymentProfile> LoadProfileAsync(string profileNameOrPath, CancellationToken cancellationToken = default) {
        _logger.LogInformation("Loading profile '{ProfileNameOrPath}'", profileNameOrPath);

        DeploymentProfile profile;

        // Determine if it's a path or a name
        if(PathResolver.IsAbsolutePath(profileNameOrPath)) {
            // Load from file path
            profile = await _repository.LoadFromPathAsync(profileNameOrPath, cancellationToken);
        } else {
            // Load by name
            profile = await _repository.LoadAsync(profileNameOrPath, cancellationToken)
                ?? throw new ProfileNotFoundException(profileNameOrPath);
        }

        // Apply global configuration defaults
        await _configService.ApplyDefaultsAsync(profile, cancellationToken);

        // Load credentials if available
        var credentials = await _credentialStore.LoadCredentialsAsync(profile.Name, cancellationToken);
        if(credentials != null) {
            profile.Username = credentials.Username;
            profile.Password = credentials.Password;
            _logger.LogDebug("Loaded credentials for profile '{ProfileName}'", profile.Name);
        }

        // Normalize port if needed
        profile.Connection.NormalizePort();

        // Validate the loaded profile
        var validationResult = ValidateProfile(profile);
        if(!validationResult.IsValid) {
            _logger.LogWarning("Loaded profile '{ProfileName}' has validation errors: {Errors}",
                profile.Name, string.Join("; ", validationResult.Errors));
        }

        if(validationResult.Warnings.Count > 0) {
            _logger.LogWarning("Loaded profile '{ProfileName}' has validation warnings: {Warnings}",
                profile.Name, string.Join("; ", validationResult.Warnings));
        }

        _logger.LogInformation("Successfully loaded profile '{ProfileName}'", profile.Name);
        return profile;
    }

    /// <inheritdoc/>
    public async Task UpdateProfileAsync(DeploymentProfile profile, CancellationToken cancellationToken = default) {
        _logger.LogInformation("Updating profile '{ProfileName}'", profile.Name);

        // Validate the profile
        var validationResult = ValidateProfile(profile);
        if(!validationResult.IsValid) {
            throw new ProfileValidationException(profile.Name, validationResult.Errors);
        }

        // Ensure profile exists
        if(!await _repository.ExistsAsync(profile.Name, cancellationToken)) {
            throw new ProfileNotFoundException(profile.Name);
        }

        // Save the updated profile
        await _repository.SaveAsync(profile, cancellationToken);

        // Update credentials if provided
        if(!string.IsNullOrWhiteSpace(profile.Username) && !string.IsNullOrWhiteSpace(profile.Password)) {
            await _credentialStore.SaveCredentialsAsync(
                profile.Name,
                profile.Username,
                profile.Password,
                cancellationToken);

            _logger.LogDebug("Updated credentials for profile '{ProfileName}'", profile.Name);
        }

        _logger.LogInformation("Successfully updated profile '{ProfileName}'", profile.Name);
    }

    /// <inheritdoc/>
    public async Task<bool> DeleteProfileAsync(string profileName, CancellationToken cancellationToken = default) {
        _logger.LogInformation("Deleting profile '{ProfileName}'", profileName);

        // Delete the profile file
        var deleted = await _repository.DeleteAsync(profileName, cancellationToken);

        if(deleted) {
            // Delete associated credentials
            await _credentialStore.DeleteCredentialsAsync(profileName, cancellationToken);
            _logger.LogInformation("Successfully deleted profile '{ProfileName}'", profileName);
        } else {
            _logger.LogWarning("Profile '{ProfileName}' not found for deletion", profileName);
        }

        return deleted;
    }

    /// <inheritdoc/>
    public async Task<List<ProfileSummary>> ListProfilesAsync(CancellationToken cancellationToken = default) {
        _logger.LogDebug("Listing all profiles");

        var profileNames = await _repository.ListProfileNamesAsync(cancellationToken);
        var summaries = new List<ProfileSummary>();

        foreach(var name in profileNames) {
            try {
                var profile = await _repository.LoadAsync(name, cancellationToken);
                if(profile == null) {
                    continue;
                }

                var filePath = _repository.GetProfilePath(name);
                var fileInfo = new FileInfo(filePath);
                var hasCredentials = await _credentialStore.HasCredentialsAsync(name, cancellationToken);

                summaries.Add(new ProfileSummary {
                    Name = profile.Name,
                    ConnectionString = profile.Connection.GetConnectionString(),
                    ProjectPath = profile.ProjectPath,
                    RemotePath = profile.RemotePath,
                    HasCredentials = hasCredentials,
                    LastModified = fileInfo.Exists ? fileInfo.LastWriteTime : DateTime.MinValue,
                    FilePath = filePath
                });
            } catch(Exception ex) {
                _logger.LogWarning(ex, "Failed to load profile '{ProfileName}' for listing", name);
            }
        }

        _logger.LogDebug("Found {Count} profiles", summaries.Count);
        return summaries;
    }

    /// <inheritdoc/>
    public ValidationResult ValidateProfile(DeploymentProfile profile) {
        var result = new ValidationResult();

        // Validate profile name
        if(string.IsNullOrWhiteSpace(profile.Name)) {
            result.AddError("Profile name cannot be empty.");
        } else if(!PathResolver.ValidateProfileName(profile.Name, out var nameErrors)) {
            foreach(var error in nameErrors) {
                result.AddError(error);
            }
        }

        // Validate connection settings
        if(!profile.Connection.Validate(out var connectionErrors)) {
            foreach(var error in connectionErrors) {
                result.AddError($"Connection: {error}");
            }
        }

        // Validate build configuration
        if(!profile.Build.Validate(out var buildErrors)) {
            foreach(var error in buildErrors) {
                result.AddError($"Build: {error}");
            }
        }

        // Validate concurrency (1-32)
        if(profile.Concurrency < 1 || profile.Concurrency > 32) {
            result.AddError("Concurrency must be between 1 and 32.");
        }

        // Validate retry count (0-10)
        if(profile.RetryCount < 0 || profile.RetryCount > 10) {
            result.AddError("Retry count must be between 0 and 10.");
        }

        // Validate remote path
        if(string.IsNullOrWhiteSpace(profile.RemotePath)) {
            result.AddError("Remote path cannot be empty.");
        }

        // Validate project path (warning only if not found)
        if(!string.IsNullOrWhiteSpace(profile.ProjectPath)) {
            if(!File.Exists(profile.ProjectPath)) {
                result.AddWarning($"Project file not found: {profile.ProjectPath}");
            }
        }

        return result;
    }
}
