namespace FTPSheep.Core.Models;

/// <summary>
/// Represents a parsed Visual Studio publish profile (.pubxml).
/// </summary>
public class PublishProfile {
    /// <summary>
    /// Gets or sets the publish method (e.g., "FTP", "MSDeploy", "FileSystem").
    /// </summary>
    public string PublishMethod { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the publish URL (FTP server and path).
    /// </summary>
    public string PublishUrl { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the username for authentication.
    /// </summary>
    public string? UserName { get; set; }

    /// <summary>
    /// Gets or sets whether to save the password.
    /// </summary>
    public bool SavePwd { get; set; }

    /// <summary>
    /// Gets or sets whether to delete existing files before publishing.
    /// </summary>
    public bool DeleteExistingFiles { get; set; }

    /// <summary>
    /// Gets or sets the target framework.
    /// </summary>
    public string? TargetFramework { get; set; }

    /// <summary>
    /// Gets or sets the self-contained flag.
    /// </summary>
    public bool? SelfContained { get; set; }

    /// <summary>
    /// Gets or sets the runtime identifier.
    /// </summary>
    public string? RuntimeIdentifier { get; set; }

    /// <summary>
    /// Gets or sets the publish protocol (e.g., "ftp", "ftps").
    /// </summary>
    public string? PublishProtocol { get; set; }

    public bool ExcludeAppData { get; set; }

    /// <summary>
    /// Gets or sets the URL to launch after publish (from .pubxml).
    /// </summary>
    public string? SiteUrlToLaunchAfterPublish { get; set; }

    /// <summary>
    /// Gets or sets whether to launch the site after publish (from .pubxml).
    /// </summary>
    public bool LaunchSiteAfterPublish { get; set; }

    /// <summary>
    /// Gets or sets additional MSBuild properties from the profile.
    /// </summary>
    public Dictionary<string, string> AdditionalProperties { get; set; } = new();

    /// <summary>
    /// Gets or sets the source file path of the .pubxml file.
    /// </summary>
    public string? SourceFilePath { get; set; }

    /// <summary>
    /// Gets whether this is an FTP publish profile.
    /// </summary>
    public bool IsFtpProfile => PublishMethod?.Equals("FTP", StringComparison.OrdinalIgnoreCase) == true;

    /// <summary>
    /// Parses the publish URL to extract host, port, and path components.
    /// </summary>
    /// <returns>A tuple containing (host, port, remotePath).</returns>
    public (string host, int port, string remotePath) ParsePublishUrl() {
        if(string.IsNullOrWhiteSpace(PublishUrl)) {
            return (string.Empty, 21, string.Empty);
        }

        try {
            // Handle URLs like "ftp://ftp.example.com/site/wwwroot" or "ftp.example.com/site/wwwroot"
            var url = PublishUrl;

            // Add ftp:// prefix if not present
            if(!url.StartsWith("ftp://", StringComparison.OrdinalIgnoreCase) &&
                !url.StartsWith("ftps://", StringComparison.OrdinalIgnoreCase)) {
                url = "ftp://" + url;
            }

            var uri = new Uri(url);
            var host = uri.Host;
            var port = uri.Port > 0 ? uri.Port : 21;
            var remotePath = uri.AbsolutePath;

            // Clean up remote path
            if(remotePath == "/") {
                remotePath = string.Empty;
            }

            return (host, port, remotePath);
        } catch {
            // If parsing fails, try simple string split
            var parts = PublishUrl.Split('/');
            var hostPart = parts[0];
            var path = parts.Length > 1 ? "/" + string.Join("/", parts.Skip(1)) : string.Empty;

            // Check for port in host part
            var hostPortParts = hostPart.Split(':');
            var host = hostPortParts[0];
            var port = hostPortParts.Length > 1 && int.TryParse(hostPortParts[1], out var p) ? p : 21;

            return (host, port, path);
        }
    }
}
