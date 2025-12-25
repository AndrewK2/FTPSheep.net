using FTPSheep.Core.Models;

namespace FTPSheep.Core.Interfaces;

/// <summary>
/// High-level service interface for managing deployment profiles with validation and credential management.
/// </summary>
public interface IProfileService {
    /// <summary>
    /// Creates a new deployment profile with validation.
    /// </summary>
    /// <param name="profileSavePath"></param>
    /// <param name="profile">The profile to create.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="Exceptions.ProfileAlreadyExistsException">Thrown when a profile with the same name already exists.</exception>
    /// <exception cref="Exceptions.ProfileValidationException">Thrown when the profile fails validation.</exception>
    Task CreateProfileAsync(string profileSavePath, DeploymentProfile profile, CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads a deployment profile by name or file path, applying global defaults and loading credentials.
    /// </summary>
    /// <param name="profileNameOrPath">The profile name or file path.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The loaded and validated profile.</returns>
    /// <exception cref="Exceptions.ProfileNotFoundException">Thrown when the profile cannot be found.</exception>
    Task<DeploymentProfile> LoadProfileAsync(string profileNameOrPath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing deployment profile with validation.
    /// </summary>
    /// <param name="profile">The profile to update.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="Exceptions.ProfileNotFoundException">Thrown when the profile does not exist.</exception>
    /// <exception cref="Exceptions.ProfileValidationException">Thrown when the profile fails validation.</exception>
    Task UpdateProfileAsync(DeploymentProfile profile, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a deployment profile and its associated credentials.
    /// </summary>
    /// <param name="profileName">The name of the profile to delete.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns><c>true</c> if the profile was deleted; <c>false</c> if it did not exist.</returns>
    Task<bool> DeleteProfileAsync(string profileName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists all available deployment profiles with summary information.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A list of profile summaries.</returns>
    Task<List<ProfileSummary>> ListProfilesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates a deployment profile without saving it.
    /// </summary>
    /// <param name="profile">The profile to validate.</param>
    /// <returns>The validation result.</returns>
    ValidationResult ValidateProfile(DeploymentProfile profile);
}
