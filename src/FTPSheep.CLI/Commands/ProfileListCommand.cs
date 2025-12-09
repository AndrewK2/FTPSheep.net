using Spectre.Console;
using Spectre.Console.Cli;

namespace FTPSheep.CLI.Commands;

/// <summary>
/// Command to list all deployment profiles.
/// </summary>
internal sealed class ProfileListCommand : Command {
    public override int Execute(CommandContext context, CancellationToken cancellationToken) {
        AnsiConsole.MarkupLine("[bold]Available Deployment Profiles[/]");
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[dim]Profile list command stub - implementation pending[/]");

        return 0;
    }
}
