using System.ComponentModel;
using Spectre.Console;
using Spectre.Console.Cli;

namespace FTPSheep.CLI.Commands;

/// <summary>
/// Command to deploy a .NET application to FTP server.
/// </summary>
internal sealed class DeployCommand : Command<DeployCommand.Settings> {
    /// <summary>
    /// Settings for the deploy command.
    /// </summary>
    public sealed class Settings : CommandSettings {
        [Description("Name of the deployment profile to use")]
        [CommandOption("-p|--profile <PROFILE>")]
        public string? ProfileName { get; init; }

        [Description("Skip all confirmation prompts")]
        [CommandOption("-y|--yes")]
        public bool AutoConfirm { get; init; }

        [Description("Enable verbose output")]
        [CommandOption("-v|--verbose")]
        public bool Verbose { get; init; }

        [Description("Perform a dry run without making changes")]
        [CommandOption("--dry-run")]
        public bool DryRun { get; init; }
    }

    public override int Execute(CommandContext context, Settings settings, CancellationToken cancellationToken) {
        AnsiConsole.MarkupLine("[bold green]FTPSheep.NET[/] - Deployment Tool");
        AnsiConsole.WriteLine();

        if(settings.ProfileName != null) {
            AnsiConsole.MarkupLine($"Using profile: [cyan]{settings.ProfileName}[/]");
        } else {
            AnsiConsole.MarkupLine("[yellow]No profile specified. Auto-discovery will be implemented.[/]");
        }

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[dim]Deploy command stub - implementation pending[/]");

        return 0;
    }
}
