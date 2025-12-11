using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using FTPSheep.BuildTools.Models;
using FTPSheep.BuildTools.Services;
using FTPSheep.Core.Models;
using FTPSheep.Core.Services;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using Spectre.Console.Cli;
using ValidationResult = Spectre.Console.ValidationResult;

namespace FTPSheep.CLI.Commands;

/// <summary>
/// Command to deploy a .NET application to FTP server.
/// </summary>
internal sealed class DeployCommand(ILogger<DeployCommand> logger) : Command<DeployCommand.Settings> {
    /// <summary>
    /// Settings for the deploy command.
    /// </summary>
    public sealed class Settings : CommandSettings {
        [Description("Name of the deployment profile to use")]
        [CommandOption("-p|--profile <PROFILE>")]
        public string? ProfileName { get; init; }

        [Description("Path to a profile JSON file")]
        [CommandOption("-f|--file <PATH>")]
        public string? ProfilePath { get; init; }

        [Description("Skip all confirmation prompts")]
        [CommandOption("-y|--yes")]
        public bool AutoConfirm { get; init; }

        [Description("Enable verbose output")]
        [CommandOption("-v|--verbose")]
        public bool Verbose { get; init; }

        [Description("Perform a dry run without making changes")]
        [CommandOption("--dry-run")]
        public bool DryRun { get; init; }

        [Description("Build configuration (Debug/Release)")]
        [CommandOption("-c|--configuration <CONFIG>")]
        [DefaultValue("Release")]
        public string Configuration { get; init; } = "Release";

        [Description("Skip the build step (use existing publish output)")]
        [CommandOption("--skip-build")]
        public bool SkipBuild { get; init; }

        [Description("Clean destination before uploading")]
        [CommandOption("--clean")]
        public bool CleanDestination { get; init; }

        [Description("Skip uploading app_offline.htm")]
        [CommandOption("--no-app-offline")]
        public bool NoAppOffline { get; init; }

        public override ValidationResult Validate() {
            if(!string.IsNullOrWhiteSpace(ProfilePath) && !File.Exists(ProfilePath)) {
                return ValidationResult.Error($"Profile file not found: {ProfilePath}");
            }
            return ValidationResult.Success();
        }
    }

    public override int Execute(CommandContext context, Settings settings, CancellationToken cancellationToken) {
        DisplayHeader(settings);

        try {
            // Phase 1: Profile Resolution
            var profile = ResolveAndLoadProfile(settings, cancellationToken);
            if(profile == null) {
                return 1;
            }

            // Validate profile
            var validationErrors = ValidateProfile(profile);
            if(validationErrors.Count > 0) {
                DisplayValidationErrors(validationErrors);
                return 1;
            }

            // Phase 2: Build Project
            PublishOutput? publishOutput;
            if(!settings.SkipBuild) {
                publishOutput = BuildProject(profile, settings, cancellationToken);
                if(publishOutput == null) {
                    return 1;
                }
            } else {
                publishOutput = ScanExistingPublishOutput(profile, settings);
                if(publishOutput == null) {
                    return 1;
                }
            }

            // Phase 3: Pre-deployment Summary
            DisplayPreDeploymentSummary(profile, publishOutput, settings);

            // Phase 4: Confirmation
            if(!settings.AutoConfirm && !settings.DryRun) {
                if(!ConfirmDeployment()) {
                    AnsiConsole.MarkupLine("[yellow]Deployment cancelled.[/]");
                    return 0;
                }
            }

            // Phase 5: Execute Deployment
            if(settings.DryRun) {
                DisplayDryRunSummary(profile, publishOutput);
                return 0;
            }

            var result = ExecuteDeployment(profile, publishOutput, settings, cancellationToken);

            // Phase 6: Display Results
            DisplayDeploymentResult(result);

            return result.Success ? 0 : 1;
        } catch(OperationCanceledException) {
            AnsiConsole.MarkupLine("\n[yellow]Deployment cancelled by user.[/]");
            return 1;
        } catch(Exception ex) {
            DisplayError(ex, settings.Verbose);
            return 1;
        }
    }

    #region Profile Resolution

    private DeploymentProfile? ResolveAndLoadProfile(Settings settings, CancellationToken ct) {
        DeploymentProfile? profile = null;

        AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .Start("Loading profile...", ctx => {
                // Priority 1: Explicit file path
                if(!string.IsNullOrWhiteSpace(settings.ProfilePath)) {
                    ctx.Status("Loading profile from file...");
                    profile = LoadProfileFromFile(settings.ProfilePath);
                    return;
                }

                // Priority 2: Profile name (from app data)
                if(!string.IsNullOrWhiteSpace(settings.ProfileName)) {
                    ctx.Status($"Loading profile '{settings.ProfileName}'...");
                    profile = LoadProfileByName(settings.ProfileName);
                    return;
                }

                // Priority 3: Auto-discover .pubxml files
                ctx.Status("Searching for publish profiles...");
            });

        // Handle auto-discovery outside of Status (needs user interaction)
        if(profile == null && string.IsNullOrWhiteSpace(settings.ProfilePath) && string.IsNullOrWhiteSpace(settings.ProfileName)) {
            profile = AutoDiscoverProfile(ct);
        }

        if(profile != null) {
            logger.LogDebug("Loaded profile: {Name}", profile.Name);
            AnsiConsole.MarkupLine($"[green]✓[/] Profile loaded: [cyan]{profile.Name}[/]");
        }

        return profile;
    }

    private DeploymentProfile? LoadProfileFromFile(string path) {
        if(!File.Exists(path)) {
            AnsiConsole.MarkupLine($"[red]Error:[/] Profile file not found: {path}");
            return null;
        }

        try {
            var json = File.ReadAllText(path);
            var options = new JsonSerializerOptions {
                PropertyNameCaseInsensitive = true,
                Converters = { new JsonStringEnumConverter() }
            };
            var profile = JsonSerializer.Deserialize<DeploymentProfile>(json, options);

            if(profile != null && string.IsNullOrWhiteSpace(profile.Name)) {
                profile.Name = Path.GetFileNameWithoutExtension(path);
            }

            return profile;
        } catch(Exception ex) {
            AnsiConsole.MarkupLine($"[red]Error:[/] Failed to parse profile: {ex.Message}");
            return null;
        }
    }

    private DeploymentProfile? LoadProfileByName(string profileName) {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var profilesDir = Path.Combine(appDataPath, ".ftpsheep", "profiles");
        var profilePath = Path.Combine(profilesDir, $"{profileName}.json");

        if(!File.Exists(profilePath)) {
            AnsiConsole.MarkupLine($"[red]Error:[/] Profile not found: {profileName}");
            DisplayProfileDiscoveryHelp();
            return null;
        }

        return LoadProfileFromFile(profilePath);
    }

    private DeploymentProfile? AutoDiscoverProfile(CancellationToken ct) {
        var parser = new PublishProfileParser();
        var converter = new PublishProfileConverter();

        var currentDir = Directory.GetCurrentDirectory();
        var profiles = parser.DiscoverProfiles(currentDir);

        if(profiles.Count == 0) {
            AnsiConsole.MarkupLine("[yellow]No publish profiles found.[/]");
            AnsiConsole.WriteLine();
            DisplayProfileDiscoveryHelp();
            return null;
        }

        string selectedPath;
        if(profiles.Count == 1) {
            selectedPath = profiles[0];
            AnsiConsole.MarkupLine($"Found profile: [cyan]{Path.GetFileName(selectedPath)}[/]");
        } else {
            var profileNames = profiles.Select(p => Path.GetFileName(p)).ToList();
            var selectedName = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Multiple profiles found. Select one:")
                    .AddChoices(profileNames));
            selectedPath = profiles[profileNames.IndexOf(selectedName)];
        }

        try {
            var pubProfile = parser.ParseProfile(selectedPath);
            var deploymentProfile = converter.Convert(pubProfile);

            if(string.IsNullOrWhiteSpace(deploymentProfile.Name)) {
                deploymentProfile.Name = Path.GetFileNameWithoutExtension(selectedPath);
            }

            // Try to find project file if not set
            if(string.IsNullOrWhiteSpace(deploymentProfile.ProjectPath)) {
                deploymentProfile.ProjectPath = FindProjectFile(selectedPath) ?? string.Empty;
            }

            return deploymentProfile;
        } catch(Exception ex) {
            AnsiConsole.MarkupLine($"[red]Error:[/] Failed to parse profile: {ex.Message}");
            return null;
        }
    }

    private List<string> ValidateProfile(DeploymentProfile profile) {
        var errors = new List<string>();

        if(string.IsNullOrWhiteSpace(profile.Connection.Host)) {
            errors.Add("Server host is required.");
        }

        if(string.IsNullOrWhiteSpace(profile.ProjectPath)) {
            errors.Add("Project path is required.");
        } else if(!File.Exists(profile.ProjectPath)) {
            errors.Add($"Project file not found: {profile.ProjectPath}");
        }

        return errors;
    }

    #endregion

    #region Build Project

    private PublishOutput? BuildProject(DeploymentProfile profile, Settings settings, CancellationToken ct) {
        var buildService = new BuildService();
        var scanner = new PublishOutputScanner();

        // Determine output path
        var projectDir = Path.GetDirectoryName(profile.ProjectPath) ?? Directory.GetCurrentDirectory();
        var publishPath = Path.Combine(projectDir, "bin", settings.Configuration, "publish");

        AnsiConsole.WriteLine();

        BuildResult? buildResult = null;

        AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .Start("Building project...", ctx => {
                ctx.Status($"Publishing ({settings.Configuration})...");
                logger.LogDebug("Publishing to: {Path}", publishPath);

                buildResult = buildService.PublishAsync(
                    profile.ProjectPath,
                    publishPath,
                    settings.Configuration,
                    ct).GetAwaiter().GetResult();
            });

        if(buildResult == null || !buildResult.Success) {
            AnsiConsole.MarkupLine("[red]✗[/] Build failed.");
            if(buildResult?.Errors.Count > 0) {
                foreach(var error in buildResult.Errors.Take(10)) {
                    AnsiConsole.MarkupLine($"  [red]{error}[/]");
                }
                if(buildResult.Errors.Count > 10) {
                    AnsiConsole.MarkupLine($"  [dim]...and {buildResult.Errors.Count - 10} more errors[/]");
                }
            }
            return null;
        }

        AnsiConsole.MarkupLine("[green]✓[/] Build succeeded");

        // Scan publish output
        return ScanPublishOutput(publishPath, profile, settings);
    }

    private PublishOutput? ScanExistingPublishOutput(DeploymentProfile profile, Settings settings) {
        var projectDir = Path.GetDirectoryName(profile.ProjectPath) ?? Directory.GetCurrentDirectory();
        var publishPath = Path.Combine(projectDir, "bin", settings.Configuration, "publish");

        if(!Directory.Exists(publishPath)) {
            AnsiConsole.MarkupLine($"[red]Error:[/] Publish output not found: {publishPath}");
            AnsiConsole.MarkupLine("[dim]Run without --skip-build to build the project first.[/]");
            return null;
        }

        AnsiConsole.MarkupLine($"[green]✓[/] Using existing publish output");

        return ScanPublishOutput(publishPath, profile, settings);
    }

    private PublishOutput? ScanPublishOutput(string publishPath, DeploymentProfile profile, Settings settings) {
        var scanner = new PublishOutputScanner();

        PublishOutput? publishOutput = null;

        AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .Start("Scanning publish output...", ctx => {
                publishOutput = scanner.ScanPublishOutput(publishPath, profile.ExclusionPatterns);
            });

        if(publishOutput == null) {
            AnsiConsole.MarkupLine("[red]Error:[/] Failed to scan publish output.");
            return null;
        }

        if(!publishOutput.IsValid) {
            AnsiConsole.MarkupLine("[red]Error:[/] Publish output validation failed:");
            foreach(var error in publishOutput.Errors) {
                AnsiConsole.MarkupLine($"  [red]✗[/] {error}");
            }
            return null;
        }

        AnsiConsole.MarkupLine($"[green]✓[/] Found {publishOutput.FileCount} files ({publishOutput.FormattedTotalSize})");

        return publishOutput;
    }

    #endregion

    #region Deployment Execution

    private DeploymentResult ExecuteDeployment(
        DeploymentProfile profile,
        PublishOutput publishOutput,
        Settings settings,
        CancellationToken ct) {

        AnsiConsole.WriteLine();

        var coordinator = new DeploymentCoordinator(
            profileService: null,  // Not used in this flow - profile already loaded
            buildService: null,    // Not used in this flow - project already built
            historyService: null,  // History recording not implemented yet
            appOfflineManager: new AppOfflineManager(profile.AppOfflineTemplate),
            exclusionMatcher: ExclusionPatternMatcher.CreateWithDefaults(profile.ExclusionPatterns)
        );

        DeploymentResult? result = null;

        // Handle Ctrl+C
        Console.CancelKeyPress += (s, e) => {
            e.Cancel = true;
            coordinator.CancelDeployment();
            AnsiConsole.MarkupLine("\n[yellow]Cancellation requested...[/]");
        };

        AnsiConsole.Progress()
            .AutoClear(false)
            .HideCompleted(false)
            .Columns(
                new TaskDescriptionColumn(),
                new ProgressBarColumn(),
                new PercentageColumn(),
                new SpinnerColumn()
            )
            .Start(ctx => {
                // Create progress tasks
                var stageTask = ctx.AddTask("[cyan]Deployment[/]", maxValue: 9);
                var uploadTask = ctx.AddTask("[dim]Uploading files[/]", maxValue: publishOutput.FileCount);
                uploadTask.IsIndeterminate = true;

                // Subscribe to events
                coordinator.StageChanged += (s, e) => {
                    stageTask.Description = GetStageDescription(e.Stage);
                    stageTask.Value = (int)e.Stage;

                    if(e.Stage == DeploymentStage.UploadingFiles) {
                        uploadTask.IsIndeterminate = false;
                        uploadTask.Description = "[cyan]Uploading files[/]";
                    }

                    if(settings.Verbose) {
                        logger.LogDebug("Stage: {Stage}", e.Stage);
                    }
                };

                coordinator.ProgressUpdated += (s, e) => {
                    uploadTask.Value = e.State.FilesUploaded;
                    uploadTask.Description = $"[cyan]Uploading[/] ({e.State.FilesUploaded}/{e.State.TotalFiles})";
                };

                // Build deployment options with pre-loaded data
                var options = new DeploymentOptions {
                    ProfileName = profile.Name,
                    ProjectPath = profile.ProjectPath,
                    TargetHost = profile.Connection.Host,
                    UseAppOffline = !settings.NoAppOffline && profile.AppOfflineEnabled,
                    CleanupMode = settings.CleanDestination || profile.CleanupMode != CleanupMode.None,
                    SkipConfirmation = true, // Already confirmed
                    DryRun = settings.DryRun,
                    BuildConfiguration = settings.Configuration,
                    MaxConcurrentUploads = profile.Concurrency,
                    Profile = profile,           // Pass pre-loaded profile
                    PublishOutput = publishOutput // Pass pre-built output
                };

                // Execute deployment
                result = coordinator.ExecuteDeploymentAsync(options, ct).GetAwaiter().GetResult();

                // Update final state
                stageTask.Value = 9;
                if(result.Success) {
                    stageTask.Description = "[green]Complete[/]";
                    uploadTask.Description = $"[green]Uploaded[/] ({result.FilesUploaded} files)";
                } else if(result.WasCancelled) {
                    stageTask.Description = "[yellow]Cancelled[/]";
                } else {
                    stageTask.Description = "[red]Failed[/]";
                }

                stageTask.StopTask();
                uploadTask.StopTask();
            });

        return result!;
    }

    private static string GetStageDescription(DeploymentStage stage) => stage switch {
        DeploymentStage.NotStarted => "[dim]Starting...[/]",
        DeploymentStage.LoadingProfile => "Loading profile...",
        DeploymentStage.BuildingProject => "Building project...",
        DeploymentStage.ConnectingToServer => "Connecting to server...",
        DeploymentStage.PreDeploymentSummary => "Preparing deployment...",
        DeploymentStage.UploadingAppOffline => "Uploading app_offline.htm...",
        DeploymentStage.UploadingFiles => "Uploading files...",
        DeploymentStage.CleaningUpObsoleteFiles => "Cleaning obsolete files...",
        DeploymentStage.DeletingAppOffline => "Removing app_offline.htm...",
        DeploymentStage.RecordingHistory => "Recording history...",
        DeploymentStage.Completed => "[green]Completed[/]",
        DeploymentStage.Failed => "[red]Failed[/]",
        DeploymentStage.Cancelled => "[yellow]Cancelled[/]",
        _ => stage.ToString()
    };

    #endregion

    #region Display Methods

    private void DisplayHeader(Settings settings) {
        AnsiConsole.MarkupLine("[bold green]FTPSheep.NET[/] - Deployment Tool");

        if(settings.DryRun) {
            AnsiConsole.MarkupLine("[cyan]Mode: DRY RUN[/]");
        }

        AnsiConsole.WriteLine();
    }

    private void DisplayPreDeploymentSummary(DeploymentProfile profile, PublishOutput output, Settings settings) {
        AnsiConsole.WriteLine();

        var grid = new Grid();
        grid.AddColumn();
        grid.AddColumn();
        grid.AddRow("[bold]Profile[/]", profile.Name);
        grid.AddRow("[bold]Server[/]", profile.Connection.GetConnectionString());
        grid.AddRow("[bold]Remote Path[/]", profile.RemotePath);
        grid.AddRow("[bold]Project[/]", Path.GetFileName(profile.ProjectPath));
        grid.AddRow("[bold]Files[/]", $"{output.FileCount:N0}");
        grid.AddRow("[bold]Total Size[/]", output.FormattedTotalSize);
        grid.AddRow("[bold]App Offline[/]", (!settings.NoAppOffline && profile.AppOfflineEnabled) ? "[green]Yes[/]" : "[dim]No[/]");
        grid.AddRow("[bold]Cleanup Mode[/]", profile.CleanupMode.ToString());

        var panel = new Panel(grid) {
            Header = new PanelHeader(" Deployment Summary ", Justify.Center),
            Border = BoxBorder.Rounded
        };

        AnsiConsole.Write(panel);

        if(output.HasWarnings) {
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[yellow]Warnings:[/]");
            foreach(var warning in output.Warnings) {
                AnsiConsole.MarkupLine($"  [yellow]![/] {warning}");
            }
        }

        AnsiConsole.WriteLine();
    }

    private void DisplayDryRunSummary(DeploymentProfile profile, PublishOutput output) {
        AnsiConsole.WriteLine();

        var rule = new Rule("[cyan]DRY RUN - No changes made[/]") { Style = Style.Parse("cyan") };
        AnsiConsole.Write(rule);

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("The following actions [bold]would[/] be performed:");
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"  1. Connect to [cyan]{profile.Connection.GetConnectionString()}[/]");

        if(profile.AppOfflineEnabled) {
            AnsiConsole.MarkupLine($"  2. Upload [cyan]app_offline.htm[/] to {profile.RemotePath}");
        }

        AnsiConsole.MarkupLine($"  3. Upload [cyan]{output.FileCount:N0}[/] files ({output.FormattedTotalSize})");

        if(profile.CleanupMode != CleanupMode.None) {
            AnsiConsole.MarkupLine($"  4. Clean up obsolete files (mode: {profile.CleanupMode})");
        }

        if(profile.AppOfflineEnabled) {
            AnsiConsole.MarkupLine($"  5. Remove [cyan]app_offline.htm[/]");
        }

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[green]✓[/] Dry run completed successfully.");
    }

    private void DisplayDeploymentResult(DeploymentResult result) {
        AnsiConsole.WriteLine();

        if(result.Success) {
            var rule = new Rule("[green]Deployment Successful[/]") { Style = Style.Parse("green") };
            AnsiConsole.Write(rule);

            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine($"  [green]✓[/] {result.FilesUploaded:N0} files uploaded");
            AnsiConsole.MarkupLine($"  [green]✓[/] {FormatBytes(result.SizeUploaded)} transferred");

            if(!string.IsNullOrWhiteSpace(result.FormattedUploadSpeed)) {
                AnsiConsole.MarkupLine($"  [green]✓[/] Average speed: {result.FormattedUploadSpeed}");
            }

            if(result.Duration.HasValue) {
                AnsiConsole.MarkupLine($"  [green]✓[/] Duration: {result.Duration.Value:mm\\:ss}");
            }

            if(result.ObsoleteFilesDeleted > 0) {
                AnsiConsole.MarkupLine($"  [green]✓[/] {result.ObsoleteFilesDeleted} obsolete files removed");
            }
        } else {
            var rule = new Rule("[red]Deployment Failed[/]") { Style = Style.Parse("red") };
            AnsiConsole.Write(rule);

            AnsiConsole.WriteLine();

            if(result.WasCancelled) {
                AnsiConsole.MarkupLine("  [yellow]Deployment was cancelled by user.[/]");
            }

            foreach(var error in result.ErrorMessages) {
                AnsiConsole.MarkupLine($"  [red]✗[/] {error}");
            }

            if(result.FilesUploaded > 0) {
                AnsiConsole.MarkupLine($"  [dim]Partial upload: {result.FilesUploaded}/{result.TotalFiles} files[/]");
            }
        }

        if(result.WarningMessages.Count > 0) {
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[yellow]Warnings:[/]");
            foreach(var warning in result.WarningMessages) {
                AnsiConsole.MarkupLine($"  [yellow]![/] {warning}");
            }
        }

        AnsiConsole.WriteLine();
    }

    private void DisplayValidationErrors(List<string> errors) {
        AnsiConsole.MarkupLine("[red]Profile validation failed:[/]");
        foreach(var error in errors) {
            AnsiConsole.MarkupLine($"  [red]✗[/] {error}");
        }
    }

    private void DisplayError(Exception ex, bool verbose) {
        AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");

        if(verbose && ex.InnerException != null) {
            AnsiConsole.MarkupLine($"[dim]Inner: {ex.InnerException.Message}[/]");
        }

        if(verbose) {
            AnsiConsole.MarkupLine($"[dim]{ex.GetType().Name}[/]");
        }
    }

    private void DisplayProfileDiscoveryHelp() {
        AnsiConsole.MarkupLine("[dim]Available commands:[/]");
        AnsiConsole.MarkupLine("  [cyan]ftpsheep deploy --profile <name>[/]  - Use a saved profile");
        AnsiConsole.MarkupLine("  [cyan]ftpsheep deploy --file <path>[/]     - Use a profile JSON file");
        AnsiConsole.MarkupLine("  [cyan]ftpsheep import[/]                   - Import a .pubxml file");
        AnsiConsole.MarkupLine("  [cyan]ftpsheep profile list[/]             - List saved profiles");
    }

    private static bool ConfirmDeployment() {
        return AnsiConsole.Confirm("Proceed with deployment?", true);
    }

    #endregion

    #region Helper Methods

    private static string? FindProjectFile(string pubxmlPath) {
        var pubxmlDir = Path.GetDirectoryName(pubxmlPath);
        if(string.IsNullOrEmpty(pubxmlDir)) {
            return null;
        }

        var currentDir = new DirectoryInfo(pubxmlDir);

        while(currentDir != null) {
            var projectExtensions = new[] { "*.csproj", "*.vbproj", "*.fsproj" };

            foreach(var extension in projectExtensions) {
                var projectFiles = currentDir.GetFiles(extension, SearchOption.TopDirectoryOnly);
                if(projectFiles.Length > 0) {
                    return projectFiles[0].FullName;
                }
            }

            currentDir = currentDir.Parent;
        }

        return null;
    }

    private static string FormatBytes(long bytes) {
        string[] sizes = ["B", "KB", "MB", "GB"];
        double len = bytes;
        var order = 0;
        while(len >= 1024 && order < sizes.Length - 1) {
            order++;
            len /= 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }

    #endregion
}
