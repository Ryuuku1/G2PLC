using CommunityToolkit.Mvvm.ComponentModel;

namespace G2PLC.UI.Wpf.ViewModels;

public partial class RegisterMonitorViewModel : ObservableObject
{
    [ObservableProperty]
    private ushort _address;

    [ObservableProperty]
    private ushort _currentValue;

    [ObservableProperty]
    private string _description;

    public RegisterMonitorViewModel(ushort address, string description = "")
    {
        _address = address;
        _description = description;
        _currentValue = 0;
    }
}
