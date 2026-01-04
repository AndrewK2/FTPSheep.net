using System.Runtime.Serialization;
using Microsoft.VisualStudio.Extensibility.UI;

namespace FTPSheep.VisualStudio.Modern.ToolWindows;

/// <summary>
/// Remote user control for the FTPSheep tool window.
/// This control displays the deployment UI and manages user interactions.
/// </summary>
internal class FTPSheepToolWindowControl : RemoteUserControl {
    public FTPSheepToolWindowControl(FTPSheepToolWindowData dataContext) : base(dataContext) {
    }

    /// <summary>
    /// Gets the data context for external access.
    /// </summary>
    public new FTPSheepToolWindowData DataContext => (FTPSheepToolWindowData)base.DataContext!;
}

/// <summary>
/// Represents a project in the solution.
/// </summary>
[DataContract]
internal class ProjectItem {
    [DataMember]
    public string Name { get; set; } = string.Empty;

    [DataMember]
    public string Path { get; set; } = string.Empty;

    public override string ToString() => Name;
}

/// <summary>
/// Represents a deployment profile.
/// </summary>
[DataContract]
internal class ProfileItem {
    [DataMember]
    public string Name { get; set; } = string.Empty;

    [DataMember]
    public string Server { get; set; } = string.Empty;

    [DataMember]
    public string RemotePath { get; set; } = string.Empty;

    [DataMember]
    public DateTime LastModified { get; set; }

    [DataMember]
    public bool HasCredentials { get; set; }

    [DataMember]
    public string FilePath { get; set; } = string.Empty;

    public override string ToString() => $"{Name} - {Server}";
}

/// <summary>
/// Represents a recent deployment history entry.
/// </summary>
[DataContract]
internal class DeploymentHistoryItem {
    [DataMember]
    public string ProfileName { get; set; } = string.Empty;

    [DataMember]
    public string ProjectName { get; set; } = string.Empty;

    [DataMember]
    public DateTime Timestamp { get; set; }

    [DataMember]
    public bool Success { get; set; }

    [DataMember]
    public int FilesUploaded { get; set; }

    [DataMember]
    public string StatusIcon => Success ? "✓" : "✗";

    [DataMember]
    public string TimeFormatted => Timestamp.ToLocalTime().ToString("g");

    public override string ToString() => $"{StatusIcon} {ProfileName} - {ProjectName} ({TimeFormatted}) - {FilesUploaded} files";
}