using Spectre.Console;
using Spectre.Console.Cli;

namespace FTPSheep.CLI.Commands;

/// <summary>
/// Command to initialize a new deployment profile interactively.
/// </summary>
internal sealed class InitCommand : Command {
    public override int Execute(CommandContext context, CancellationToken cancellationToken) {
        AnsiConsole.MarkupLine("[bold]Initialize New Deployment Profile[/]");
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[dim]Init command stub - implementation pending[/]");

        return 0;
    }
}
