using System.Xml.Linq;
using FTPSheep.Core.Exceptions;
using FTPSheep.Core.Models;

namespace FTPSheep.Core.Services;

/// <summary>
/// Service for parsing Visual Studio publish profiles (.pubxml files).
/// </summary>
public class PublishProfileParser {
    /// <summary>
    /// Parses a Visual Studio publish profile from XML content.
    /// </summary>
    /// <param name="xmlContent">The XML content of the publish profile.</param>
    /// <returns>The parsed publish profile.</returns>
    /// <exception cref="ArgumentException">If the content is null or empty.</exception>
    /// <exception cref="ProfileException">If the content cannot be parsed.</exception>
    public PublishProfile ParseFromString(string xmlContent) {
        if(string.IsNullOrWhiteSpace(xmlContent)) {
            throw new ArgumentException("XML content cannot be null or empty.", nameof(xmlContent));
        }

        try {
            var doc = XDocument.Parse(xmlContent);
            return ParseProfileXml(doc);
        } catch(Exception ex) when(ex is not ProfileException && ex is not ArgumentException) {
            throw new ProfileException($"Failed to parse publish profile XML: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Parses a Visual Studio publish profile from a file path.
    /// </summary>
    /// <param name="pubxmlPath">The path to the .pubxml file.</param>
    /// <returns>The parsed publish profile.</returns>
    /// <exception cref="ArgumentException">If the path is null or empty.</exception>
    /// <exception cref="FileNotFoundException">If the file does not exist.</exception>
    /// <exception cref="ProfileException">If the file cannot be parsed.</exception>
    public PublishProfile ParseProfile(string pubxmlPath) {
        if(string.IsNullOrWhiteSpace(pubxmlPath)) {
            throw new ArgumentException("Profile path cannot be null or empty.", nameof(pubxmlPath));
        }

        if(!File.Exists(pubxmlPath)) {
            throw new FileNotFoundException($"Publish profile not found: {pubxmlPath}", pubxmlPath);
        }

        try {
            var xmlContent = File.ReadAllText(pubxmlPath);
            var profile = ParseFromString(xmlContent);
            profile.SourceFilePath = pubxmlPath;

            return profile;
        } catch(ProfileException ex) {
            throw new ProfileException($"Failed to parse publish profile '{pubxmlPath}': {ex.Message}", ex);
        } catch(Exception ex) when(ex is not FileNotFoundException && ex is not ArgumentException) {
            throw new ProfileException($"Failed to parse publish profile '{pubxmlPath}': {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Searches for .pubxml files in a directory.
    /// </summary>
    /// <param name="searchPath">The directory to search (typically the project directory).</param>
    /// <returns>List of found .pubxml file paths.</returns>
    public List<string> DiscoverProfiles(string searchPath) {
        if(string.IsNullOrWhiteSpace(searchPath)) {
            searchPath = Directory.GetCurrentDirectory();
        }

        if(!Directory.Exists(searchPath)) {
            return [];
        }

        var profiles = new List<string>();

        // Search in Properties/PublishProfiles/ (standard location)
        var publishProfilesPath = Path.Combine(searchPath, "Properties", "PublishProfiles");
        if(Directory.Exists(publishProfilesPath)) {
            profiles.AddRange(Directory.GetFiles(publishProfilesPath, "*.pubxml", SearchOption.TopDirectoryOnly));
        }

        // Search recursively for any .pubxml files (fallback)
        if(profiles.Count == 0) {
            profiles.AddRange(Directory.GetFiles(searchPath, "*.pubxml", SearchOption.AllDirectories));
        }

        return profiles;
    }

    /// <summary>
    /// Parses the XML document to extract publish profile properties.
    /// </summary>
    private PublishProfile ParseProfileXml(XDocument doc) {
        var profile = new PublishProfile();

        // Get the PropertyGroup element (ignoring namespace)
        var propertyGroup = doc.Descendants().FirstOrDefault(e => e.Name.LocalName == "PropertyGroup");
        if(propertyGroup == null) {
            throw new ProfileException("Invalid publish profile: No PropertyGroup element found.");
        }

        // Extract standard properties
        profile.PublishMethod = GetElementValue(propertyGroup, "WebPublishMethod") ??
                                GetElementValue(propertyGroup, "PublishMethod") ??
                                string.Empty;

        profile.PublishUrl = GetElementValue(propertyGroup, "PublishUrl") ?? string.Empty;
        profile.UserName = GetElementValue(propertyGroup, "UserName");
        profile.SavePwd = GetElementValue(propertyGroup, "SavePWD")?.Equals("true", StringComparison.OrdinalIgnoreCase) == true;
        profile.DeleteExistingFiles = GetElementValue(propertyGroup, "DeleteExistingFiles")?.Equals("true", StringComparison.OrdinalIgnoreCase) == true;
        profile.TargetFramework = GetElementValue(propertyGroup, "TargetFramework");
        profile.RuntimeIdentifier = GetElementValue(propertyGroup, "RuntimeIdentifier");
        profile.PublishProtocol = GetElementValue(propertyGroup, "PublishProtocol");
        profile.ExcludeAppData = GetElementValue(propertyGroup, "ExcludeApp_Data")?.Equals("true", StringComparison.OrdinalIgnoreCase) == true;

        // Parse SelfContained
        var selfContainedValue = GetElementValue(propertyGroup, "SelfContained");
        if(selfContainedValue != null) {
            profile.SelfContained = selfContainedValue.Equals("true", StringComparison.OrdinalIgnoreCase);
        }

        // Post-deployment actions
        profile.SiteUrlToLaunchAfterPublish = GetElementValue(propertyGroup, "SiteUrlToLaunchAfterPublish");
        profile.LaunchSiteAfterPublish = GetElementValue(propertyGroup, "LaunchSiteAfterPublish")?.Equals("true", StringComparison.OrdinalIgnoreCase) == true;

        // Capture any additional properties that might be useful
        foreach(var element in propertyGroup.Elements()) {
            var name = element.Name.LocalName;
            var value = element.Value;

            // Skip properties we've already captured
            if(name is "WebPublishMethod" or "PublishMethod" or "PublishUrl" or "UserName" or
                "SavePWD" or "DeleteExistingFiles" or "TargetFramework" or "RuntimeIdentifier" or
                "PublishProtocol" or "ExcludeApp_Data" or "SelfContained" or
                "SiteUrlToLaunchAfterPublish" or "LaunchSiteAfterPublish") {
                continue;
            }

            if(!string.IsNullOrWhiteSpace(value)) {
                profile.AdditionalProperties[name] = value;
            }
        }

        return profile;
    }

    /// <summary>
    /// Gets the value of an XML element by name (ignoring namespace).
    /// </summary>
    private static string? GetElementValue(XElement parent, string elementName) {
        return parent.Elements().FirstOrDefault(e => e.Name.LocalName.Equals(elementName, StringComparison.OrdinalIgnoreCase))?.Value;
    }
}
