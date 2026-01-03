using System.Diagnostics.CodeAnalysis;
using FTPSheep.Core.Models;
using FTPSheep.Core.Services;
using FTPSheep.VisualStudio.Modern.Services;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Extensibility;
using Microsoft.VisualStudio.Extensibility.ToolWindows;
using Microsoft.VisualStudio.RpcContracts.RemoteUI;

namespace FTPSheep.VisualStudio.Modern.ToolWindows;

/// <summary>
/// FTPSheep tool window for managing FTP deployments.
/// Provides UI for profile management and deployment execution.
/// </summary>
[VisualStudioContribution]
public class FTPSheepToolWindow : ToolWindow {
    private FTPSheepToolWindowControl? toolWindowControl;
    private readonly ProfileService profileService;
    private readonly JsonDeploymentHistoryService historyService;
    private readonly VsDeploymentOrchestrator deploymentOrchestrator;
    private readonly ILogger<FTPSheepToolWindow> logger;

    public FTPSheepToolWindow(VisualStudioExtensibility extensibility, ProfileService profileService, JsonDeploymentHistoryService historyService, VsDeploymentOrchestrator deploymentOrchestrator,
        ILogger<FTPSheepToolWindow> logger) : base(extensibility) {
        Title = "FTPSheep";
        this.profileService = profileService;
        this.historyService = historyService;
        this.deploymentOrchestrator = deploymentOrchestrator;
        this.logger = logger;
    }

    /// <summary>
    /// Tool window configuration - dock to the right by default.
    /// </summary>
    public override ToolWindowConfiguration ToolWindowConfiguration =>
        new() {
            Placement = ToolWindowPlacement.Floating,
            DockDirection = Dock.None,
            AllowAutoCreation = false
        };

    /// <summary>
    /// Initialize the tool window (called before creating the UI).
    /// </summary>
    [Experimental("VSEXTPREVIEW_OUTPUTWINDOW")]
    public override async Task InitializeAsync(CancellationToken cancellationToken) {
        // Initialization logic will be performed when UI is created

        logger.LogInformation("Initializing tool window");

        //using var outputChannel = await Extensibility.Views().Output.CreateOutputChannelAsync("FTPSheep", CancellationToken.None);
        //await outputChannel.WriteLineAsync("Initializing FTPSheep");

        await Task.CompletedTask;
    }

    /// <summary>
    /// Create and return the tool window UI content.
    /// </summary>
    public override Task<IRemoteUserControl> GetContentAsync(CancellationToken cancellationToken) {
        try {
            // Create the control with minimal initialization
            toolWindowControl = new FTPSheepToolWindowControl();

            // Set up basic data - don't try to load profiles/history yet
            var dataContext = toolWindowControl.DataContext;
            dataContext.WelcomeMessage = "FTPSheep Deployment Tool";
            dataContext.Projects = new List<ProjectItem> {
                new() {
                    Name = "No projects loaded",
                    Path = string.Empty
                }
            };
            dataContext.Profiles = new List<ProfileItem>();
            dataContext.RecentDeployments = new List<DeploymentHistoryItem>();

            // Set up command handlers with placeholder implementations
            toolWindowControl.SetCommandHandlers(
                deployCommand: async (param, ct) => await Task.CompletedTask,
                newProfileCommand: async (param, ct) => await Task.CompletedTask,
                editProfileCommand: async (param, ct) => await Task.CompletedTask,
                deleteProfileCommand: async (param, ct) => await Task.CompletedTask,
                importProfileCommand: async (param, ct) => await Task.CompletedTask);

            return Task.FromResult<IRemoteUserControl>(toolWindowControl);
        } catch (Exception ex) {
            // Log the exception details for debugging
            throw new InvalidOperationException($"Failed to create tool window content: {ex.GetType().Name} - {ex.Message}\nStack: {ex.StackTrace}", ex);
        }
    }

    /// <summary>
    /// Handles the Deploy command.
    /// </summary>
    private async Task OnDeployAsync(object? parameter, CancellationToken cancellationToken) {
        if(toolWindowControl?.DataContext == null)
            return;

        var dataContext = toolWindowControl.DataContext;

        try {
            // Get selected profile
            var selectedProfileItem = dataContext.Profiles
                .FirstOrDefault(p => p.Name == dataContext.SelectedProfile);

            if(selectedProfileItem == null) {
                await ShowErrorAsync("Please select a deployment profile", cancellationToken);
                return;
            }

            // Load the full profile
            var profile = await profileService.LoadProfileAsync(selectedProfileItem.FilePath,
                cancellationToken);

            // Validate profile has required settings
            if(string.IsNullOrWhiteSpace(profile.ProjectPath)) {
                await ShowErrorAsync($"Profile '{profile.Name}' does not have a project path configured. Please edit the profile and specify a project path.",
                    cancellationToken);
                return;
            }

            // Check if project file exists
            if(!File.Exists(profile.ProjectPath)) {
                await ShowErrorAsync($"Project file not found: {profile.ProjectPath}",
                    cancellationToken);
                return;
            }

            // Create deployment options
            var options = new DeploymentOptions {
                ProfileName = profile.Name,
                Profile = profile,
                ProjectPath = profile.ProjectPath,
                TargetHost = profile.Connection.Host,
                UseAppOffline = profile.AppOfflineEnabled,
                CleanupMode = profile.CleanupMode != CleanupMode.None
            };

            // Execute deployment through the orchestrator
            var result = await deploymentOrchestrator.ExecuteDeploymentAsync(options,
                cancellationToken);

            // Refresh deployment history after successful deployment
            if(result.Success) {
                await RefreshDeploymentHistoryAsync(cancellationToken);
            }
        } catch(OperationCanceledException) {
            await ShowWarningAsync("Deployment was cancelled", cancellationToken);
        } catch(Exception ex) {
            await ShowErrorAsync($"Deployment failed: {ex.Message}", cancellationToken);
        }
    }

    /// <summary>
    /// Handles the New Profile command.
    /// </summary>
    private async Task OnNewProfileAsync(object? parameter, CancellationToken cancellationToken) {
        try {
            // Generate a unique profile name with timestamp
            var profileName = $"New Profile {DateTime.Now:yyyy-MM-dd HHmmss}";

            // Create new empty profile with defaults
            var newProfile = new DeploymentProfile {
                Name = profileName,
                Connection = new ServerConnection {
                    Host = "ftp.example.com",
                    Port = 21,
                    Protocol = ProtocolType.Ftp,
                    UseSsl = false
                },
                Build = new BuildConfiguration {
                    Configuration = "Release",
                    TargetFramework = "net8.0"
                },
                RemotePath = "/",
                AppOfflineEnabled = true,
                CleanupMode = CleanupMode.None,
                Concurrency = 4
            };

            // TODO: Show Profile Editor Dialog to let user customize the profile

            // Determine the save path (in standard profiles directory)
            var profilesDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                ".ftpsheep",
                "profiles");

            Directory.CreateDirectory(profilesDir);
            var profilePath = Path.Combine(profilesDir, $"{profileName}.ftpsheep");

            // Save the profile using ProfileService
            await profileService.CreateProfileAsync(profilePath, newProfile, cancellationToken);

            // Refresh the profiles list
            await RefreshProfilesListAsync(cancellationToken);

            // Select the newly created profile
            if(toolWindowControl?.DataContext != null) {
                toolWindowControl.DataContext.SelectedProfile = newProfile.Name;
            }
        } catch(Exception ex) {
            await ShowErrorAsync($"Failed to create new profile: {ex.Message}", cancellationToken);
        }
    }

    /// <summary>
    /// Handles the Edit Profile command.
    /// </summary>
    private async Task OnEditProfileAsync(object? parameter, CancellationToken cancellationToken) {
        if(toolWindowControl?.DataContext == null)
            return;

        var dataContext = toolWindowControl.DataContext;

        try {
            // Get selected profile
            var selectedProfileItem = dataContext.Profiles
                .FirstOrDefault(p => p.Name == dataContext.SelectedProfile);

            if(selectedProfileItem == null) {
                await ShowErrorAsync("Please select a profile to edit", cancellationToken);
                return;
            }

            // Load the full profile
            var profile = await profileService.LoadProfileAsync(selectedProfileItem.FilePath,
                cancellationToken);

            // TODO: Show Profile Editor Dialog to let user modify the profile
            // For now, we'll just show a message indicating this feature needs a dialog
            await ShowErrorAsync($"Profile editor dialog not yet implemented. Profile '{profile.Name}' is loaded and ready to edit.",
                cancellationToken);

            // After dialog implementation:
            // 1. Show dialog with profile data
            // 2. If user clicks OK, save changes:
            //    await profileService.SaveProfileAsync(profile, cancellationToken);
            // 3. Refresh profiles list
            //    await RefreshProfilesListAsync(cancellationToken);
        } catch(Exception ex) {
            await ShowErrorAsync($"Failed to edit profile: {ex.Message}", cancellationToken);
        }
    }

    /// <summary>
    /// Handles the Delete Profile command.
    /// </summary>
    private async Task OnDeleteProfileAsync(object? parameter, CancellationToken cancellationToken) {
        if(toolWindowControl?.DataContext == null)
            return;

        var dataContext = toolWindowControl.DataContext;

        try {
            // Get selected profile
            var selectedProfileItem = dataContext.Profiles
                .FirstOrDefault(p => p.Name == dataContext.SelectedProfile);

            if(selectedProfileItem == null) {
                await ShowErrorAsync("Please select a profile to delete", cancellationToken);
                return;
            }

            // TODO: Show confirmation dialog
            // For now, we'll proceed with deletion without confirmation
            // In production, should show: "Are you sure you want to delete profile 'X'?"

            // Delete the profile (this deletes the file and credentials)
            await profileService.DeleteProfileAsync(selectedProfileItem.Name, cancellationToken);

            // Refresh the profiles list
            await RefreshProfilesListAsync(cancellationToken);

            // Clear selection
            dataContext.SelectedProfile = string.Empty;
        } catch(Exception ex) {
            await ShowErrorAsync($"Failed to delete profile: {ex.Message}", cancellationToken);
        }
    }

    /// <summary>
    /// Handles the Import .pubxml command.
    /// </summary>
    private async Task OnImportProfileAsync(object? parameter, CancellationToken cancellationToken) {
        try {
            // TODO: Show file picker dialog for .pubxml files
            // For now, we'll provide instructions on how to use this feature
            await ShowErrorAsync("Import .pubxml feature requires a file picker dialog.\n\n" +
                                 "To import a Visual Studio publish profile:\n" +
                                 "1. Locate your .pubxml file (usually in Properties/PublishProfiles/)\n" +
                                 "2. Use the CLI command: ftpsheep import <path-to-pubxml>\n" +
                                 "3. The imported profile will appear in this list automatically",
                cancellationToken);

            // After file picker implementation:
            // 1. Show file picker dialog
            // 2. User selects .pubxml file
            // 3. Parse the file:
            //    var parser = new PublishProfileParser();
            //    var publishProfile = await parser.ParseAsync(selectedFilePath, cancellationToken);
            // 4. Convert to DeploymentProfile:
            //    var converter = new PublishProfileConverter();
            //    var profile = converter.Convert(publishProfile);
            // 5. Prompt for profile name and password
            // 6. Save:
            //    await profileService.SaveProfileAsync(profile, cancellationToken);
            // 7. Refresh profiles list:
            //    await RefreshProfilesListAsync(cancellationToken);
        } catch(Exception ex) {
            await ShowErrorAsync($"Failed to import profile: {ex.Message}", cancellationToken);
        }
    }

    /// <summary>
    /// Refreshes the profiles list from ProfileService.
    /// </summary>
    private async Task RefreshProfilesListAsync(CancellationToken cancellationToken) {
        if(toolWindowControl?.DataContext == null)
            return;

        var dataContext = toolWindowControl.DataContext;

        try {
            var profileSummaries = await profileService.ListProfilesAsync(cancellationToken);

            dataContext.Profiles = profileSummaries.Select(p => new ProfileItem {
                Name = p.Name,
                Server = p.ConnectionString,
                RemotePath = p.RemotePath,
                LastModified = p.LastModified,
                HasCredentials = p.HasCredentials,
                FilePath = p.FilePath
            }).ToList();
        } catch {
            // If profile refresh fails, keep existing list
        }
    }

    /// <summary>
    /// Refreshes the deployment history list.
    /// </summary>
    private async Task RefreshDeploymentHistoryAsync(CancellationToken cancellationToken) {
        if(toolWindowControl?.DataContext == null)
            return;

        var dataContext = toolWindowControl.DataContext;

        try {
            var history = await historyService.GetRecentEntriesAsync(10, cancellationToken);

            dataContext.RecentDeployments = history.Select(h => new DeploymentHistoryItem {
                ProfileName = h.ProfileName,
                ProjectName = h.ProfileName, // TODO: Get actual project name
                Timestamp = h.Timestamp,
                Success = h.Success,
                FilesUploaded = h.FilesUploaded
            }).ToList();
        } catch {
            // If history refresh fails, silently ignore
        }
    }

    /// <summary>
    /// Shows an error message to the user.
    /// </summary>
    private Task ShowErrorAsync(string message, CancellationToken cancellationToken) {
        // TODO: Show a proper error dialog
        // For now, just log to the deployment orchestrator's output
        return Task.CompletedTask;
    }

    /// <summary>
    /// Shows a warning message to the user.
    /// </summary>
    private Task ShowWarningAsync(string message, CancellationToken cancellationToken) {
        // TODO: Show a proper warning dialog
        // For now, just log to the deployment orchestrator's output
        return Task.CompletedTask;
    }
}