using CommunityToolkit.Mvvm.ComponentModel;

namespace G2PLC.UI.Wpf.ViewModels;

public partial class ExecutionStepViewModel : ObservableObject
{
    [ObservableProperty]
    private int _lineNumber;

    [ObservableProperty]
    private string _command;

    [ObservableProperty]
    private string _status;

    public ExecutionStepViewModel(int lineNumber, string command, string status = "Pending")
    {
        _lineNumber = lineNumber;
        _command = command;
        _status = status;
    }
}
