using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using FTPSheep.Core.Services;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using Spectre.Console.Cli;

namespace FTPSheep.CLI.Commands;

/// <summary>
/// Command to import Visual Studio publish profile.
/// </summary>
[UsedImplicitly]
internal sealed class ImportCommand(ILogger<ImportCommand> logger) : AsyncCommand<ImportCommand.Settings> {
    /// <summary>
    /// Settings for the import command.
    /// </summary>
    public sealed class Settings : CommandSettings {
        [Description("Path to the Visual Studio publish profile (.pubxml)")]
        [CommandArgument(0, "[PROFILE_PATH]")]
        public string? ProfilePath { get; init; }
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken) {
        AnsiConsole.MarkupLine("[bold]Import Visual Studio Publish Profile[/]");
        AnsiConsole.WriteLine();

        try {
            // Create service instances
            var parser = new PublishProfileParser();
            var converter = new PublishProfileConverter();
            var encryption = new DpapiEncryptionService();

            logger.LogDebug("Importing PUBXML: {0}", settings.ProfilePath);

            // Determine which .pubxml file to import
            string pubxmlPath;
            if(!string.IsNullOrWhiteSpace(settings.ProfilePath)) {
                pubxmlPath = settings.ProfilePath;
                if(!File.Exists(pubxmlPath)) {
                    AnsiConsole.MarkupLine($"[red]Error:[/] File not found: {pubxmlPath}");
                    return 1;
                }
            } else {
                // Discover .pubxml files in current directory
                var currentDir = Directory.GetCurrentDirectory();
                var discoveredProfiles = parser.DiscoverProfiles(currentDir);

                if(discoveredProfiles.Count == 0) {
                    AnsiConsole.MarkupLine("[yellow]No publish profiles (.pubxml) found in the current directory.[/]");
                    AnsiConsole.MarkupLine("Please specify a path to a .pubxml file or run this command from a project directory.");
                    return 1;
                }

                if(discoveredProfiles.Count == 1) {
                    pubxmlPath = discoveredProfiles[0];
                    AnsiConsole.MarkupLine($"Found profile: [cyan]{Path.GetFileName(pubxmlPath)}[/]");
                } else {
                    // Let user select from multiple profiles
                    var profileNames = discoveredProfiles
                        .Select(p => Path.GetFileName(p))
                        .ToList();

                    var selectedName = AnsiConsole.Prompt(
                        new SelectionPrompt<string>()
                        .Title("Multiple publish profiles found. Which one would you like to import?")
                            .AddChoices(profileNames));

                    pubxmlPath = discoveredProfiles[profileNames.IndexOf(selectedName)];
                }
            }

            AnsiConsole.WriteLine();
            AnsiConsole.Status()
                .Start("Importing profile...", ctx => {
                    ctx.Status("Parsing publish profile...");

                    // Parse the .pubxml file
                    var publishProfile = parser.ParseProfile(pubxmlPath);

                    // Convert to DeploymentProfile
                    ctx.Status("Converting to FTPSheep profile...");
                    var deploymentProfile = converter.Convert(publishProfile);

                    // Validate the converted profile
                    var validationErrors = converter.ValidateImportedProfile(deploymentProfile);
                    if(validationErrors.Count > 0) {
                        AnsiConsole.MarkupLine("[red]Validation errors:[/]");
                        foreach(var error in validationErrors) {
                            AnsiConsole.MarkupLine($"  [red]•[/] {error}");
                        }
                        throw new InvalidOperationException("Profile validation failed.");
                    }

                    ctx.Status("Profile converted successfully");
                });

            // Re-parse to get the profile for prompts
            var profile = parser.ParseProfile(pubxmlPath);
            var convertedProfile = converter.Convert(profile);

            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[green]✓[/] Profile parsed and converted successfully");
            AnsiConsole.WriteLine();

            // Display summary
            var table = new Table();
            table.Border(TableBorder.Rounded);
            table.AddColumn("Property");
            table.AddColumn("Value");
            table.AddRow("Host", convertedProfile.Connection.Host);
            table.AddRow("Port", convertedProfile.Connection.Port.ToString());
            table.AddRow("Protocol", convertedProfile.Connection.UseSsl ? "FTPS" : "FTP");
            table.AddRow("Username", convertedProfile.Username ?? "[dim]not set[/]");
            table.AddRow("Remote Path", string.IsNullOrWhiteSpace(convertedProfile.RemotePath) ? "/" : convertedProfile.RemotePath);
            AnsiConsole.Write(table);
            AnsiConsole.WriteLine();

            // Prompt for profile name
            var defaultName = Path.GetFileNameWithoutExtension(pubxmlPath);
            var profileName = AnsiConsole.Ask("Enter profile name:", defaultName);

            // Update profile name
            convertedProfile.Name = profileName;

            // Prompt for password if not already set
            if(string.IsNullOrWhiteSpace(convertedProfile.Username)) {
                var username = AnsiConsole.Ask<string>("Enter FTP username:");
                convertedProfile.Username = username;
            }

            var password = AnsiConsole.Prompt(
                new TextPrompt<string>("Enter FTP password:")
                    .Secret());

            // Encrypt and store the password
            if(!string.IsNullOrWhiteSpace(password)) {
                convertedProfile.EncryptedPassword = encryption.Encrypt(password);
            }

            // Prompt for project path if not set
            if(string.IsNullOrWhiteSpace(convertedProfile.ProjectPath)) {
                var defaultProjectPath = FindProjectFile(pubxmlPath);
                string projectPath;

                if(!string.IsNullOrEmpty(defaultProjectPath)) {
                    projectPath = AnsiConsole.Ask("Enter path to .csproj file:", defaultProjectPath);
                } else {
                    projectPath = AnsiConsole.Ask<string>("Enter path to .csproj file:");
                }

                convertedProfile.ProjectPath = projectPath;
            }

            // Prompt for save directory
            var defaultSaveDir = Path.GetDirectoryName(pubxmlPath);
            if(string.IsNullOrWhiteSpace(defaultSaveDir)) {
                defaultSaveDir = Directory.GetCurrentDirectory();
            }
            var saveDirectory = AnsiConsole.Ask("Enter directory to save profile:", defaultSaveDir);

            // Ensure directory exists
            if(!Directory.Exists(saveDirectory)) {
                Directory.CreateDirectory(saveDirectory);
            }

            // Build the full save path
            var profileFileName = $"{profileName}.ftpsheep";
            var profileSavePath = Path.Combine(saveDirectory, profileFileName);

            // Convert ProjectPath to relative if possible
            if (!string.IsNullOrWhiteSpace(convertedProfile.ProjectPath))
            {
                // Ensure we have an absolute path
                var absoluteProjectPath = Path.GetFullPath(convertedProfile.ProjectPath);

                // Get the directory where profile will be saved
                var profileDirectory = Path.GetDirectoryName(profileSavePath)!;

                // Try to create relative path
                var relativePath = Path.GetRelativePath(profileDirectory, absoluteProjectPath);

                // Use relative path if valid, otherwise use absolute
                // Path.GetRelativePath returns absolute path if no relative path exists (different drives)
                if (Path.IsPathRooted(relativePath))
                {
                    // Different drives or can't create relative path - use absolute
                    convertedProfile.ProjectPath = absoluteProjectPath;
                }
                else
                {
                    // Relative path is valid - use it
                    convertedProfile.ProjectPath = relativePath;
                }
            }

            // Check if file already exists at the custom location
            if(File.Exists(profileSavePath)) {
                var overwriteFile = await AnsiConsole.ConfirmAsync($"File '{profileFileName}' already exists in this directory. Overwrite?", false, cancellationToken);
                if(!overwriteFile) {
                    AnsiConsole.MarkupLine("[yellow]Import cancelled.[/]");
                    return 0;
                }
            }

            //await profileService.CreateProfileAsync(convertedProfile, cancellationToken);

            
            // Serialize and save the profile to the custom location
            var jsonOptions = new JsonSerializerOptions {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                Converters = { new JsonStringEnumConverter() }
            };

            var json = JsonSerializer.Serialize(convertedProfile, jsonOptions);

            logger.LogTrace("Profile JSON:\n{p}", json);
            logger.LogDebug("Writing profile to: {p}", profileSavePath);

            File.WriteAllText(profileSavePath, json);

            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[green]✓[/] Profile imported successfully!");
            AnsiConsole.MarkupLine($"Profile saved to: [cyan]{profileSavePath}[/]");
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine($"You can now deploy using: [cyan]ftpsheep deploy --profile {profileName}[/] or [cyan]ftpsheep deploy --file \"{profileSavePath}\"[/]");
            

            return 0;
        } catch(Exception ex) {
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
            return 1;
        }
    }

    /// <summary>
    /// Searches for a .csproj, .vbproj, or .fsproj file starting from the .pubxml file's directory and moving up to parent directories.
    /// </summary>
    /// <param name="pubxmlPath">The path to the .pubxml file to use as the starting point.</param>
    /// <returns>The path to the first project file found, or null if none found.</returns>
    private static string? FindProjectFile(string pubxmlPath) {
        // Start from the directory containing the .pubxml file
        var pubxmlDir = Path.GetDirectoryName(pubxmlPath);
        if(string.IsNullOrEmpty(pubxmlDir)) {
            pubxmlDir = Directory.GetCurrentDirectory();
        }

        var currentDir = new DirectoryInfo(pubxmlDir);

        while(currentDir != null) {
            // Search for project files in order of preference
            var projectExtensions = new[] { "*.csproj", "*.vbproj", "*.fsproj" };

            foreach(var extension in projectExtensions) {
                var projectFiles = currentDir.GetFiles(extension, SearchOption.TopDirectoryOnly);
                if(projectFiles.Length > 0) {
                    // Return the first found project file
                    return projectFiles[0].FullName;
                }
            }

            // Move to parent directory
            currentDir = currentDir.Parent;
        }

        return null;
    }
}
