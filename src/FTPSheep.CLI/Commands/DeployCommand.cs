using System.ComponentModel;
using System.Diagnostics;
using FluentFTP;
using FTPSheep.BuildTools.Models;
using FTPSheep.BuildTools.Services;
using FTPSheep.Core.Interfaces;
using FTPSheep.Core.Models;
using FTPSheep.Core.Services;
using FTPSheep.Protocols.Models;
using FTPSheep.Protocols.Services;
using FTPSheep.Utilities.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Spectre.Console;
using Spectre.Console.Cli;
using BuildResult = FTPSheep.BuildTools.Models.BuildResult;
using DeploymentStage = FTPSheep.Core.Models.DeploymentStage;
using ValidationResult = Spectre.Console.ValidationResult;

namespace FTPSheep.CLI.Commands;

/// <summary>
/// Command to deploy a .NET application to FTP server.
/// </summary>
internal sealed class DeployCommand(IProfileService profiles, FtpClientFactory ftpClientFactory, ILogger<DeployCommand> logger) : AsyncCommand<DeployCommand.Settings> {
    /// <summary>
    /// Settings for the deploy command.
    /// </summary>
    public sealed class Settings : CommandSettings {
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

        [Description("Skip FTP connection validation before building")]
        [CommandOption("--skip-connection-test")]
        public bool SkipConnectionTest { get; init; }

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

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken) {
        DisplayHeader(settings);

        try {
            // Phase 1: Profile Resolution
            var (profile, profileFullPath) = await ResolveAndLoadProfile(settings, cancellationToken);

            if(profile == null) {
                return 1;
            }

            Console.Title = "FTPSheep - " + profile.Name;

            // Validate profile
            var validationErrors = ValidateProfile(profile);

            if(validationErrors.Count > 0) {
                logger
                    .BuildErrorMessage("Profile validation failed")
                    .Add("Errors", validationErrors)
                    .AddAsJson("Profile", profile)
                    .Write();
                DisplayValidationErrors(validationErrors);
                return 1;
            }

            // Phase 2: Validate FTP Connection (before building)
            if(!settings.SkipConnectionTest) {
                var connectionValid = await ValidateFtpConnection(profile, profileFullPath, cancellationToken);

                if(!connectionValid) {
                    return 1;
                }
            } else {
                AnsiConsole.MarkupLine("[dim]FTP connection validation skipped[/]");
            }

            // Phase 3: Build Project
            PublishOutput? publishOutput;

            if(!settings.SkipBuild) {
                publishOutput = await BuildProject(profile, settings, cancellationToken);

                if(publishOutput == null) {
                    return 1;
                }
            } else {
                publishOutput = ScanExistingPublishOutput(profile, settings);

                if(publishOutput == null) {
                    return 1;
                }
            }

            // Phase 4: Pre-deployment Summary
            DisplayPreDeploymentSummary(profile, publishOutput, settings);

            // Phase 5: Confirmation
            if(!settings.AutoConfirm && !settings.DryRun) {
                if(!ConfirmDeployment()) {
                    AnsiConsole.MarkupLine("[yellow]Deployment cancelled.[/]");
                    return 0;
                }
            }

            // Phase 6: Execute Deployment
            if(settings.DryRun) {
                DisplayDryRunSummary(profile, publishOutput);
                return 0;
            }

            var result = await ExecuteDeployment(profile, publishOutput, settings, cancellationToken);

            logger.BuildDebugMessage("Deployment result")
                .AddAsJson("Result", result)
                .Write();

            // Phase 6: Display Results
            DisplayDeploymentResult(result, profile);

            Console.Title = "✅" + profile.Name + " - FTPSheep";

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

    private async Task<(DeploymentProfile?, string? FileFullPath)> ResolveAndLoadProfile(Settings settings, CancellationToken ct) {
        DeploymentProfile? profile = null;
        string? actualProfilePath = null;

        await AnsiConsole
            .Status()
            .Spinner(Spinner.Known.Dots)
            .StartAsync("Loading profile...", async ctx => {
                // Priority 1: Explicit file path
                if(!string.IsNullOrWhiteSpace(settings.ProfilePath)) {
                    // Warn if file doesn't have .ftpsheep extension
                    if(!settings.ProfilePath.EndsWith(".ftpsheep", StringComparison.OrdinalIgnoreCase)) {
                        ctx.Status(""); // Clear status to show warning
                        AnsiConsole.MarkupLine("[yellow]Warning:[/] The specified file does not have a .ftpsheep extension.");
                        AnsiConsole.MarkupLine("[dim]Expected extension: .ftpsheep[/]");
                        AnsiConsole.WriteLine();
                    }

                    ctx.Status("Loading profile from file...");
                    profile = await LoadProfileFromFile(settings.ProfilePath);
                    actualProfilePath = Path.GetFullPath(settings.ProfilePath);
                    return;
                }

                // Priority 3: Auto-discover .pubxml files
                ctx.Status("Searching for publish profiles...");
            });

        // Handle auto-discovery outside of Status (needs user interaction)
        if(profile == null && string.IsNullOrWhiteSpace(actualProfilePath)) {
            (profile, actualProfilePath) = await AutoDiscoverProfile(ct);
        }

        if(profile != null) {
            logger
                .BuildDebugMessage("Loaded profile: {0}", profile.Name)
                .Add("Actual Path", actualProfilePath)
                .Add("FTP Username", profile.Username)
                .Add("FTP password present", !string.IsNullOrEmpty(profile.Password))
                .Write();
            AnsiConsole.MarkupLine($"[green]✓[/] Profile loaded: [cyan]{profile.Name}[/]");
        }

        return (profile, actualProfilePath);
    }

    private async Task<DeploymentProfile?> LoadProfileFromFile(string path) {
        try {
            return await profiles.LoadProfileAsync(path);
        } catch(Exception ex) {
            logger.LogException(ex);
            AnsiConsole.MarkupLine($"[red]Error:[/] Failed to load profile: {ex.Message}");
            return null;
        }
    }

    private async Task<(DeploymentProfile?, string? FilePath)> AutoDiscoverProfile(CancellationToken ct) {
        var scanner = new FtpSheepProfileScanner();
        var currentDir = Directory.GetCurrentDirectory();

        // SAFETY CHECK FIRST
        if(scanner.IsSystemDirectory(currentDir, out var warningMessage)) {
            ShowSystemDirectoryWarning(currentDir, warningMessage);

            if(!ConfirmScanSystemDirectory()) {
                AnsiConsole.MarkupLine("[yellow]Scan cancelled. Use --file to specify a profile.[/]");
                return (null, null);
            }
        }

        // Search for .ftpsheep files ONLY
        var (profile, path) = await ScanForFtpSheepProfiles(scanner, currentDir, ct);

        if(profile != null) {
            return (profile, path);
        }

        // Nothing found
        AnsiConsole.MarkupLine("[yellow]No .ftpsheep profiles found.[/]");
        DisplayProfileDiscoveryHelp();
        return (null, null);
    }

    private async Task<(DeploymentProfile?, string? FilePath)> ScanForFtpSheepProfiles(FtpSheepProfileScanner scanner, string currentDir, CancellationToken ct) {
        logger.LogDebug("Scanning for FTPSheep profiles");
        List<string>? profilePaths = null;
        var filesFound = 0;

        AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .Start("Searching for .ftpsheep profiles...", ctx => {
                profilePaths = scanner.DiscoverProfiles(currentDir, cancellationToken: ct);
                filesFound = profilePaths.Count;

                if(filesFound > 0) {
                    ctx.Status($"Found {filesFound} .ftpsheep file(s)");
                }
            });

        if(profilePaths == null || profilePaths.Count == 0) {
            logger.LogDebug("No profiles found");
            return (null, null);
        }

        logger
            .BuildDebugMessage("Found {0} profiles", profilePaths.Count)
            .Add("Paths", profilePaths.Order())
            .Write();

        // Show warning if we hit the limit
        if(filesFound >= 50) {
            AnsiConsole.MarkupLine("[yellow]Warning:[/] Search limit reached (50 files). Some profiles may not be shown.");
            AnsiConsole.MarkupLine("[dim]Use --file to specify a profile directly if yours is missing.[/]");
            AnsiConsole.WriteLine();
        }

        string selectedPath;

        if(profilePaths.Count == 1) {
            selectedPath = profilePaths[0];
            var relativePath = Path.GetRelativePath(currentDir, selectedPath);
            AnsiConsole.MarkupLine($"Found profile: [cyan]{relativePath}[/]");
        } else {
            // Show relative paths for better UX
            var profileChoices = profilePaths
                .Select(p => new {
                    FullPath = p,
                    DisplayPath = Path.GetRelativePath(currentDir, p)
                })
                .OrderBy(p => p.DisplayPath)
                .ToList();

            var selectedDisplay = AnsiConsole.Prompt(new SelectionPrompt<string>()
                .Title($"Multiple .ftpsheep profiles found ({profileChoices.Count}). Select one:")
                .PageSize(15)
                .AddChoices(profileChoices.Select(p => p.DisplayPath)));

            selectedPath = profileChoices.First(p => p.DisplayPath == selectedDisplay).FullPath;
        }

        // Load the profile using ProfileService
        try {
            logger.LogDebug("Loading selected profile from the path: {0}", selectedPath);
            return (await profiles.LoadProfileAsync(selectedPath, ct), selectedPath);
        } catch(Exception ex) {
            AnsiConsole.MarkupLine($"[red]Error:[/] Failed to load profile: {ex.Message}");
            return (null, null);
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

    private async Task<PublishOutput?> BuildProject(DeploymentProfile profile, Settings settings, CancellationToken ct) {
        var buildService = new BuildService();
        var scanner = new PublishOutputScanner();

        // Determine output path
        var projectDir = Path.GetDirectoryName(profile.ProjectPath) ?? Directory.GetCurrentDirectory();
        var publishPath = Path.Combine(projectDir, "bin", settings.Configuration, "publish");

        logger.LogDebug("Project dir: {Path}", projectDir);
        logger.LogDebug("Publishing to: {Path}", publishPath);

        AnsiConsole.WriteLine();

        BuildResult? buildResult = null;

        var sw = Stopwatch.StartNew();

        await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .StartAsync("Building project...", async ctx => {
                ctx.Status($"Publishing ({settings.Configuration})...");

                buildResult = await buildService.PublishAsync(profile.ProjectPath, publishPath, settings.Configuration, ct);
            });

        logger
            .BuildDebugMessage("Project build completed")
            .Add("Time taken", sw.Elapsed)
            .AddAsJson("Result", buildResult)
            .Write();

        if(buildResult is not { Success: true }) {
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

    #region FTP Connection Validation

    private async Task<bool> ValidateFtpConnection(DeploymentProfile profile, string? profilePath, CancellationToken ct) {
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[cyan]Validating FTP connection...[/]");

        var password = profile.Password;
        var passwordEnteredInteractively = false;
        var shouldSavePassword = false;
        const int maxRetries = 3;
        var retryCount = 0;

        while(retryCount < maxRetries) {
            try {
                // Check if password is missing and prompt for it
                if(string.IsNullOrEmpty(password) || retryCount > 0) {
                    if(retryCount == 0) {
                        AnsiConsole.MarkupLine("[yellow]Password not found in profile.[/]");
                    } else {
                        AnsiConsole.MarkupLine($"[yellow]Authentication failed. Please try again ({retryCount}/{maxRetries} attempts)[/]");
                    }

                    password = AnsiConsole.Prompt(new TextPrompt<string>($"Enter password for [cyan]{profile.Username ?? "user"}[/]@[cyan]{profile.Connection.Host}[/]:")
                        .PromptStyle("red")
                        .Secret());

                    passwordEnteredInteractively = true;

                    // Ask if user wants to save the password (only on first attempt)
                    if(retryCount == 0) {
                        shouldSavePassword = await AnsiConsole.ConfirmAsync("Save password to profile for future use?", false, ct);
                    }

                    if(shouldSavePassword) {
                        profile.Password = password;
                    }
                }

                // Create FTP connection configuration
                var ftpConfig = new FtpConnectionConfig {
                    Host = profile.Connection.Host,
                    Port = profile.Connection.Port,
                    Username = profile.Username ?? string.Empty,
                    Password = password ?? string.Empty,
                    RemoteRootPath = profile.RemotePath,
                    EncryptionMode = profile.Connection.UseSsl
                        ? FtpEncryptionMode.Explicit
                        : FtpEncryptionMode.None
                };

                logger.LogDebug("Validating connection to {Host}:{Port}", ftpConfig.Host, ftpConfig.Port);

                // Create FTP client factory and client
                var factory = new FtpClientFactory(NullLoggerFactory.Instance);
                using var client = factory.CreateClient(ftpConfig);

                // Connect to server
                await client.ConnectAsync(ct);
                AnsiConsole.MarkupLine($"[green]✓[/] Connected to {ftpConfig.Host}:{ftpConfig.Port}");

                // Test write permissions
                var canWrite = await client.TestConnectionAsync(profile.RemotePath, ct);

                if(!canWrite) {
                    AnsiConsole.MarkupLine($"[red]✗[/] Cannot write to remote path: {profile.RemotePath}");
                    AnsiConsole.MarkupLine("[yellow]Hint:[/] Check directory permissions on the FTP server.");
                    return false;
                }

                AnsiConsole.MarkupLine($"[green]✓[/] Write permissions verified for {profile.RemotePath}");

                // Save the password if it was entered interactively and user wants to save it
                if(passwordEnteredInteractively && shouldSavePassword && !string.IsNullOrEmpty(profilePath)) {
                    try {
                        logger.LogInformation("Saving entered password for profile: " + profilePath);
                        await profiles.UpdatePasswordAsync(profilePath, profile.Username ?? string.Empty, password ?? string.Empty, ct);
                        AnsiConsole.MarkupLine("[green]✓[/] Password saved to credential store");
                    } catch(Exception ex) {
                        AnsiConsole.MarkupLine($"[yellow]Warning:[/] Failed to save password: {ex.Message}");
                    }
                }

                return true;
            } catch(Exception ex) {
                var errorMsg = $"Failed to validate FTP connection to {profile.Connection.Host}:{profile.Connection.Port}";
                var isAuthenticationError = ex.Message.Contains("authentication", StringComparison.OrdinalIgnoreCase) ||
                                           ex.Message.Contains("login", StringComparison.OrdinalIgnoreCase) ||
                                           ex.Message.Contains("530", StringComparison.OrdinalIgnoreCase); // FTP 530 = Login incorrect

                // Add helpful context based on error type
                if(isAuthenticationError) {
                    errorMsg = $"{errorMsg} - Authentication failed";
                    AnsiConsole.MarkupLine($"[red]✗[/] {errorMsg}");

                    retryCount++;

                    if(retryCount < maxRetries) {
                        // Reset password to prompt again
                        password = null;
                        continue; // Retry with new password
                    } else {
                        AnsiConsole.MarkupLine("[red]Maximum retry attempts reached.[/]");
                        AnsiConsole.MarkupLine("[yellow]Hint:[/] Check username and password in your profile.");
                    }
                } else if(ex.Message.Contains("timeout", StringComparison.OrdinalIgnoreCase)) {
                    errorMsg = $"{errorMsg} - Connection timeout";
                    AnsiConsole.MarkupLine($"[red]✗[/] {errorMsg}");
                    AnsiConsole.MarkupLine("[yellow]Hint:[/] Check server address, port, and firewall settings.");
                } else if(ex.Message.Contains("refused", StringComparison.OrdinalIgnoreCase)) {
                    errorMsg = $"{errorMsg} - Connection refused";
                    AnsiConsole.MarkupLine($"[red]✗[/] {errorMsg}");
                    AnsiConsole.MarkupLine("[yellow]Hint:[/] Check if FTP server is running and accessible.");
                } else {
                    AnsiConsole.MarkupLine($"[red]✗[/] {errorMsg}");
                    AnsiConsole.MarkupLine($"[dim]Error: {ex.Message}[/]");
                }

                logger.LogException(ex, "FTP connection validation failed");
                return false;
            }
        }

        return false;
    }

    #endregion

    #region Deployment Execution

    private async Task<DeploymentResult> ExecuteDeployment(DeploymentProfile profile,
        PublishOutput publishOutput,
        Settings settings,
        CancellationToken ct) {
        AnsiConsole.WriteLine();

        var coordinator = new DeploymentCoordinator(ftpClientFactory: ftpClientFactory,
            profileService: null, // Not used in this flow - profile already loaded
            buildService: null, // Not used in this flow - project already built
            historyService: null, // History recording not implemented yet
            appOfflineManager: new AppOfflineManager(profile.AppOfflineTemplate),
            exclusionMatcher: ExclusionPatternMatcher.CreateWithDefaults(profile.ExclusionPatterns),
            logger: logger);

        DeploymentResult? result = null;

        // Handle Ctrl+C
        Console.CancelKeyPress += (s, e) => {
            e.Cancel = true;
            coordinator.CancelDeployment();
            AnsiConsole.MarkupLine("\n[yellow]Cancellation requested...[/]");
        };

        await AnsiConsole
            .Progress()
            .AutoClear(false)
            .HideCompleted(false)
            .Columns(new TaskDescriptionColumn(),
                new ProgressBarColumn(),
                new PercentageColumn(),
                new SpinnerColumn())
            .StartAsync(async ctx => {
                // Create progress tasks
                var stageTask = ctx.AddTask("[cyan]Deployment[/]", maxValue: 10);
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
                    SkipConnectionTest = true, // Already validated in CLI before building
                    DryRun = settings.DryRun,
                    BuildConfiguration = settings.Configuration,
                    MaxConcurrentUploads = profile.Concurrency,
                    Profile = profile, // Pass pre-loaded profile
                    PublishOutput = publishOutput // Pass pre-built output
                };

                // Execute deployment
                result = await coordinator.ExecuteDeploymentAsync(options, ct);

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

    private static string GetStageDescription(DeploymentStage stage) =>
        stage switch {
            DeploymentStage.NotStarted => "[dim]Starting...[/]",
            DeploymentStage.LoadingProfile => "Loading profile...",
            DeploymentStage.ValidatingConnection => "Validating FTP connection...",
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

    private void DisplayDeploymentResult(DeploymentResult result, DeploymentProfile? profile = null) {
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

            AnsiConsole.WriteLine();

            // Open URL if configured
            if(profile != null) {
                OpenUrlIfConfigured(profile);
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

    private void OpenUrlIfConfigured(DeploymentProfile profile) {
        if(string.IsNullOrWhiteSpace(profile.SiteUrlToLaunchAfterPublish)) {
            return;
        }

        if(!profile.LaunchSiteAfterPublish) {
            return;
        }

        try {
            AnsiConsole.MarkupLine($"[grey]Opening URL:[/] [link]{profile.SiteUrlToLaunchAfterPublish}[/]");

            // Open URL in default browser
            var psi = new System.Diagnostics.ProcessStartInfo {
                FileName = profile.SiteUrlToLaunchAfterPublish,
                UseShellExecute = true
            };
            System.Diagnostics.Process.Start(psi);
        } catch(Exception ex) {
            // Log warning but don't fail deployment
            AnsiConsole.MarkupLine($"[yellow]Warning:[/] Failed to open URL: {ex.Message}");
            logger.LogWarning(ex, "Failed to open post-deployment URL: {Url}",
                profile.SiteUrlToLaunchAfterPublish);
        }
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
        AnsiConsole.MarkupLine("  [cyan]ftpsheep deploy --file <path>[/]     - Use a .ftpsheep or .pubxml file");
        AnsiConsole.MarkupLine("  [cyan]ftpsheep import[/]                   - Import a .pubxml file to .ftpsheep");
        AnsiConsole.MarkupLine("  [cyan]ftpsheep profile list[/]             - List saved profiles");
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[dim]Profile discovery searches for:[/]");
        AnsiConsole.MarkupLine("  - .ftpsheep files in current directory and subdirectories");
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[dim]Use --file to specify a .pubxml or other profile file directly.[/]");
    }

    private void ShowSystemDirectoryWarning(string path, string? message) {
        AnsiConsole.WriteLine();

        var panel = new Panel(new Markup(
            $"[yellow]⚠ Warning: Potentially unsafe directory[/]\n\n" +
            $"Current directory: [cyan]{path}[/]\n" +
            $"Reason: {message ?? "Unknown"}\n\n" +
            $"Scanning this location may be [red]very slow[/] and could find hundreds of files.\n" +
            $"Consider running this command from your project directory instead."))
        {
            Border = BoxBorder.Heavy,
            BorderStyle = new Style(Color.Yellow)
        };

        AnsiConsole.Write(panel);
        AnsiConsole.WriteLine();
    }

    private bool ConfirmScanSystemDirectory() {
        return AnsiConsole.Confirm(
            "Are you sure you want to scan this directory for profiles?",
            defaultValue: false);
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