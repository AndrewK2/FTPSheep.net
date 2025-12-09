using System.Text.RegularExpressions;

namespace FTPSheep.Core.Utils;

/// <summary>
/// Utility class for resolving and validating file paths for FTPSheep configuration and profiles.
/// </summary>
public static partial class PathResolver {
    private const string AppDataFolderName = ".ftpsheep";
    private const string ProfilesFolderName = "profiles";
    private const string CredentialsFolderName = "credentials";
    private const string ConfigFileName = "config.json";
    private const int MaxProfileNameLength = 100;

    private static readonly string[] reservedWindowsNames = [
        "CON", "PRN", "AUX", "NUL",
        "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
        "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9"
    ];

    /// <summary>
    /// Gets the application data directory path (%APPDATA%\.ftpsheep).
    /// </summary>
    /// <returns>The full path to the application data directory.</returns>
    public static string GetApplicationDataPath() {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(appDataPath, AppDataFolderName);
    }

    /// <summary>
    /// Gets the profiles directory path (%APPDATA%\.ftpsheep\profiles).
    /// </summary>
    /// <returns>The full path to the profiles directory.</returns>
    public static string GetProfilesDirectoryPath() {
        return Path.Combine(GetApplicationDataPath(), ProfilesFolderName);
    }

    /// <summary>
    /// Gets the credentials directory path (%APPDATA%\.ftpsheep\credentials).
    /// </summary>
    /// <returns>The full path to the credentials directory.</returns>
    public static string GetCredentialsDirectoryPath() {
        return Path.Combine(GetApplicationDataPath(), CredentialsFolderName);
    }

    /// <summary>
    /// Gets the global configuration file path (%APPDATA%\.ftpsheep\config.json).
    /// </summary>
    /// <returns>The full path to the global configuration file.</returns>
    public static string GetGlobalConfigPath() {
        return Path.Combine(GetApplicationDataPath(), ConfigFileName);
    }

    /// <summary>
    /// Gets the full file path for a profile with the specified name.
    /// </summary>
    /// <param name="profileName">The name of the profile.</param>
    /// <returns>The full path to the profile JSON file.</returns>
    public static string GetProfileFilePath(string profileName) {
        return Path.Combine(GetProfilesDirectoryPath(), $"{profileName}.json");
    }

    /// <summary>
    /// Ensures that the application data directories exist, creating them if necessary.
    /// </summary>
    public static void EnsureDirectoriesExist() {
        var appDataPath = GetApplicationDataPath();
        var profilesPath = GetProfilesDirectoryPath();
        var credentialsPath = GetCredentialsDirectoryPath();

        if(!Directory.Exists(appDataPath)) {
            Directory.CreateDirectory(appDataPath);
        }

        if(!Directory.Exists(profilesPath)) {
            Directory.CreateDirectory(profilesPath);
        }

        if(!Directory.Exists(credentialsPath)) {
            Directory.CreateDirectory(credentialsPath);
        }
    }

    /// <summary>
    /// Validates a profile name according to file system naming rules.
    /// </summary>
    /// <param name="profileName">The profile name to validate.</param>
    /// <param name="errors">When this method returns, contains any validation errors.</param>
    /// <returns><c>true</c> if the profile name is valid; otherwise, <c>false</c>.</returns>
    public static bool ValidateProfileName(string profileName, out List<string> errors) {
        errors = [];

        if(string.IsNullOrWhiteSpace(profileName)) {
            errors.Add("Profile name cannot be empty.");
            return false;
        }

        if(profileName.Length > MaxProfileNameLength) {
            errors.Add($"Profile name cannot exceed {MaxProfileNameLength} characters.");
        }

        // Must start with letter or number
        if(!char.IsLetterOrDigit(profileName[0])) {
            errors.Add("Profile name must start with a letter or number.");
        }

        // Can only contain alphanumeric, hyphens, and underscores
        if(!ProfileNameRegex().IsMatch(profileName)) {
            errors.Add("Profile name can only contain letters, numbers, hyphens, and underscores.");
        }

        // Check for reserved Windows names
        var nameUpper = profileName.ToUpperInvariant();

        if(reservedWindowsNames.Contains(nameUpper)) {
            errors.Add($"Profile name '{profileName}' is a reserved Windows name and cannot be used.");
        }

        return errors.Count == 0;
    }

    /// <summary>
    /// Determines whether the specified string is an absolute file path or a profile name.
    /// </summary>
    /// <param name="pathOrName">The string to check.</param>
    /// <returns><c>true</c> if the string appears to be an absolute path; otherwise, <c>false</c>.</returns>
    public static bool IsAbsolutePath(string pathOrName) {
        if(string.IsNullOrWhiteSpace(pathOrName)) {
            return false;
        }

        // Check for Windows absolute paths (C:\, \\server\share)
        if(Path.IsPathRooted(pathOrName)) {
            return true;
        }

        // Check for Unix-style absolute paths (/)
        if(pathOrName.StartsWith('/')) {
            return true;
        }

        return false;
    }

    [GeneratedRegex("^[.a-zA-Z0-9_-]+$")]
    private static partial Regex ProfileNameRegex();
}