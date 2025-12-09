namespace FTPSheep.Core.Services;

/// <summary>
/// Manages the creation and content of app_offline.htm files for IIS deployments.
/// </summary>
public class AppOfflineManager {
    private const string DefaultAppOfflineContent = @"<!DOCTYPE html>
<html>
<head>
    <title>Application Offline</title>
    <style>
        body {
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            display: flex;
            justify-content: center;
            align-items: center;
            min-height: 100vh;
            margin: 0;
            padding: 20px;
        }
        .container {
            background: white;
            border-radius: 10px;
            padding: 40px;
            max-width: 600px;
            box-shadow: 0 10px 40px rgba(0,0,0,0.2);
            text-align: center;
        }
        h1 {
            color: #333;
            margin-top: 0;
            font-size: 2em;
        }
        p {
            color: #666;
            line-height: 1.6;
            font-size: 1.1em;
        }
        .icon {
            font-size: 4em;
            margin-bottom: 20px;
        }
        .footer {
            margin-top: 30px;
            color: #999;
            font-size: 0.9em;
        }
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""icon"">🔧</div>
        <h1>Application Offline for Maintenance</h1>
        <p>We're currently performing maintenance and updates to improve your experience.</p>
        <p>This application will be back online shortly. Thank you for your patience!</p>
        <div class=""footer"">
            <p>Deployed with FTPSheep.NET</p>
        </div>
    </div>
</body>
</html>";

    private const string ErrorAppOfflineContent = @"<!DOCTYPE html>
<html>
<head>
    <title>Deployment Failed</title>
    <style>
        body {
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            background: linear-gradient(135deg, #f093fb 0%, #f5576c 100%);
            display: flex;
            justify-content: center;
            align-items: center;
            min-height: 100vh;
            margin: 0;
            padding: 20px;
        }
        .container {
            background: white;
            border-radius: 10px;
            padding: 40px;
            max-width: 600px;
            box-shadow: 0 10px 40px rgba(0,0,0,0.2);
            text-align: center;
        }
        h1 {
            color: #d32f2f;
            margin-top: 0;
            font-size: 2em;
        }
        p {
            color: #666;
            line-height: 1.6;
            font-size: 1.1em;
        }
        .icon {
            font-size: 4em;
            margin-bottom: 20px;
        }
        .error-message {
            background: #ffebee;
            border-left: 4px solid #d32f2f;
            padding: 15px;
            margin: 20px 0;
            text-align: left;
            font-family: monospace;
            color: #333;
        }
        .footer {
            margin-top: 30px;
            color: #999;
            font-size: 0.9em;
        }
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""icon"">⚠️</div>
        <h1>Deployment Failed</h1>
        <p>An error occurred during the deployment process.</p>
        <div class=""error-message"">{ERROR_MESSAGE}</div>
        <p>Please contact your system administrator for assistance.</p>
        <div class=""footer"">
            <p>Deployed with FTPSheep.NET</p>
        </div>
    </div>
</body>
</html>";

    private readonly string? customTemplate;

    /// <summary>
    /// Initializes a new instance of the <see cref="AppOfflineManager"/> class.
    /// </summary>
    /// <param name="customTemplate">Optional custom app_offline.htm template.</param>
    public AppOfflineManager(string? customTemplate = null) {
        this.customTemplate = customTemplate;
    }

    /// <summary>
    /// Gets the default app_offline.htm content.
    /// </summary>
    public static string DefaultContent => DefaultAppOfflineContent;

    /// <summary>
    /// Generates the app_offline.htm content for normal deployment.
    /// </summary>
    /// <returns>The HTML content for app_offline.htm.</returns>
    public string GenerateAppOfflineContent() {
        return string.IsNullOrWhiteSpace(customTemplate)
            ? DefaultAppOfflineContent
            : customTemplate;
    }

    /// <summary>
    /// Generates the app_offline.htm content for deployment failure scenarios.
    /// </summary>
    /// <param name="errorMessage">The error message to display.</param>
    /// <returns>The HTML content for app_offline.htm with error information.</returns>
    public string GenerateErrorAppOfflineContent(string? errorMessage = null) {
        var sanitizedMessage = SanitizeHtml(errorMessage ?? "An unknown error occurred.");
        return ErrorAppOfflineContent.Replace("{ERROR_MESSAGE}", sanitizedMessage);
    }

    /// <summary>
    /// Creates an app_offline.htm file in the specified directory.
    /// </summary>
    /// <param name="directoryPath">The directory where the file should be created.</param>
    /// <param name="isError">Whether this is an error app_offline.htm.</param>
    /// <param name="errorMessage">The error message (if isError is true).</param>
    /// <returns>The full path to the created file.</returns>
    public async Task<string> CreateAppOfflineFileAsync(
        string directoryPath,
        bool isError = false,
        string? errorMessage = null) {
        if(string.IsNullOrWhiteSpace(directoryPath)) {
            throw new ArgumentException("Directory path cannot be null or whitespace.", nameof(directoryPath));
        }

        if(!Directory.Exists(directoryPath)) {
            throw new DirectoryNotFoundException($"Directory not found: {directoryPath}");
        }

        var filePath = Path.Combine(directoryPath, "app_offline.htm");
        var content = isError
            ? GenerateErrorAppOfflineContent(errorMessage)
            : GenerateAppOfflineContent();

        await File.WriteAllTextAsync(filePath, content);

        return filePath;
    }

    /// <summary>
    /// Validates that an app_offline.htm file exists and has valid content.
    /// </summary>
    /// <param name="filePath">The path to the app_offline.htm file.</param>
    /// <returns>True if the file is valid; otherwise, false.</returns>
    public async Task<bool> ValidateAppOfflineFileAsync(string filePath) {
        if(string.IsNullOrWhiteSpace(filePath)) {
            return false;
        }

        if(!File.Exists(filePath)) {
            return false;
        }

        try {
            var content = await File.ReadAllTextAsync(filePath);
            // File should have some content and look like HTML
            return !string.IsNullOrWhiteSpace(content)
                   && content.Contains("<html", StringComparison.OrdinalIgnoreCase)
                   && content.Length >= 100; // Minimum reasonable size
        } catch {
            return false;
        }
    }

    /// <summary>
    /// Gets the standard filename for app_offline.htm.
    /// </summary>
    public static string FileName => "app_offline.htm";

    /// <summary>
    /// Sanitizes HTML content to prevent XSS attacks.
    /// </summary>
    /// <param name="input">The input string to sanitize.</param>
    /// <returns>The sanitized string.</returns>
    private static string SanitizeHtml(string input) {
        if(string.IsNullOrEmpty(input)) {
            return string.Empty;
        }

        return input
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;")
            .Replace("'", "&#39;");
    }
}
