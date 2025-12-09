using System.Text;
using FTPSheep.Core.Exceptions;
using FTPSheep.Core.Models;

namespace FTPSheep.Core.Utils;

/// <summary>
/// Provides utilities for formatting user-friendly error messages with context and suggestions.
/// </summary>
public static class ErrorMessageFormatter {
    /// <summary>
    /// Formats an exception into a user-friendly error message.
    /// </summary>
    /// <param name="exception">The exception to format.</param>
    /// <param name="verbosity">The verbosity level (affects detail level).</param>
    /// <returns>A formatted error message.</returns>
    public static string FormatException(Exception exception, LogVerbosity verbosity = LogVerbosity.Normal) {
        if(exception == null) {
            throw new ArgumentNullException(nameof(exception));
        }

        var sb = new StringBuilder();

        // Add error header
        sb.AppendLine($"ERROR: {exception.Message}");
        sb.AppendLine();

        // Add specific suggestions based on exception type
        var suggestions = GetSuggestions(exception);
        if(suggestions.Count > 0) {
            sb.AppendLine("Suggestions:");
            foreach(var suggestion in suggestions) {
                sb.AppendLine($"  • {suggestion}");
            }
            sb.AppendLine();
        }

        // Add technical details in verbose mode
        if(verbosity >= LogVerbosity.Verbose) {
            AppendTechnicalDetails(sb, exception);
        }

        return sb.ToString();
    }

    /// <summary>
    /// Formats an exception into a concise single-line error message.
    /// </summary>
    /// <param name="exception">The exception to format.</param>
    /// <returns>A concise error message.</returns>
    public static string FormatConcise(Exception exception) {
        if(exception == null) {
            throw new ArgumentNullException(nameof(exception));
        }

        return $"{exception.GetType().Name}: {exception.Message}";
    }

    private static List<string> GetSuggestions(Exception exception) {
        var suggestions = new List<string>();

        switch(exception) {
            case BuildToolNotFoundException buildToolEx:
                suggestions.Add($"Install {buildToolEx.ToolName} or ensure it's in your PATH");
                suggestions.Add("If using Visual Studio, run from Developer Command Prompt");
                break;

            case BuildCompilationException buildEx:
                suggestions.Add("Review build errors and fix compilation issues");
                suggestions.Add("Ensure all project dependencies are restored (dotnet restore)");
                suggestions.Add("Check that the target framework is installed");
                break;

            case ConnectionTimeoutException timeoutEx:
                suggestions.Add("Check your network connection");
                suggestions.Add("Verify the server is running and accessible");
                suggestions.Add("Try increasing the timeout value in your profile");
                suggestions.Add("Check if a firewall is blocking the connection");
                break;

            case ConnectionRefusedException refusedEx:
                suggestions.Add($"Verify the FTP/SFTP server is running on {refusedEx.Host}:{refusedEx.Port}");
                suggestions.Add("Check that the correct port is specified");
                suggestions.Add("Ensure no firewall is blocking the connection");
                break;

            case SslCertificateException sslEx:
                suggestions.Add("Verify the server's SSL certificate is valid");
                suggestions.Add("If using a self-signed certificate, you may need to disable certificate validation");
                suggestions.Add("Check that the system date and time are correct");
                break;

            case InvalidCredentialsException credEx:
                suggestions.Add("Verify your username and password are correct");
                suggestions.Add("Check if credentials are stored: ftpsheep credentials list");
                suggestions.Add("Try setting credentials in environment variables (FTP_USERNAME, FTP_PASSWORD)");
                suggestions.Add("Re-save credentials: ftpsheep credentials save <profile>");
                break;

            case InsufficientPermissionsException permEx:
                suggestions.Add($"Ensure the user has '{permEx.RequiredPermission}' permission on the server");
                suggestions.Add("Contact your server administrator to grant the necessary permissions");
                suggestions.Add("Verify the target directory exists and is writable");
                break;

            case AuthenticationException authEx:
                suggestions.Add("Verify your credentials are correct");
                suggestions.Add("Check if the authentication method (password/key) matches server requirements");
                if(authEx.Username != null) {
                    suggestions.Add($"Verify user '{authEx.Username}' exists on the server");
                }
                break;

            case FileTransferException transferEx:
                suggestions.Add("Check network connectivity and stability");
                suggestions.Add("Verify you have write permissions on the remote directory");
                suggestions.Add("Ensure sufficient disk space on the remote server");
                suggestions.Add("Try the operation again (file transfers are often transient failures)");
                break;

            case InsufficientDiskSpaceException diskEx:
                var required = diskEx.RequiredBytes / 1024 / 1024;
                var available = diskEx.AvailableBytes / 1024 / 1024;
                suggestions.Add($"Free up at least {required - available} MB of disk space on the remote server");
                suggestions.Add("Clean up old deployments or temporary files");
                suggestions.Add("Contact your server administrator about increasing disk quota");
                break;

            case ProfileNotFoundException profileEx:
                suggestions.Add($"Create the profile: ftpsheep profile create {profileEx.ProfileName}");
                suggestions.Add("List available profiles: ftpsheep profile list");
                suggestions.Add("Check for typos in the profile name");
                break;

            case ProfileValidationException validationEx:
                suggestions.Add("Fix the validation errors listed above");
                suggestions.Add("Edit the profile: ftpsheep profile edit <name>");
                suggestions.Add("Review profile settings: ftpsheep profile show <name>");
                break;

            case ConfigurationException configEx:
                suggestions.Add("Check your configuration file for errors");
                suggestions.Add("Validate JSON syntax if editing manually");
                suggestions.Add("Reset to defaults if needed: ftpsheep config reset");
                break;

            case OperationCanceledException:
                suggestions.Add("Operation was cancelled by user (Ctrl+C)");
                suggestions.Add("If unintentional, run the command again");
                break;

            case DeploymentException deployEx when deployEx.IsRetryable:
                suggestions.Add("This is a transient error - try running the command again");
                suggestions.Add("Check network connectivity");
                break;
        }

        return suggestions;
    }

    private static void AppendTechnicalDetails(StringBuilder sb, Exception exception) {
        sb.AppendLine("Technical Details:");
        sb.AppendLine($"  Exception Type: {exception.GetType().FullName}");

        // Add exception-specific properties
        switch(exception) {
            case ProfileException profileEx when profileEx.ProfileName != null:
                sb.AppendLine($"  Profile: {profileEx.ProfileName}");
                break;

            case BuildException buildEx:
                if(buildEx.ProjectPath != null) {
                    sb.AppendLine($"  Project: {buildEx.ProjectPath}");
                }
                if(buildEx.BuildConfiguration != null) {
                    sb.AppendLine($"  Configuration: {buildEx.BuildConfiguration}");
                }
                break;

            case ConnectionException connEx:
                if(connEx.Host != null) {
                    sb.AppendLine($"  Host: {connEx.Host}");
                }
                if(connEx.Port.HasValue) {
                    sb.AppendLine($"  Port: {connEx.Port}");
                }
                sb.AppendLine($"  Transient: {connEx.IsTransient}");
                break;

            case AuthenticationException authEx:
                if(authEx.Username != null) {
                    sb.AppendLine($"  Username: {authEx.Username}");
                }
                if(authEx.Host != null) {
                    sb.AppendLine($"  Host: {authEx.Host}");
                }
                break;

            case DeploymentException deployEx:
                if(deployEx.ProfileName != null) {
                    sb.AppendLine($"  Profile: {deployEx.ProfileName}");
                }
                sb.AppendLine($"  Phase: {deployEx.Phase}");
                sb.AppendLine($"  Retryable: {deployEx.IsRetryable}");
                break;
        }

        // Add stack trace in Debug verbosity
        if(exception.InnerException != null) {
            sb.AppendLine();
            sb.AppendLine($"Inner Exception: {exception.InnerException.GetType().Name}");
            sb.AppendLine($"  Message: {exception.InnerException.Message}");
        }

        sb.AppendLine();
        sb.AppendLine("Stack Trace:");
        sb.AppendLine(exception.StackTrace ?? "  (No stack trace available)");
    }
}
