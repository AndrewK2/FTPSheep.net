using FTPSheep.Core.Exceptions;
using FTPSheep.Core.Interfaces;
using FTPSheep.Core.Models;
using FTPSheep.Core.Utils;
using FTPSheep.Utilities;
using FTPSheep.Utilities.Exceptions;
using Microsoft.Extensions.Logging;

namespace FTPSheep.Core.Services;

/// <summary>
/// High-level service for managing deployment profiles with validation and credential management.
/// </summary>
public sealed class ProfileService : IProfileService {
    private readonly ICredentialStore credentialStore;
    private readonly IProfileRepository profilesRepository;
    private readonly ILogger<ProfileService> logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProfileService"/> class.
    /// </summary>
    /// <param name="credentialStore">The credential store.</param>
    /// <param name="profilesRepository"></param>
    /// <param name="logger">The logger instance.</param>
    public ProfileService(ICredentialStore credentialStore, IProfileRepository profilesRepository, ILogger<ProfileService> logger) {
        this.credentialStore = credentialStore;
        this.profilesRepository = profilesRepository;
        this.logger = logger;
    }

    /// <param name="profileSavePath"></param>
    /// <param name="profile"></param>
    /// <param name="cancellationToken"></param>
    /// <exception cref="ProfileValidationException"></exception>
    public async Task CreateProfileAsync(string profileSavePath, DeploymentProfile profile, CancellationToken cancellationToken = default) {
        logger.LogInformation("Creating profile '{ProfileName}'", profile.Name);

        // Validate the profile
        var validationResult = ValidateProfile(profile);
        if(!validationResult.IsValid) {
            throw new ProfileValidationException(profile.Name, validationResult.Errors);
        }

        await profilesRepository.SaveAsync(profileSavePath, profile, cancellationToken);

        // Save credentials if provided
        if(!string.IsNullOrWhiteSpace(profile.Username) && !string.IsNullOrWhiteSpace(profile.Password)) {
            await credentialStore.SaveCredentialsAsync(profileSavePath, profile.Username, profile.Password, cancellationToken);

            logger.LogDebug("Saved credentials for profile '{ProfileName}'", profile.Name);
        }

        logger.LogInformation("Successfully created profile '{ProfileName}'", profile.Name);
    }

    /// <inheritdoc/>
    public async Task<DeploymentProfile> LoadProfileAsync(string filePath, CancellationToken cancellationToken = default) {
        if(string.IsNullOrWhiteSpace(filePath)) {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(filePath));
        }

        try {
            logger.LogInformation("Loading profile from path: {ProfileNameOrPath}", filePath);

            // Resolve filePath to full absolute path if it's relative
            if(!PathResolver.IsAbsolutePath(filePath)) {
                filePath = Path.GetFullPath(filePath);
                logger.LogDebug("Resolved relative profile path to absolute: {AbsolutePath}", filePath);
            }

            var profile = await profilesRepository.LoadFromPathAsync(filePath, cancellationToken);

            if(string.IsNullOrWhiteSpace(profile.Name)) {
                profile.Name = Path.GetFileNameWithoutExtension(filePath);
            }

            // Resolve ProjectPath to absolute if it's relative
            if(!string.IsNullOrWhiteSpace(profile.ProjectPath) && !PathResolver.IsAbsolutePath(profile.ProjectPath)) {
                profile.ProjectPath = ResolveAbsoluteProjectPath(filePath, profile.ProjectPath);
            }

            // Load credentials if available
            var credentials = await credentialStore.LoadCredentialsAsync(filePath, cancellationToken);
            if(credentials != null) {
                profile.Password = credentials.Password;
                logger.LogDebug("Loaded credentials for profile '{ProfileName}'", profile.Name);
            } else {
                logger.LogInformation("Not loaded credentials for profile '{ProfileName}', path: '{FilePath}'", profile.Name, filePath);
            }

            // Normalize port if needed
            profile.Connection.NormalizePort();

            // Validate the loaded profile
            var validationResult = ValidateProfile(profile);
            if(!validationResult.IsValid) {
                logger.LogWarning("Loaded profile '{ProfileName}' has validation errors: {Errors}", profile.Name, string.Join("; ", validationResult.Errors));
            }

            if(validationResult.Warnings.Count > 0) {
                logger.LogWarning("Loaded profile '{ProfileName}' has validation warnings: {Warnings}", profile.Name, string.Join("; ", validationResult.Warnings));
            }

            logger.LogInformation("Successfully loaded profile '{ProfileName}'", profile.Name);

            return profile;
        } catch(Exception ex) {
            throw "Failed to load profile: {0}"
                .F(filePath)
                .ToException(ex)
                .Add("Path", filePath);
        }
    }

    private string ResolveAbsoluteProjectPath(string filePath, string projectPath) {
        try {
            var profileDirectory = Path.GetDirectoryName(filePath);

            // If profile is in current directory (no directory component), use current directory
            if(string.IsNullOrEmpty(profileDirectory)) {
                profileDirectory = Directory.GetCurrentDirectory();
            }

            var absoluteProjectPath = Path.GetFullPath(projectPath, profileDirectory);
            logger.LogDebug("Resolved ProjectPath from relative '{Relative}' to absolute '{Absolute}'", projectPath, absoluteProjectPath);
            return absoluteProjectPath;
        } catch(Exception ex) {
            throw "Failed to resolve relative project path to absolute"
                .ToException(ex)
                .Add("Profile Path", filePath)
                .Add("Project Path", projectPath);
        }
    }

    /// <inheritdoc/>
    public async Task UpdateProfileAsync(DeploymentProfile profile, CancellationToken cancellationToken = default) {
        logger.LogInformation("Updating profile '{ProfileName}'", profile.Name);

        // Validate the profile
        var validationResult = ValidateProfile(profile);
        if(!validationResult.IsValid) {
            throw new ProfileValidationException(profile.Name, validationResult.Errors);
        }

        // Ensure profile exists
        if(!await profilesRepository.ExistsAsync(profile.Name, cancellationToken)) {
            throw new ProfileNotFoundException(profile.Name);
        }

        // Save the updated profile
        throw new NotImplementedException("Use UpdateProfileAsync(string filePath, DeploymentProfile profile) instead");
        //await Repository.SaveAsync(filePath, profile, cancellationToken);

        // Update credentials if provided
        if(!string.IsNullOrWhiteSpace(profile.Username) && !string.IsNullOrWhiteSpace(profile.Password)) {
            await credentialStore.SaveCredentialsAsync(
                profile.Name,
                profile.Username,
                profile.Password,
                cancellationToken);

            logger.LogDebug("Updated credentials for profile '{ProfileName}'", profile.Name);
        }

        logger.LogInformation("Successfully updated profile '{ProfileName}'", profile.Name);
    }

    /// <summary>
    /// Updates an existing deployment profile at the specified file path.
    /// </summary>
    /// <param name="filePath">The path to the profile file to update.</param>
    /// <param name="profile">The profile with updated values.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="ProfileValidationException">Thrown when the profile fails validation.</exception>
    /// <exception cref="ProfileNotFoundException">Thrown when the profile file does not exist.</exception>
    public async Task UpdateProfileAsync(string filePath, DeploymentProfile profile, CancellationToken cancellationToken = default) {
        logger.LogInformation("Updating profile '{ProfileName}' at path '{FilePath}'", profile.Name, filePath);

        // Validate the profile
        var validationResult = ValidateProfile(profile);
        if(!validationResult.IsValid) {
            throw new ProfileValidationException(profile.Name, validationResult.Errors);
        }

        // Ensure profile file exists
        if(!File.Exists(filePath)) {
            throw new ProfileNotFoundException($"Profile file not found: {filePath}");
        }

        // Save the updated profile to the repository
        await profilesRepository.SaveAsync(filePath, profile, cancellationToken);

        logger.LogDebug("Saved updated profile to '{FilePath}'", filePath);

        // Update credentials if provided
        if(!string.IsNullOrWhiteSpace(profile.Username) && !string.IsNullOrWhiteSpace(profile.Password)) {
            await credentialStore.SaveCredentialsAsync(
                filePath,
                profile.Username,
                profile.Password,
                cancellationToken);

            logger.LogDebug("Updated credentials for profile '{ProfileName}'", profile.Name);
        }

        logger.LogInformation("Successfully updated profile '{ProfileName}'", profile.Name);
    }

    /// <summary>
    /// Updates the password for a deployment profile without modifying the profile file.
    /// </summary>
    /// <param name="profilePath">The path to the profile file.</param>
    /// <param name="username">The username for the credentials.</param>
    /// <param name="password">The new password to save.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="ProfileNotFoundException">Thrown when the profile file does not exist.</exception>
    public async Task UpdatePasswordAsync(string profilePath, string username, string password, CancellationToken cancellationToken = default) {
        logger.LogInformation("Updating password for profile at '{ProfilePath}'", profilePath);

        // Ensure profile file exists
        if(!File.Exists(profilePath)) {
            throw new ProfileNotFoundException($"Profile file not found: {profilePath}");
        }

        // Validate username and password
        if(string.IsNullOrWhiteSpace(username)) {
            throw new ArgumentException("Username cannot be empty", nameof(username));
        }

        if(string.IsNullOrWhiteSpace(password)) {
            throw new ArgumentException("Password cannot be empty", nameof(password));
        }

        if(string.IsNullOrWhiteSpace(profilePath)) {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(profilePath));
        }

        // Save credentials to the credential store
        await credentialStore.SaveCredentialsAsync(
            profilePath,
            username,
            password,
            cancellationToken);

        logger.LogInformation("Successfully updated password for profile at '{ProfilePath}'", profilePath);
    }

    /// <inheritdoc/>
    public async Task<bool> DeleteProfileAsync(string profileName, CancellationToken cancellationToken = default) {
        logger.LogInformation("Deleting profile '{ProfileName}'", profileName);

        // Delete the profile file
        var deleted = await profilesRepository.DeleteAsync(profileName, cancellationToken);

        if(deleted) {
            // Delete associated credentials
            await credentialStore.DeleteCredentialsAsync(profileName, cancellationToken);
            logger.LogInformation("Successfully deleted profile '{ProfileName}'", profileName);
        } else {
            logger.LogWarning("Profile '{ProfileName}' not found for deletion", profileName);
        }

        return deleted;
    }

    /// <inheritdoc/>
    public async Task<List<ProfileSummary>> ListProfilesAsync(CancellationToken cancellationToken = default) {
        logger.LogDebug("Listing all profiles");

        var profilesPaths = await profilesRepository.ListProfileNamesAsync(cancellationToken);
        var summaries = new List<ProfileSummary>();

        foreach(var filePath in profilesPaths) {
            try {
                var profile = await profilesRepository.LoadFromPathAsync(filePath, cancellationToken);
                if(profile == null) {
                    continue;
                }

                var fileInfo = new FileInfo(filePath);
                var hasCredentials = await credentialStore.HasCredentialsAsync(filePath, cancellationToken);

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
                logger.LogWarning(ex, "Failed to load profile for listing: {ProfileName}", filePath);
            }
        }

        logger.LogDebug("Found {Count} profiles", summaries.Count);
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
