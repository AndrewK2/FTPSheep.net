using FTPSheep.VisualStudio.Modern.ToolWindows;
using Microsoft.VisualStudio.Extensibility;
using Microsoft.VisualStudio.Extensibility.Commands;

namespace FTPSheep.VisualStudio.Modern.Commands;

/// <summary>
/// Command to show the FTPSheep tool window.
/// This command appears in the View > Other Windows menu.
/// </summary>
[VisualStudioContribution]
public class ShowFTPSheepCommand : Command {
    /// <inheritdoc />
    public ShowFTPSheepCommand(VisualStudioExtensibility extensibility) : base(extensibility) {
    }

    /// <summary>
    /// Command configuration - appears in View > Other Windows menu.
    /// </summary>
    public override CommandConfiguration CommandConfiguration =>
        new("Publish with FTPSheep") {
            Placements = [CommandPlacement.KnownPlacements.ToolsMenu],
            Icon = new(ImageMoniker.KnownValues.ToolWindow, IconSettings.IconAndText),
            Shortcuts = [new CommandShortcutConfiguration(ModifierKey.ControlShiftLeftAlt, Key.P)],
        };

    /// <summary>
    /// Execute the command - show the FTPSheep tool window.
    /// </summary>
    public override async Task ExecuteCommandAsync(IClientContext context, CancellationToken cancellationToken) {
        // Show the tool window
        await Extensibility.Shell().ShowToolWindowAsync<FTPSheepToolWindow>(activate: true, cancellationToken);
    }
}