using FTPSheep.Core.Exceptions;
using FTPSheep.Core.Models;

namespace FTPSheep.Core.Services;

/// <summary>
/// Service for converting Visual Studio publish profiles to FTPSheep deployment profiles.
/// </summary>
public class PublishProfileConverter {
    /// <summary>
    /// Converts a Visual Studio publish profile to a FTPSheep deployment profile.
    /// </summary>
    /// <param name="publishProfile">The publish profile to convert.</param>
    /// <param name="profileName">The name for the FTPSheep profile (optional, will use .pubxml filename if not provided).</param>
    /// <returns>The converted deployment profile.</returns>
    /// <exception cref="ArgumentNullException">If publishProfile is null.</exception>
    /// <exception cref="ProfileException">If the profile cannot be converted.</exception>
    public DeploymentProfile Convert(PublishProfile publishProfile, string? profileName = null) {
        if(publishProfile == null) {
            throw new ArgumentNullException(nameof(publishProfile));
        }

        // Validate that this is an FTP profile
        if(!publishProfile.IsFtpProfile) {
            throw new ProfileException($"Cannot import non-FTP publish profile '{publishProfile.SourceFilePath}'. PublishMethod is '{publishProfile.PublishMethod}', expected 'FTP'.");
        }

        // Validate that we have a publish URL
        if(string.IsNullOrWhiteSpace(publishProfile.PublishUrl)) {
            throw new ProfileException($"Cannot import publish profile '{publishProfile.SourceFilePath}': PublishUrl is missing.");
        }

        // Parse the publish URL to extract connection details
        var (host, port, remotePath) = publishProfile.ParsePublishUrl();

        if(string.IsNullOrWhiteSpace(host)) {
            throw new ProfileException($"Cannot parse host from PublishUrl '{publishProfile.PublishUrl}' in '{publishProfile.SourceFilePath}'.");
        }

        // Determine profile name
        var name = profileName;

        if(string.IsNullOrWhiteSpace(name) && !string.IsNullOrWhiteSpace(publishProfile.SourceFilePath)) {
            name = Path.GetFileNameWithoutExtension(publishProfile.SourceFilePath);
        }

        if(string.IsNullOrWhiteSpace(name)) {
            name = $"{host}-import";
        }

        // Create the deployment profile
        var deploymentProfile = new DeploymentProfile {
            Name = name,
            Connection = new ServerConnection {
                Host = host,
                Port = port,
                Protocol = DetermineProtocol(publishProfile),
                UseSsl = publishProfile.PublishProtocol?.Equals("ftps", StringComparison.OrdinalIgnoreCase) == true ||
                         publishProfile.PublishUrl?.StartsWith("ftps://", StringComparison.OrdinalIgnoreCase) == true
            },
            Username = publishProfile.UserName ?? string.Empty,
            RemotePath = remotePath,
            Build = new BuildConfiguration {
                Configuration = "Release", // Default, can be overridden
                TargetFramework = publishProfile.TargetFramework,
                RuntimeIdentifier = publishProfile.RuntimeIdentifier,
                SelfContained = publishProfile.SelfContained
            },
            CleanupMode = publishProfile.DeleteExistingFiles ? CleanupMode.DeleteObsolete : CleanupMode.None,
            AppOfflineEnabled = !publishProfile.ExcludeAppData, // If excluding app_data, likely don't want app_offline
            Concurrency = 4, // Default
            RetryCount = 3 // Default
        };

        return deploymentProfile;
    }

    /// <summary>
    /// Determines the protocol from the publish profile.
    /// </summary>
    private static ProtocolType DetermineProtocol(PublishProfile publishProfile) {
        // For now, we only support FTP in V1
        // SFTP support is low priority and will be added later
        return ProtocolType.Ftp;
    }

    /// <summary>
    /// Validates that an imported profile is usable.
    /// </summary>
    /// <param name="profile">The profile to validate.</param>
    /// <returns>A list of validation errors (empty if valid).</returns>
    public List<string> ValidateImportedProfile(DeploymentProfile profile) {
        var errors = new List<string>();

        if(string.IsNullOrWhiteSpace(profile.Name)) {
            errors.Add("Profile name is required.");
        }

        if(string.IsNullOrWhiteSpace(profile.Connection.Host)) {
            errors.Add("Connection host is required.");
        }

        if(profile.Connection.Port <= 0 || profile.Connection.Port > 65535) {
            errors.Add($"Invalid port: {profile.Connection.Port}. Must be between 1 and 65535.");
        }

        if(string.IsNullOrWhiteSpace(profile.Username)) {
            errors.Add("Username is required.");
        }

        return errors;
    }
}