using FTPSheep.Core.Models;

namespace FTPSheep.Core.Interfaces;

/// <summary>
/// Service interface for managing global configuration settings.
/// </summary>
public interface IConfigurationService {
    /// <summary>
    /// Loads the global configuration, creating a default one if it doesn't exist.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The global configuration.</returns>
    Task<GlobalConfiguration> LoadConfigurationAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves the global configuration to storage.
    /// </summary>
    /// <param name="configuration">The configuration to save.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SaveConfigurationAsync(GlobalConfiguration configuration, CancellationToken cancellationToken = default);

    /// <summary>
    /// Applies global configuration defaults to a deployment profile where the profile has not specified values.
    /// </summary>
    /// <param name="profile">The profile to apply defaults to.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ApplyDefaultsAsync(DeploymentProfile profile, CancellationToken cancellationToken = default);
}
