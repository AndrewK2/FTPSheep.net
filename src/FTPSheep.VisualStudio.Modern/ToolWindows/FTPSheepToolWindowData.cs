using System.Runtime.Serialization;
using Microsoft.VisualStudio.Extensibility.UI;

namespace FTPSheep.VisualStudio.Modern.ToolWindows;

/// <summary>
/// Data context for the FTPSheep tool window.
/// All properties must be serializable with DataContract/DataMember for remote UI.
/// </summary>
[DataContract]
internal class FTPSheepToolWindowData : NotifyPropertyChangedObject {
    public FTPSheepToolWindowData(Func<object?, CancellationToken, Task> deployCommand,
        Func<object?, CancellationToken, Task> newProfileCommand,
        Func<object?, CancellationToken, Task> editProfileCommand,
        Func<object?, CancellationToken, Task> deleteProfileCommand,
        Func<object?, CancellationToken, Task> importProfileCommand) {
        // Create commands with proper signature (parameter, clientContext, cancellationToken)
        DeployCommand = new AsyncCommand((parameter, clientContext, cancellationToken) =>
            deployCommand(parameter, cancellationToken));

        NewProfileCommand = new AsyncCommand((parameter, clientContext, cancellationToken) =>
            newProfileCommand(parameter, cancellationToken));

        EditProfileCommand = new AsyncCommand((parameter, clientContext, cancellationToken) =>
            editProfileCommand(parameter, cancellationToken));

        DeleteProfileCommand = new AsyncCommand((parameter, clientContext, cancellationToken) =>
            deleteProfileCommand(parameter, cancellationToken));

        ImportProfileCommand = new AsyncCommand((parameter, clientContext, cancellationToken) =>
            importProfileCommand(parameter, cancellationToken));
    }

    private string _welcomeMessage = "Welcome to FTPSheep!";

    [DataMember]
    public string WelcomeMessage {
        get => _welcomeMessage;
        set => SetProperty(ref _welcomeMessage, value);
    }

    private string _selectedProject = "No project selected";

    [DataMember]
    public string SelectedProject {
        get => _selectedProject;
        set => SetProperty(ref _selectedProject, value);
    }

    private string _selectedProfile = "No profile selected";

    [DataMember]
    public string SelectedProfile {
        get => _selectedProfile;
        set => SetProperty(ref _selectedProfile, value);
    }

    private List<ProjectItem> _projects = new();

    [DataMember]
    public List<ProjectItem> Projects {
        get => _projects;
        set => SetProperty(ref _projects, value);
    }

    private List<ProfileItem> _profiles = new();

    [DataMember]
    public List<ProfileItem> Profiles {
        get => _profiles;
        set => SetProperty(ref _profiles, value);
    }

    private List<DeploymentHistoryItem> _recentDeployments = new();

    [DataMember]
    public List<DeploymentHistoryItem> RecentDeployments {
        get => _recentDeployments;
        set => SetProperty(ref _recentDeployments, value);
    }

    // Commands must be [DataMember] for Remote UI
    [DataMember]
    public AsyncCommand DeployCommand { get; }

    [DataMember]
    public AsyncCommand NewProfileCommand { get; }

    [DataMember]
    public AsyncCommand EditProfileCommand { get; }

    [DataMember]
    public AsyncCommand DeleteProfileCommand { get; }

    [DataMember]
    public AsyncCommand ImportProfileCommand { get; }
}