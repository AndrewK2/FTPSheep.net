using System.Diagnostics.CodeAnalysis;
using FTPSheep.Core.Interfaces;
using FTPSheep.Core.Models;
using FTPSheep.Core.Services;
using FTPSheep.VisualStudio.Modern.Services;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Extensibility;
using Microsoft.VisualStudio.Extensibility.ToolWindows;
using Microsoft.VisualStudio.ProjectSystem.Query;
using Microsoft.VisualStudio.RpcContracts.RemoteUI;

namespace FTPSheep.VisualStudio.Modern.ToolWindows;

/// <summary>
/// FTPSheep tool window for managing FTP deployments.
/// Provides UI for profile management and deployment execution.
/// </summary>
[VisualStudioContribution]
public class FTPSheepToolWindow : ToolWindow {
    private FTPSheepToolWindowControl? toolWindowControl;
    private readonly IProfileService profileService;
    private readonly JsonDeploymentHistoryService historyService;
    private readonly VsDeploymentOrchestrator deploymentOrchestrator;
    private readonly PublishProfileParser publishProfileParser;
    private readonly PublishProfileConverter publishProfileConverter;
    private readonly ILogger<FTPSheepToolWindow> logger;

    public FTPSheepToolWindow(
        VisualStudioExtensibility extensibility,
        IProfileService profileService,
        JsonDeploymentHistoryService historyService,
        VsDeploymentOrchestrator deploymentOrchestrator,
        PublishProfileParser publishProfileParser,
        PublishProfileConverter publishProfileConverter,
        ILogger<FTPSheepToolWindow> logger) : base(extensibility) {
        Title = "FTPSheep";
        this.profileService = profileService;
        this.historyService = historyService;
        this.deploymentOrchestrator = deploymentOrchestrator;
        this.publishProfileParser = publishProfileParser;
        this.publishProfileConverter = publishProfileConverter;
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
        logger.LogInformation("Initializing tool window");
        await Task.CompletedTask;
    }

    /// <summary>
    /// Create and return the tool window UI content.
    /// </summary>
    public override async Task<IRemoteUserControl> GetContentAsync(CancellationToken cancellationToken) {
        try {
            // Create the data context with command handlers
            var dataContext = new FTPSheepToolWindowData(
                deployCommand: OnDeployAsync,
                newProfileCommand: OnNewProfileAsync,
                editProfileCommand: OnEditProfileAsync,
                deleteProfileCommand: OnDeleteProfileAsync,
                importProfileCommand: OnImportProfileAsync);

            // Set up initial data
            dataContext.WelcomeMessage = "FTPSheep Deployment Tool";
            dataContext.Projects = [];
            dataContext.Profiles = [];
            dataContext.RecentDeployments = [];

            // Create the control with the data context
            toolWindowControl = new FTPSheepToolWindowControl(dataContext);

            // Load projects, profiles, and history asynchronously
            _ = Task.Run(async () => {
                try {
                    await LoadProjectsAsync(cancellationToken);
                    await RefreshProfilesListAsync(cancellationToken);
                    await RefreshDeploymentHistoryAsync(cancellationToken);
                } catch (Exception ex) {
                    logger.LogError(ex, "Failed to load initial data for tool window");
                }
            }, cancellationToken);

            return toolWindowControl;
        } catch (Exception ex) {
            // Log the exception details for debugging
            throw new InvalidOperationException($"Failed to create tool window content: {ex.GetType().Name} - {ex.Message}\nStack: {ex.StackTrace}", ex);
        }
    }

    /// <summary>
    /// Loads projects from the Visual Studio workspace.
    /// </summary>
    private async Task LoadProjectsAsync(CancellationToken cancellationToken) {
        if(toolWindowControl?.DataContext == null)
            return;

        var dataContext = toolWindowControl.DataContext;

        try {
            // Access the workspace through the extensibility API
            var workspace = Extensibility.Workspaces();
            var projectSnapshots = await workspace.QueryProjectsAsync(
                project => project.With(p => p.Name)
                                 .With(p => p.Path),
                cancellationToken);

            var projectList = new List<ProjectItem>();
            foreach(var projectSnapshot in projectSnapshots) {
                var projectPath = projectSnapshot.Path ?? string.Empty;
                var projectName = !string.IsNullOrEmpty(projectSnapshot.Name)
                    ? projectSnapshot.Name
                    : Path.GetFileNameWithoutExtension(projectPath) ?? "Unknown";

                projectList.Add(new ProjectItem {
                    Name = projectName,
                    Path = projectPath
                });

                logger.LogDebug("Processing project \"{0}\"", projectPath);
            }

            if(projectList.Count > 0) {
                dataContext.Projects = projectList;

                // Select the first project by default
                dataContext.SelectedProject = projectList[0].Name;

                logger.LogInformation($"Loaded {projectList.Count} projects from workspace");
            } else {
                // No projects in workspace
                dataContext.Projects = [
                    new() {
                        Name = "No projects in solution",
                        Path = string.Empty
                    }
                ];
                logger.LogWarning("No projects found in workspace");
            }
        } catch(Exception ex) {
            logger.LogError(ex, "Failed to load projects from workspace");
            // Show error in UI
            dataContext.Projects = [
                new() {
                    Name = "Error loading projects",
                    Path = string.Empty
                }
            ];
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
        logger.LogInformation("OnImportProfileAsync called - starting import process");

        try {
            logger.LogInformation("Creating file picker dialog options");

            // Show file picker dialog for .pubxml files
            var filters = new List<Microsoft.VisualStudio.Extensibility.Shell.FileDialog.DialogFilter> {
                new("Visual Studio Publish Profiles", new[] { "*.pubxml" }),
                new("All Files", new[] { "*.*" })
            };

            var options = new Microsoft.VisualStudio.Extensibility.Shell.FileDialog.FileDialogOptions {
                Title = "Import Visual Studio Publish Profile",
                Filters = new Microsoft.VisualStudio.Extensibility.Shell.FileDialog.DialogFilters(filters) {
                    DefaultFilterIndex = 0
                }
            };

            logger.LogInformation("Showing file picker dialog");
            var selectedFilePath = await Extensibility.Shell().ShowOpenFileDialogAsync(options, cancellationToken);
            logger.LogInformation($"File picker returned: {selectedFilePath ?? "(null)"}");

            // Check if user cancelled
            if(string.IsNullOrEmpty(selectedFilePath)) {
                logger.LogInformation("User cancelled import profile dialog");
                return;
            }

            logger.LogInformation($"Importing publish profile from: {selectedFilePath}");

            // Parse the .pubxml file (synchronous method)
            var publishProfile = publishProfileParser.ParseProfile(selectedFilePath);

            // Convert to DeploymentProfile
            var profile = publishProfileConverter.Convert(publishProfile);

            // Generate profile name based on the original file
            var originalName = Path.GetFileNameWithoutExtension(selectedFilePath);
            profile.Name = originalName;

            // Save the profile next to the .pubxml file
            var sourceDirectory = Path.GetDirectoryName(selectedFilePath) ?? Environment.CurrentDirectory;
            var profilePath = Path.Combine(sourceDirectory, $"{originalName}.ftpsheep");

            // Save the profile
            await profileService.CreateProfileAsync(profilePath, profile, cancellationToken);

            logger.LogInformation($"Successfully imported profile: {originalName} to {profilePath}");

            // Refresh the profiles list
            await RefreshProfilesListAsync(cancellationToken);

            // Select the newly imported profile
            if(toolWindowControl?.DataContext != null) {
                toolWindowControl.DataContext.SelectedProfile = originalName;
            }

            logger.LogInformation($"Profile '{originalName}' imported successfully from {selectedFilePath}");
        } catch(Exception ex) {
            logger.LogError(ex, "Failed to import publish profile");
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