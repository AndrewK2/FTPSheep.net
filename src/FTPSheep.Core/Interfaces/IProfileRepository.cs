using FTPSheep.Core.Models;

namespace FTPSheep.Core.Interfaces;

/// <summary>
/// Repository interface for managing deployment profile persistence.
/// </summary>
public interface IProfileRepository
{
    /// <summary>
    /// Saves a deployment profile to storage.
    /// </summary>
    /// <param name="profile">The profile to save.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SaveAsync(DeploymentProfile profile, CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads a deployment profile by name.
    /// </summary>
    /// <param name="profileName">The name of the profile to load.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The loaded profile, or null if not found.</returns>
    Task<DeploymentProfile?> LoadAsync(string profileName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads a deployment profile from a specific file path.
    /// </summary>
    /// <param name="filePath">The full path to the profile file.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The loaded profile.</returns>
    /// <exception cref="FileNotFoundException">Thrown when the file does not exist.</exception>
    Task<DeploymentProfile> LoadFromPathAsync(string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists all available profile names.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A list of profile names.</returns>
    Task<List<string>> ListProfileNamesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a deployment profile by name.
    /// </summary>
    /// <param name="profileName">The name of the profile to delete.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns><c>true</c> if the profile was deleted; <c>false</c> if it did not exist.</returns>
    Task<bool> DeleteAsync(string profileName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a profile with the specified name exists.
    /// </summary>
    /// <param name="profileName">The name of the profile to check.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns><c>true</c> if the profile exists; otherwise, <c>false</c>.</returns>
    Task<bool> ExistsAsync(string profileName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the file path for a profile with the specified name.
    /// </summary>
    /// <param name="profileName">The name of the profile.</param>
    /// <returns>The full path to the profile file.</returns>
    string GetProfilePath(string profileName);
}
