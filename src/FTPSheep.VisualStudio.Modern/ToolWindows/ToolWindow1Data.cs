
using System.Runtime.Serialization;
using Microsoft.VisualStudio.Extensibility.UI;

namespace FTPSheep.VisualStudio.Modern.ToolWindows;
/// <summary>
/// ViewModel for the ToolWindow1Content remote user control.
/// </summary>
[DataContract]
internal class ToolWindow1Data : NotifyPropertyChangedObject {
    public ToolWindow1Data() {
        HelloCommand = new AsyncCommand((parameter, clientContext, cancellationToken) => {
            Text = $"Hello {parameter as string}!";
            return Task.CompletedTask;
        });
    }

    private string _name = string.Empty;
    [DataMember]
    public string Name {
        get => _name;
        set => SetProperty(ref this._name, value);
    }

    private string _text = string.Empty;
    [DataMember]
    public string Text {
        get => _text;
        set => SetProperty(ref this._text, value);
    }

    [DataMember]
    public AsyncCommand HelloCommand { get; }
}
