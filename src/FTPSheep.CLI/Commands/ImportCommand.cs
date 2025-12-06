using System.ComponentModel;
using Spectre.Console;
using Spectre.Console.Cli;

namespace FTPSheep.CLI.Commands;

/// <summary>
/// Command to import Visual Studio publish profile.
/// </summary>
internal sealed class ImportCommand : Command<ImportCommand.Settings>
{
    /// <summary>
    /// Settings for the import command.
    /// </summary>
    public sealed class Settings : CommandSettings
    {
        [Description("Path to the Visual Studio publish profile (.pubxml)")]
        [CommandArgument(0, "[PROFILE_PATH]")]
        public string? ProfilePath { get; init; }
    }

    public override int Execute(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        AnsiConsole.MarkupLine("[bold]Import Visual Studio Publish Profile[/]");
        AnsiConsole.WriteLine();

        if (settings.ProfilePath != null)
        {
            AnsiConsole.MarkupLine($"Profile path: [cyan]{settings.ProfilePath}[/]");
        }

        AnsiConsole.MarkupLine("[dim]Import command stub - implementation pending[/]");

        return 0;
    }
}
