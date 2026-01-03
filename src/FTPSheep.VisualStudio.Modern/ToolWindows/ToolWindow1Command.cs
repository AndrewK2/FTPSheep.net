
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Extensibility;
using Microsoft.VisualStudio.Extensibility.Commands;

namespace FTPSheep.VisualStudio.Modern.ToolWindows;
/// <summary>
/// A command for showing a tool window.
/// </summary>
[VisualStudioContribution]
public class ToolWindow1Command : Command {
    /// <inheritdoc />
    public override CommandConfiguration CommandConfiguration => new(displayName: "Show Tool Window") {
        // Use this object initializer to set optional parameters for the command. The required parameter,
        // displayName, is set above. To localize the displayName, add an entry in .vsextension\string-resources.json
        // and reference it here by passing "%FTPSheep.VisualStudio.Modern.ToolWindows.ToolWindow1Command.DisplayName%" as a constructor parameter.
        Placements = [CommandPlacement.KnownPlacements.ExtensionsMenu],
        Icon = new(ImageMoniker.KnownValues.Extension, IconSettings.IconAndText),
    };

    /// <inheritdoc />
    public override async Task ExecuteCommandAsync(IClientContext context, CancellationToken cancellationToken) {
        await this.Extensibility.Shell().ShowToolWindowAsync<ToolWindow1>(activate: true, cancellationToken);
    }
}
