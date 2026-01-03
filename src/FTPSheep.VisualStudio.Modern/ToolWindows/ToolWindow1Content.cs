
using Microsoft.VisualStudio.Extensibility.UI;

namespace FTPSheep.VisualStudio.Modern.ToolWindows;
/// <summary>
/// A remote user control to use as tool window UI content.
/// </summary>
internal class ToolWindow1Content : RemoteUserControl {
    /// <summary>
    /// Initializes a new instance of the <see cref="ToolWindow1Content" /> class.
    /// </summary>
    public ToolWindow1Content()
        : base(dataContext: new ToolWindow1Data()) {
    }
}
