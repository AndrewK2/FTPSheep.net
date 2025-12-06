using Spectre.Console;
using Spectre.Console.Cli;

namespace FTPSheep.CLI.Commands;

/// <summary>
/// Command to create a new deployment profile.
/// </summary>
internal sealed class ProfileCreateCommand : Command
{
    public override int Execute(CommandContext context, CancellationToken cancellationToken)
    {
        AnsiConsole.MarkupLine("[bold]Create New Deployment Profile[/]");
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[dim]Profile create command stub - implementation pending[/]");

        return 0;
    }
}
