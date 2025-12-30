using Microsoft.VisualStudio.Extensibility.UI;
using System.Collections.ObjectModel;
using System.Runtime.Serialization;
using System.Windows.Input;

namespace FTPSheep.VisualStudio.Modern.ToolWindows;

/// <summary>
/// Remote user control for the FTPSheep tool window.
/// This control displays the deployment UI and manages user interactions.
/// </summary>
internal class FTPSheepToolWindowControl : RemoteUserControl
{
    public FTPSheepToolWindowControl()
        : base(new FTPSheepToolWindowData())
    {
    }

    /// <summary>
    /// Gets the data context for external access.
    /// </summary>
    public new FTPSheepToolWindowData DataContext => (FTPSheepToolWindowData)base.DataContext!;

    /// <summary>
    /// Sets the command handlers for the tool window.
    /// </summary>
    public void SetCommandHandlers(
        Func<object?, CancellationToken, Task> deployCommand,
        Func<object?, CancellationToken, Task> newProfileCommand,
        Func<object?, CancellationToken, Task> editProfileCommand,
        Func<object?, CancellationToken, Task> deleteProfileCommand,
        Func<object?, CancellationToken, Task> importProfileCommand)
    {
        var dataContext = this.DataContext;
        dataContext.DeployCommand = new AsyncCommand(deployCommand);
        dataContext.NewProfileCommand = new AsyncCommand(newProfileCommand);
        dataContext.EditProfileCommand = new AsyncCommand(editProfileCommand);
        dataContext.DeleteProfileCommand = new AsyncCommand(deleteProfileCommand);
        dataContext.ImportProfileCommand = new AsyncCommand(importProfileCommand);
    }
}

/// <summary>
/// Data context for the FTPSheep tool window.
/// All properties must be serializable with DataContract/DataMember for remote UI.
/// </summary>
[DataContract]
internal class FTPSheepToolWindowData
{
    [DataMember]
    public string WelcomeMessage { get; set; } = "Welcome to FTPSheep!";

    [DataMember]
    public string SelectedProject { get; set; } = "No project selected";

    [DataMember]
    public string SelectedProfile { get; set; } = "No profile selected";

    [DataMember]
    public List<ProjectItem> Projects { get; set; } = new();

    [DataMember]
    public List<ProfileItem> Profiles { get; set; } = new();

    [DataMember]
    public List<DeploymentHistoryItem> RecentDeployments { get; set; } = new();

    // Command properties (not serialized, set at runtime)
    public IAsyncCommand? DeployCommand { get; set; }
    public IAsyncCommand? NewProfileCommand { get; set; }
    public IAsyncCommand? EditProfileCommand { get; set; }
    public IAsyncCommand? DeleteProfileCommand { get; set; }
    public IAsyncCommand? ImportProfileCommand { get; set; }
}

/// <summary>
/// Represents a project in the solution.
/// </summary>
[DataContract]
internal class ProjectItem
{
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
internal class ProfileItem
{
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
internal class DeploymentHistoryItem
{
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

    public override string ToString() =>
        $"{StatusIcon} {ProfileName} - {ProjectName} ({TimeFormatted}) - {FilesUploaded} files";
}
